namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL1313: Forward an available <see cref="CancellationToken" /> to calls that can accept one.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al1313CancellationTokenPropagationAnalyzer : AlAnalyzer {
    /// <summary>The diagnostic identifier for AL1313.</summary>
    public const string DiagnosticId = "AL1313";

    private static readonly DiagnosticDescriptor s_rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.Reliability,
        DiagnosticSeverity.Info);

    /// <summary>Gets the diagnostic descriptors supported by this analyzer.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_rule];

    /// <summary>Registers analysis actions for AL1313.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterCompilationStartAction(OnCompilationStart);

    private static void OnCompilationStart(CompilationStartAnalysisContext context) {
        if (context.Compilation.GetTypeByMetadataName("System.Threading.CancellationToken") is not
            INamedTypeSymbol cancellationTokenType) {
            return;
        }

        var expressionType =
            context.Compilation.GetTypeByMetadataName("System.Linq.Expressions.Expression`1");

        context.RegisterOperationAction(
            operationContext => AnalyzeInvocation(
                operationContext,
                cancellationTokenType,
                expressionType),
            OperationKind.Invocation);
    }

    private static void AnalyzeInvocation(
        OperationAnalysisContext context,
        INamedTypeSymbol cancellationTokenType,
        INamedTypeSymbol? expressionType) {
        var invocation = (IInvocationOperation)context.Operation;

        if (!Al1313CancellationTokenPropagationAnalysis.TryFindSuggestion(
                invocation,
                cancellationTokenType,
                expressionType,
                context.CancellationToken,
                out _)) {
            return;
        }

        context.ReportDiagnostic(s_rule, invocation.Syntax.GetLocation(), invocation.TargetMethod.Name);
    }
}
