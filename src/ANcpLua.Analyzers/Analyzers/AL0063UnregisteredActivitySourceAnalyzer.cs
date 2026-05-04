using System.Collections.Concurrent;

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
    private const string DiagnosticId = "AL0063";

    private const string ActivitySourceTypeName = "System.Diagnostics.ActivitySource";

    private static readonly DiagnosticDescriptor s_rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.OpenTelemetry,
        DiagnosticSeverities.Suggestion);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_rule];

    /// <summary>Registers compilation-wide analysis for cross-file ActivitySource tracking.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterCompilationStartAction(OnCompilationStart);

    private static void OnCompilationStart(CompilationStartAnalysisContext context) {
        if (context.Compilation.GetTypeByMetadataName(ActivitySourceTypeName) is not { } activitySourceSymbol) {
            return;
        }

        var tracerProviderBuilderType = context.Compilation.GetTypeByMetadataName("OpenTelemetry.Trace.TracerProviderBuilder");

        var registeredSources = new ConcurrentBag<string>();
        var activitySourceCreations = new ConcurrentBag<(Location Location, string Name)>();
        var fieldSourceNames = new ConcurrentDictionary<IFieldSymbol, ImmutableArray<string>>(SymbolEqualityComparer.Default);
        var pendingForeachResolutions = new ConcurrentBag<IOperation>();

        // Pre-index static readonly string[] fields once, using the already-provided ctx.SemanticModel
        context.RegisterSyntaxNodeAction(
            ctx => CollectStaticReadonlyFieldConstants(ctx, fieldSourceNames),
            SyntaxKind.VariableDeclarator);

        context.RegisterOperationAction(ctx => {
            var invocation = (IInvocationOperation)ctx.Operation;
            if (!invocation.IsMethodNamed(tracerProviderBuilderType, "AddSource") || invocation.Arguments.Length is 0) {
                return;
            }

            var argumentValue = invocation.Arguments[0].Value;

            if (argumentValue.ConstantValue is { HasValue: true, Value: string name }) {
                registeredSources.Add(name);
            } else {
                // Defer foreach resolution to CompilationEnd where fieldSourceNames is fully populated
                pendingForeachResolutions.Add(argumentValue);
            }
        }, OperationKind.Invocation);

        context.RegisterOperationAction(ctx => {
            var creation = (IObjectCreationOperation)ctx.Operation;
            if (!creation.Type.IsEqualTo(activitySourceSymbol) || creation.Arguments.Length is 0) {
                return;
            }

            if (creation.Arguments[0].Value.ConstantValue is { HasValue: true, Value: string name }) {
                activitySourceCreations.Add((creation.Arguments[0].Value.Syntax.GetLocation(), name));
            }
        }, OperationKind.ObjectCreation);

        context.RegisterCompilationEndAction(endCtx => {
            // Resolve foreach-based registrations now that all field constants are indexed
            foreach (var argumentValue in pendingForeachResolutions) {
                ResolveFromForeachCollection(argumentValue, fieldSourceNames, registeredSources);
            }

            var registered = registeredSources.ToArray();

            foreach (var (location, sourceName) in activitySourceCreations) {
                if (!IsRegistered(sourceName, registered)) {
                    endCtx.ReportDiagnostic(Diagnostic.Create(s_rule, location, sourceName));
                }
            }
        });
    }

    private static void CollectStaticReadonlyFieldConstants(
        SyntaxNodeAnalysisContext context,
        ConcurrentDictionary<IFieldSymbol, ImmutableArray<string>> fieldSourceNames) {
        var declarator = (VariableDeclaratorSyntax)context.Node;

        if (declarator.Initializer?.Value is not { } initializerValue) {
            return;
        }

        if (context.SemanticModel.GetDeclaredSymbol(declarator, context.CancellationToken)
            is not IFieldSymbol { IsStatic: true, IsReadOnly: true } field) {
            return;
        }

        if (GetInitializerElements(initializerValue) is not { } elements) {
            return;
        }

        var builder = ImmutableArray.CreateBuilder<string>();

        foreach (var expr in elements) {
            if (context.SemanticModel.GetConstantValue(expr, context.CancellationToken)
                is { HasValue: true, Value: string name }) {
                builder.Add(name);
            }
        }

        if (builder.Count > 0) {
            fieldSourceNames.TryAdd(field, builder.ToImmutable());
        }
    }

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

    private static void ResolveFromForeachCollection(
        IOperation argumentValue,
        ConcurrentDictionary<IFieldSymbol, ImmutableArray<string>> fieldSourceNames,
        ConcurrentBag<string> registeredSources) {
        var current = argumentValue.Parent;
        while (current is not null and not IForEachLoopOperation) {
            current = current.Parent;
        }

        if (current is not IForEachLoopOperation foreachLoop) {
            return;
        }

        var collection = foreachLoop.Collection.UnwrapAllConversions();

        if (collection is not IFieldReferenceOperation { Field: var field }) {
            return;
        }

        if (!fieldSourceNames.TryGetValue(field, out var names)) {
            return;
        }

        foreach (var name in names.AsSpan()) {
            registeredSources.Add(name);
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
