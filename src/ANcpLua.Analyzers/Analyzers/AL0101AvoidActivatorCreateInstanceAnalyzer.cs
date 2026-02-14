using ANcpLua.Roslyn.Utilities.Matching;

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
    /// <summary>The diagnostic identifier for AL0101.</summary>
    public const string DiagnosticId = "AL0101";

    private const string ActivatorTypeName = "System.Activator";
    private static readonly InvocationMatcher CreateInstanceInvocation = Invoke.Method("CreateInstance");

    private static readonly DiagnosticDescriptor Rule = CreateRule(
        DiagnosticId,
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
        if (context.Operation is not IInvocationOperation invocation ||
            !CreateInstanceInvocation.Matches(invocation)) {
            return;
        }

        var targetMethod = invocation.TargetMethod;
        if (!targetMethod.ContainingType.IsEqualTo(activatorType)) {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            Rule,
            invocation.Syntax.GetLocation(),
            GetTargetTypeName(invocation)));
    }

    private static string GetTargetTypeName(IInvocationOperation invocation) {
        var targetMethod = invocation.TargetMethod;
        if (targetMethod.IsGenericMethod && targetMethod.TypeArguments.Length > 0) {
            return targetMethod.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        }

        if (invocation.Arguments.Length > 0 &&
            invocation.Arguments[0].Value is ITypeOfOperation typeOfOperation) {
            return typeOfOperation.TypeOperand.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        }

        return "T";
    }
}
