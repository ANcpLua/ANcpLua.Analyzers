using ANcpLua.Analyzers.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using System.Collections.Immutable;

namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0101: Avoid Activator.CreateInstance in AOT context.
/// </summary>
/// <remarks>
///     <para>
///         <c>Activator.CreateInstance</c> uses reflection to create instances at runtime.
///         In Native AOT, this requires the target type's constructor to be preserved.
///         Without explicit preservation, the constructor may be trimmed, causing runtime failures.
///     </para>
///     <para>
///         Prefer explicit construction, factory patterns, or annotate with
///         [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructors)].
///     </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0101AvoidActivatorCreateInstanceAnalyzer : AlAnalyzer {
    private const string ActivatorTypeName = "System.Activator";

    private static readonly DiagnosticDescriptor Rule = CreateRule(
        DiagnosticIds.AvoidActivatorCreateInstance,
        DiagnosticCategories.AotTesting,
        DiagnosticSeverities.Suggestion);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    /// <summary>Registers a compilation start action to resolve the Activator type once.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterCompilationStartAction(OnCompilationStart);

    private static void OnCompilationStart(CompilationStartAnalysisContext context) {
        if (context.Compilation.GetTypeByMetadataName(ActivatorTypeName) is not { } activatorType) {
            // System.Activator is not referenced; nothing to analyze
            return;
        }

        context.RegisterOperationAction(
            ctx => AnalyzeInvocation(ctx, activatorType),
            OperationKind.Invocation);
    }

    private static void AnalyzeInvocation(OperationAnalysisContext context, INamedTypeSymbol activatorType) {
        var invocation = (IInvocationOperation)context.Operation;
        var targetMethod = invocation.TargetMethod;

        // Check if the method is CreateInstance on System.Activator
        if (targetMethod.Name is not "CreateInstance") {
            return;
        }

        if (!SymbolEqualityComparer.Default.Equals(targetMethod.ContainingType, activatorType)) {
            return;
        }

        // Extract type argument for diagnostic message
        string typeName;
        if (targetMethod.IsGenericMethod && targetMethod.TypeArguments.Length > 0) {
            // Generic overload: Activator.CreateInstance<T>()
            typeName = targetMethod.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        }
        else if (invocation.Arguments.Length > 0
                 && invocation.Arguments[0].Value is ITypeOfOperation typeOfOp) {
            // Non-generic overload with typeof: Activator.CreateInstance(typeof(T))
            typeName = typeOfOp.TypeOperand.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        }
        else {
            // Dynamic type argument - report with generic message
            typeName = "T";
        }

        context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.Syntax.GetLocation(), typeName));
    }
}
