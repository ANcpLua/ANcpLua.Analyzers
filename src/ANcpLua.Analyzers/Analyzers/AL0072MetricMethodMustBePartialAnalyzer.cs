using ANcpLua.Analyzers.Core;

namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0072: Detects [Counter]/[Histogram] methods that are not declared as partial.
/// </summary>
/// <remarks>
///     <para>
///         The source generator requires metric methods to be partial because:
///         <list type="bullet">
///             <item>The generator creates the method implementation</item>
///             <item>The implementation records values to the appropriate instrument</item>
///             <item>Without partial, the method would have no body or conflict with generated code</item>
///         </list>
///     </para>
///     <para>
///         Example of correct usage:
///         <code>
///         [Counter("orders.created")]
///         public static partial void RecordOrderCreated([Tag("status")] string status);
///
///         [Histogram("order.processing.duration", Unit = "ms")]
///         public static partial void RecordProcessingDuration(double duration, [Tag("type")] string orderType);
///         </code>
///     </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0072MetricMethodMustBePartialAnalyzer : AlAnalyzer {
    private const string CounterAttributeFullName = "qyl.ServiceDefaults.Instrumentation.CounterAttribute";
    private const string HistogramAttributeFullName = "qyl.ServiceDefaults.Instrumentation.HistogramAttribute";

    private static readonly DiagnosticDescriptor Rule = CreateRule(
        DiagnosticIds.MetricMethodMustBePartial,
        DiagnosticCategories.Metrics,
        DiagnosticSeverities.RequiredFix);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    /// <summary>Registers syntax node actions to analyze method declarations with metric attributes.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterSyntaxNodeAction(AnalyzeMethodDeclaration, SyntaxKind.MethodDeclaration);

    private static void AnalyzeMethodDeclaration(SyntaxNodeAnalysisContext context) {
        var methodDeclaration = (MethodDeclarationSyntax)context.Node;

        // Quick check: skip if no attributes
        if (methodDeclaration.AttributeLists.Count is 0) {
            return;
        }

        // Check if method has [Counter] or [Histogram] attribute
        if (context.SemanticModel.GetDeclaredSymbol(methodDeclaration, context.CancellationToken) is not { } methodSymbol) {
            return;
        }

        if (GetMetricAttributeName(methodSymbol, context.SemanticModel.Compilation) is not { } metricAttributeName) {
            return;
        }

        // Check for partial modifier
        var hasPartial = methodDeclaration.Modifiers.Any(SyntaxKind.PartialKeyword);

        // Report if missing partial modifier
        if (!hasPartial) {
            context.ReportDiagnostic(Diagnostic.Create(
                Rule,
                methodDeclaration.Identifier.GetLocation(),
                methodSymbol.Name,
                metricAttributeName));
        }
    }

    private static string? GetMetricAttributeName(IMethodSymbol methodSymbol, Compilation compilation) {
        var counterAttributeType = compilation.GetTypeByMetadataName(CounterAttributeFullName);
        var histogramAttributeType = compilation.GetTypeByMetadataName(HistogramAttributeFullName);

        foreach (var attribute in methodSymbol.GetAttributes()) {
            if (counterAttributeType is not null &&
                SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, counterAttributeType)) {
                return "Counter";
            }

            if (histogramAttributeType is not null &&
                SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, histogramAttributeType)) {
                return "Histogram";
            }
        }

        return null;
    }
}
