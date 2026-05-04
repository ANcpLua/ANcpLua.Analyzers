
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
    /// <summary>The diagnostic identifier for AL0072.</summary>
    public const string DiagnosticId = "AL0072";

    private const string CounterAttributeFullName = "Qyl.Instrumentation.Instrumentation.CounterAttribute";
    private const string HistogramAttributeFullName = "Qyl.Instrumentation.Instrumentation.HistogramAttribute";

    private static readonly DiagnosticDescriptor s_rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.Metrics,
        DiagnosticSeverities.RequiredFix);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_rule];

    /// <summary>Registers syntax node actions to analyze method declarations with metric attributes.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterSyntaxNodeAction(AnalyzeMethodDeclaration, SyntaxKind.MethodDeclaration);

    private static void AnalyzeMethodDeclaration(SyntaxNodeAnalysisContext context) {
        var methodDeclaration = (MethodDeclarationSyntax)context.Node;

        if (methodDeclaration.AttributeLists.Count is 0) {
            return;
        }

        if (context.SemanticModel.GetDeclaredSymbol(methodDeclaration, context.CancellationToken) is not { } methodSymbol
            || GetMetricAttributeName(methodSymbol, context.SemanticModel.Compilation) is not { } metricAttributeName) {
            return;
        }

        if (!methodDeclaration.Modifiers.Any(SyntaxKind.PartialKeyword)) {
            context.ReportDiagnostic(Diagnostic.Create(
                s_rule,
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
                attribute.AttributeClass.IsEqualTo(counterAttributeType)) {
                return "Counter";
            }

            if (histogramAttributeType is not null &&
                attribute.AttributeClass.IsEqualTo(histogramAttributeType)) {
                return "Histogram";
            }
        }

        return null;
    }
}
