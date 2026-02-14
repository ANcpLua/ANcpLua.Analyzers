using System.Collections.Concurrent;
using ANcpLua.Analyzers.Core;
using ANcpLua.Roslyn.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0063: Detects ActivitySource instances that are not registered with AddSource().
/// </summary>
/// <remarks>
///     <para>
///         ActivitySources must be registered with AddSource() in the OpenTelemetry tracing
///         configuration to emit spans. Unregistered sources will silently fail to produce traces.
///     </para>
///     <para>
///         Uses cross-compilation analysis: collects all AddSource() calls and ActivitySource
///         creations across the entire compilation, then reports only unmatched sources.
///         Supports wildcard patterns (e.g., AddSource("OpenAI.*") covers "OpenAI.Chat").
///     </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0063UnregisteredActivitySourceAnalyzer : AlAnalyzer {
    /// <summary>The diagnostic identifier for AL0063.</summary>
    public const string DiagnosticId = "AL0063";

    private const string ActivitySourceTypeName = "System.Diagnostics.ActivitySource";

    private static readonly DiagnosticDescriptor Rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.OpenTelemetry,
        DiagnosticSeverities.Suggestion);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    /// <summary>Registers compilation-wide analysis for cross-file ActivitySource tracking.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterCompilationStartAction(OnCompilationStart);

    private static void OnCompilationStart(CompilationStartAnalysisContext context) {
        var compilation = context.Compilation;
        var registeredSources = new ConcurrentBag<string>();
        var activitySourceCreations = new ConcurrentBag<(Location Location, string Name)>();

        // Collect AddSource() calls with constant string arguments
        context.RegisterOperationAction(ctx => {
            if (ctx.Operation is not IInvocationOperation invocation) return;
            if (invocation.TargetMethod.Name != "AddSource") return;
            if (invocation.Arguments.Length is 0) return;

            if (invocation.Arguments[0].Value.ConstantValue is { HasValue: true, Value: string name }) {
                registeredSources.Add(name);
            } else {
                ResolveFromForeachCollection(invocation.Arguments[0].Value, compilation, registeredSources);
            }
        }, OperationKind.Invocation);

        // Collect new ActivitySource(...) creations with constant string arguments
        context.RegisterOperationAction(ctx => {
            var creation = (IObjectCreationOperation)ctx.Operation;
            if (creation.Type?.ToDisplayString() != ActivitySourceTypeName) return;
            if (creation.Arguments.Length is 0) return;

            if (creation.Arguments[0].Value.ConstantValue is { HasValue: true, Value: string name }) {
                activitySourceCreations.Add((creation.Arguments[0].Value.Syntax.GetLocation(), name));
            }
        }, OperationKind.ObjectCreation);

        // At compilation end, report only unregistered sources
        context.RegisterCompilationEndAction(endCtx => {
            // Snapshot the registered sources for matching
            var registered = registeredSources.ToArray();

            foreach (var (location, sourceName) in activitySourceCreations) {
                if (!IsRegistered(sourceName, registered)) {
                    endCtx.ReportDiagnostic(Diagnostic.Create(Rule, location, sourceName));
                }
            }
        });
    }

    /// <summary>
    ///     Checks whether a source name matches any registered AddSource() call,
    ///     including wildcard patterns like "OpenAI.*".
    /// </summary>
    private static bool IsRegistered(string sourceName, ReadOnlySpan<string> registered) {
        foreach (var pattern in registered) {
            if (string.Equals(pattern, sourceName, StringComparison.Ordinal)) {
                return true;
            }

            // Wildcard: "OpenAI.*" matches "OpenAI.Chat", "OpenAI.Embeddings", etc.
            if (pattern.Length > 2 &&
                pattern.EndsWithOrdinal(".*") &&
                sourceName.AsSpan().StartsWith(pattern.AsSpan(0, pattern.Length - 2), StringComparison.Ordinal)) {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    ///     Resolves registered source names from a foreach-over-array pattern:
    ///     <c>foreach (var s in SomeStaticReadonlyArray) tracing.AddSource(s);</c>
    /// </summary>
    private static void ResolveFromForeachCollection(
        IOperation argumentValue, Compilation compilation, ConcurrentBag<string> registeredSources) {
        // Walk up to find enclosing foreach loop
        var current = argumentValue.Parent;
        while (current is not null and not IForEachLoopOperation) {
            current = current.Parent;
        }

        if (current is not IForEachLoopOperation foreachLoop) return;

        // Unwrap conversion wrappers on the collection
        var collection = foreachLoop.Collection;
        while (collection is IConversionOperation conversion) {
            collection = conversion.Operand;
        }

        // Must be a reference to a static readonly field
        if (collection is not IFieldReferenceOperation { Field: { IsStatic: true, IsReadOnly: true } field }) return;

        // Resolve field declaration syntax to extract initializer elements
        if (field.DeclaringSyntaxReferences.FirstOrDefault() is not { } syntaxRef) return;

        var fieldSyntax = syntaxRef.GetSyntax();

        // RS1030: Cross-tree analysis requires GetSemanticModel — no alternative for resolving
        // constant values from field initializers declared in a different syntax tree.
#pragma warning disable RS1030
        var model = compilation.GetSemanticModel(fieldSyntax.SyntaxTree);
#pragma warning restore RS1030

        // Find the initializer — walk from VariableDeclarator to its EqualsValueClause
        if (fieldSyntax is not VariableDeclaratorSyntax { Initializer.Value: { } initializerValue }) return;

        // Extract elements from collection expression [...] or array initializer new[] { ... }
        if (GetInitializerElements(initializerValue) is not { } elements) return;

        foreach (var expr in elements) {
            var constant = model.GetConstantValue(expr);
            if (constant is { HasValue: true, Value: string name }) {
                registeredSources.Add(name);
            }
        }
    }

    private static IEnumerable<ExpressionSyntax>? GetInitializerElements(ExpressionSyntax initializer) =>
        initializer switch {
            CollectionExpressionSyntax col => col.Elements.OfType<ExpressionElementSyntax>()
                .Select(static e => e.Expression),
            ArrayCreationExpressionSyntax arr => arr.Initializer?.Expressions,
            ImplicitArrayCreationExpressionSyntax implArr => implArr.Initializer.Expressions,
            _ => null
        };
}
