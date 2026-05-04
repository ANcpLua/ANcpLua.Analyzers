
namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0065: Detects token usage metrics that don't use the standard histogram.
/// </summary>
/// <remarks>
///     <para>
///         GenAI token usage should be recorded using the standard histogram:
///         <c>gen_ai.client.token.usage</c>
///     </para>
///     <para>
///         This ensures compatibility with standard GenAI observability dashboards.
///     </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0065UseTokenUsageHistogramAnalyzer : AlAnalyzer {
    private const string CorrectMetricName = "gen_ai.client.token.usage";
    private const string HistogramAttributeFullName = "Qyl.Instrumentation.Instrumentation.HistogramAttribute";

    private static readonly string[] s_tokenRelatedPatterns = [
        "token", "input_token", "output_token", "prompt_token", "completion_token"
    ];

    /// <summary>The diagnostic identifier for AL0065.</summary>
    private const string DiagnosticId = "AL0065";

    private static readonly DiagnosticDescriptor s_rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.GenAI,
        DiagnosticSeverities.Suggestion);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_rule];

    /// <summary>Registers symbol actions to analyze methods with histogram attributes.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterSymbolAction(AnalyzeMethod, SymbolKind.Method);

    private static void AnalyzeMethod(SymbolAnalysisContext context) {
        var method = (IMethodSymbol)context.Symbol;

        if (context.Compilation.GetTypeByMetadataName(HistogramAttributeFullName) is not { } histogramType) {
            return;
        }

        foreach (var attribute in method.GetAttributes()) {
            if (!attribute.AttributeClass.IsEqualTo(histogramType) ||
                attribute.ConstructorArguments.Length is 0 ||
                attribute.ConstructorArguments[0].Value is not string metricName ||
                !IsTokenRelatedMetric(metricName) ||
                metricName.EqualsIgnoreCase(CorrectMetricName)) {
                continue;
            }

            var location = attribute.ApplicationSyntaxReference?.GetSyntax(context.CancellationToken)
                .GetLocation() ?? method.Locations.FirstOrDefault();

            if (location is not null) {
                context.ReportDiagnostic(Diagnostic.Create(s_rule, location, metricName));
            }
        }
    }

    private static bool IsTokenRelatedMetric(string metricName) =>
        s_tokenRelatedPatterns.Any(metricName.ContainsIgnoreCase);
}
