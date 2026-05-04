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
    private const string CounterAttributeFullName = "Qyl.Instrumentation.Instrumentation.CounterAttribute";
    private const string HistogramAttributeFullName = "Qyl.Instrumentation.Instrumentation.HistogramAttribute";

    // Pattern: lowercase letters, numbers, dots, and underscores only
    // Should have at least one dot (namespace separator)
    private static readonly Regex s_validNamePattern = new(
        @"^[a-z][a-z0-9_.]*\.[a-z][a-z0-9_.]*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>The diagnostic identifier for AL0068.</summary>
    private const string DiagnosticId = "AL0068";

    private static readonly DiagnosticDescriptor s_rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.Metrics,
        DiagnosticSeverities.Suggestion);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_rule];

    /// <summary>Registers symbol actions to analyze methods with metric attributes.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterSymbolAction(AnalyzeMethod, SymbolKind.Method);

    private static void AnalyzeMethod(SymbolAnalysisContext context) {
        var method = (IMethodSymbol)context.Symbol;

        var counterType = context.Compilation.GetTypeByMetadataName(CounterAttributeFullName);
        var histogramType = context.Compilation.GetTypeByMetadataName(HistogramAttributeFullName);

        foreach (var attribute in method.GetAttributes()) {
            if (!attribute.AttributeClass.IsEqualTo(counterType) &&
                !attribute.AttributeClass.IsEqualTo(histogramType)) {
                continue;
            }

            if (attribute.ConstructorArguments.Length is 0 ||
                attribute.ConstructorArguments[0].Value is not string metricName ||
                string.IsNullOrWhiteSpace(metricName) ||
                s_validNamePattern.IsMatch(metricName)) {
                continue;
            }

            var location = attribute.ApplicationSyntaxReference?.GetSyntax(context.CancellationToken)
                .GetLocation() ?? method.Locations.FirstOrDefault();

            if (location is not null) {
                context.ReportDiagnostic(Diagnostic.Create(s_rule, location, metricName));
            }
        }
    }
}
