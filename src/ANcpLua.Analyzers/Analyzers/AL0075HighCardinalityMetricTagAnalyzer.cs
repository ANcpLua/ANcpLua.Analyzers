
namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0075: Warns about high-cardinality tags on metrics.
/// </summary>
/// <remarks>
///     <para>
///         High-cardinality tags (like user.id, request.id, session.id) create a
///         unique time series for each distinct value. This can cause:
///         <list type="bullet">
///             <item>Memory exhaustion in metrics backends (Prometheus, etc.)</item>
///             <item>Increased storage costs</item>
///             <item>Query performance degradation</item>
///             <item>Cardinality explosions that crash collectors</item>
///         </list>
///     </para>
///     <para>
///         Alternatives to high-cardinality metric tags:
///         <list type="bullet">
///             <item>Use span/trace attributes instead (spans are sampled)</item>
///             <item>Aggregate into buckets (e.g., user_type instead of user_id)</item>
///             <item>Use exemplars to link metrics to traces</item>
///         </list>
///     </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0075HighCardinalityMetricTagAnalyzer : AlAnalyzer {
    private enum KnownType { TagAttribute, CounterAttribute, HistogramAttribute }

    private static readonly string[] s_knownTypeNames = [
        "Qyl.Instrumentation.Instrumentation.TagAttribute",
        "Qyl.Instrumentation.Instrumentation.CounterAttribute",
        "Qyl.Instrumentation.Instrumentation.HistogramAttribute"
    ];

    /// <summary>
    ///     Known high-cardinality tag patterns that should be avoided on metrics.
    /// </summary>
    private static readonly string[] s_highCardinalityPatterns = [
        "user.id", "user_id", "userId",
        "request.id", "request_id", "requestId",
        "session.id", "session_id", "sessionId",
        "trace.id", "trace_id", "traceId",
        "span.id", "span_id", "spanId",
        "correlation.id", "correlation_id", "correlationId",
        "transaction.id", "transaction_id", "transactionId",
        "message.id", "message_id", "messageId",
        "order.id", "order_id", "orderId",
        "customer.id", "customer_id", "customerId",
        "account.id", "account_id", "accountId",
        "email", "ip", "ip_address", "user_agent",
        "url", "uri", "path", "query",
        "timestamp", "uuid", "guid"
    ];

    /// <summary>The diagnostic identifier for AL0075.</summary>
    private const string DiagnosticId = "AL0075";

    private static readonly DiagnosticDescriptor s_highCardinalityMetricTagRule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.Metrics,
        DiagnosticSeverities.Suggestion);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_highCardinalityMetricTagRule];

    /// <summary>Registers compilation start action to resolve metric attribute types once.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterCompilationStartAction(OnCompilationStart);

    private static void OnCompilationStart(CompilationStartAnalysisContext context) {
        var cache = new TypeCache<KnownType>(type => context.Compilation.GetTypeByMetadataName(s_knownTypeNames[(int)type]));

        if (cache.Get(KnownType.TagAttribute) is null) {
            return;
        }

        if (cache.Get(KnownType.CounterAttribute) is null && cache.Get(KnownType.HistogramAttribute) is null) {
            return;
        }

        context.RegisterSyntaxNodeAction(
            ctx => AnalyzeParameterForHighCardinalityTags(ctx, cache),
            SyntaxKind.Parameter);
    }

    private static void AnalyzeParameterForHighCardinalityTags(SyntaxNodeAnalysisContext context, TypeCache<KnownType> cache) {
        var parameter = (ParameterSyntax)context.Node;

        if (parameter.AttributeLists.Count is 0) {
            return;
        }

        if (context.SemanticModel.GetDeclaredSymbol(parameter, context.CancellationToken) is not { ContainingSymbol: IMethodSymbol methodSymbol } parameterSymbol
            || (!cache.HasAttribute(methodSymbol, KnownType.CounterAttribute) && !cache.HasAttribute(methodSymbol, KnownType.HistogramAttribute))
            || cache.GetAttribute(parameterSymbol, KnownType.TagAttribute) is not { ConstructorArguments: [{ Value: string tagName }, ..] }) {
            return;
        }

        if (MatchesHighCardinalityPattern(tagName)) {
            context.ReportDiagnostic(Diagnostic.Create(
                s_highCardinalityMetricTagRule,
                parameter.GetLocation(),
                tagName));
        }
    }

    private static bool MatchesHighCardinalityPattern(string tagName) {
        var normalizedTag = tagName.ToUpperInvariant();

        foreach (var pattern in s_highCardinalityPatterns) {
            var normalizedPattern = pattern.ToUpperInvariant();

            if (normalizedTag == normalizedPattern) {
                return true;
            }

            if (normalizedTag.EndsWithOrdinal("." + normalizedPattern) ||
                normalizedTag.EndsWithOrdinal("_" + normalizedPattern.ReplaceOrdinal(".", "_"))) {
                return true;
            }

            if (!normalizedPattern.ContainsOrdinal(".") && !normalizedPattern.ContainsOrdinal("_")
                && (normalizedTag.EndsWithOrdinal("." + normalizedPattern)
                    || normalizedTag.EndsWithOrdinal("_" + normalizedPattern))) {
                return true;
            }
        }

        return false;
    }
}
