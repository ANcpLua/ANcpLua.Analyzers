using ANcpLua.Analyzers.Core;

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
    private const string TagAttributeFullName = "qyl.ServiceDefaults.Instrumentation.TagAttribute";
    private const string CounterAttributeFullName = "qyl.ServiceDefaults.Instrumentation.CounterAttribute";
    private const string HistogramAttributeFullName = "qyl.ServiceDefaults.Instrumentation.HistogramAttribute";

    /// <summary>
    ///     Known high-cardinality tag patterns that should be avoided on metrics.
    /// </summary>
    private static readonly string[] HighCardinalityPatterns = [
        "user.id",
        "user_id",
        "userId",
        "request.id",
        "request_id",
        "requestId",
        "session.id",
        "session_id",
        "sessionId",
        "trace.id",
        "trace_id",
        "traceId",
        "span.id",
        "span_id",
        "spanId",
        "correlation.id",
        "correlation_id",
        "correlationId",
        "transaction.id",
        "transaction_id",
        "transactionId",
        "message.id",
        "message_id",
        "messageId",
        "order.id",
        "order_id",
        "orderId",
        "customer.id",
        "customer_id",
        "customerId",
        "account.id",
        "account_id",
        "accountId",
        "email",
        "ip",
        "ip_address",
        "user_agent",
        "url",
        "uri",
        "path",
        "query",
        "timestamp",
        "uuid",
        "guid"
    ];

    private static readonly DiagnosticDescriptor HighCardinalityMetricTagRule = CreateRule(
        DiagnosticIds.HighCardinalityMetricTag,
        DiagnosticCategories.Metrics,
        DiagnosticSeverities.Suggestion);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [HighCardinalityMetricTagRule];

    /// <summary>Registers syntax node actions to analyze parameters with [Tag] attribute.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterSyntaxNodeAction(AnalyzeParameterForHighCardinalityTags, SyntaxKind.Parameter);

    private static void AnalyzeParameterForHighCardinalityTags(SyntaxNodeAnalysisContext context) {
        var parameter = (ParameterSyntax)context.Node;

        // Quick check: skip if no attributes
        if (parameter.AttributeLists.Count is 0) {
            return;
        }

        // Get parameter symbol
        if (context.SemanticModel.GetDeclaredSymbol(parameter, context.CancellationToken) is not { } parameterSymbol) {
            return;
        }

        // Check if this parameter is in a metric method
        if (parameterSymbol.ContainingSymbol is not IMethodSymbol methodSymbol) {
            return;
        }

        if (!HasCounterOrHistogramAttribute(methodSymbol, context.SemanticModel.Compilation)) {
            return;
        }

        // Check if parameter has [Tag] attribute with high-cardinality name
        if (GetTagAttributeNameOrNull(parameterSymbol, context.SemanticModel.Compilation) is not { } tagName) {
            return;
        }

        // Check if tag name matches high-cardinality patterns
        if (MatchesHighCardinalityPattern(tagName)) {
            context.ReportDiagnostic(Diagnostic.Create(
                HighCardinalityMetricTagRule,
                parameter.GetLocation(),
                tagName));
        }
    }

    private static bool HasCounterOrHistogramAttribute(IMethodSymbol methodSymbol, Compilation compilation) {
        var counterAttributeType = compilation.GetTypeByMetadataName(CounterAttributeFullName);
        var histogramAttributeType = compilation.GetTypeByMetadataName(HistogramAttributeFullName);

        foreach (var attribute in methodSymbol.GetAttributes()) {
            if (counterAttributeType is not null &&
                SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, counterAttributeType)) {
                return true;
            }

            if (histogramAttributeType is not null &&
                SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, histogramAttributeType)) {
                return true;
            }
        }

        return false;
    }

    private static string? GetTagAttributeNameOrNull(IParameterSymbol parameterSymbol, Compilation compilation) {
        if (compilation.GetTypeByMetadataName(TagAttributeFullName) is not { } tagAttributeType) {
            return null;
        }

        foreach (var attribute in parameterSymbol.GetAttributes()) {
            if (!SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, tagAttributeType)) {
                continue;
            }

            // Get tag name from constructor argument
            if (attribute.ConstructorArguments.Length > 0 &&
                attribute.ConstructorArguments[0].Value is string tagName) {
                return tagName;
            }
        }

        return null;
    }

    private static bool MatchesHighCardinalityPattern(string tagName) {
        // Normalize tag name for comparison (uppercase for culture-safe comparison)
        var normalizedTag = tagName.ToUpperInvariant();

        foreach (var pattern in HighCardinalityPatterns) {
            var normalizedPattern = pattern.ToUpperInvariant();

            // Exact match
            if (normalizedTag == normalizedPattern) {
                return true;
            }

            // Check if tag ends with pattern (e.g., "my.user.id" ends with "user.id")
            // This is more precise than Contains to avoid false positives like "user.type"
            if (normalizedTag.EndsWithOrdinal("." + normalizedPattern) ||
                normalizedTag.EndsWithOrdinal("_" + normalizedPattern.Replace(".", "_", StringComparison.Ordinal))) {
                return true;
            }

            // For single-word patterns (email, ip, url, etc.), also check if it's a segment
            // e.g., "client.email" should match "email" pattern
            if (!normalizedPattern.ContainsOrdinal(".") &&
                !normalizedPattern.ContainsOrdinal("_")) {
                // Check if it's the last segment after a separator
                if (normalizedTag.EndsWithOrdinal("." + normalizedPattern) ||
                    normalizedTag.EndsWithOrdinal("_" + normalizedPattern) ||
                    normalizedTag == normalizedPattern) {
                    return true;
                }
            }
        }

        return false;
    }
}
