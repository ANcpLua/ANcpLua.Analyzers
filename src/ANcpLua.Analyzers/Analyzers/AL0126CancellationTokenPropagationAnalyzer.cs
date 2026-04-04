namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0126: Forward an available <see cref="CancellationToken" /> to calls that can accept one.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0126CancellationTokenPropagationAnalyzer : AlAnalyzer {
    /// <summary>The diagnostic identifier for AL0126.</summary>
    public const string DiagnosticId = "AL0126";

    private static readonly DiagnosticDescriptor Rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.Reliability,
        DiagnosticSeverity.Info);

    /// <summary>Gets the diagnostic descriptors supported by this analyzer.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    /// <summary>Registers analysis actions for AL0126.</summary>
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

        if (!Al0126CancellationTokenPropagationAnalysis.TryFindSuggestion(
                invocation,
                cancellationTokenType,
                expressionType,
                context.CancellationToken,
                out _)) {
            return;
        }

        context.ReportDiagnostic(Rule, invocation.Syntax.GetLocation(), invocation.TargetMethod.Name);
    }
}
