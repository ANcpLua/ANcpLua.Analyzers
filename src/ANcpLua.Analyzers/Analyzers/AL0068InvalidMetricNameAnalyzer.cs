using System.Text.RegularExpressions;

namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0068: Detects metric instrument names that don't follow naming conventions.
/// </summary>
/// <remarks>
///     <para>
///         Metric names should follow OpenTelemetry naming conventions:
///         <list type="bullet">
///             <item>Use dot-separated namespaces (e.g., myapp.orders.count)</item>
///             <item>Use snake_case for individual words</item>
///             <item>Include unit as suffix when applicable</item>
///         </list>
///     </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0068InvalidMetricNameAnalyzer : AlAnalyzer {
    private const string CounterAttributeFullName = "qyl.ServiceDefaults.Instrumentation.CounterAttribute";
    private const string HistogramAttributeFullName = "qyl.ServiceDefaults.Instrumentation.HistogramAttribute";

    // Pattern: lowercase letters, numbers, dots, and underscores only
    // Should have at least one dot (namespace separator)
    private static readonly Regex ValidNamePattern = new(
        @"^[a-z][a-z0-9_.]*\.[a-z][a-z0-9_.]*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>The diagnostic identifier for AL0068.</summary>
    public const string DiagnosticId = "AL0068";

    private static readonly DiagnosticDescriptor Rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.Metrics,
        DiagnosticSeverities.Suggestion);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    /// <summary>Registers symbol actions to analyze methods with metric attributes.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterSymbolAction(AnalyzeMethod, SymbolKind.Method);

    private static void AnalyzeMethod(SymbolAnalysisContext context) {
        var method = (IMethodSymbol)context.Symbol;

        var counterType = context.Compilation.GetTypeByMetadataName(CounterAttributeFullName);
        var histogramType = context.Compilation.GetTypeByMetadataName(HistogramAttributeFullName);

        foreach (var attribute in method.GetAttributes()) {
            var isCounter = SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, counterType);
            var isHistogram = SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, histogramType);

            if (!isCounter && !isHistogram) {
                continue;
            }

            // Get the metric name from constructor argument
            if (attribute.ConstructorArguments.Length is 0 ||
                attribute.ConstructorArguments[0].Value is not string metricName) {
                continue;
            }

            if (string.IsNullOrWhiteSpace(metricName)) {
                continue; // Empty names are caught by other analyzers
            }

            if (!ValidNamePattern.IsMatch(metricName)) {
                var location = attribute.ApplicationSyntaxReference?.GetSyntax(context.CancellationToken)
                    .GetLocation() ?? method.Locations.FirstOrDefault();

                if (location is not null) {
                    context.ReportDiagnostic(Diagnostic.Create(
                        Rule,
                        location,
                        metricName));
                }
            }
        }
    }
}
