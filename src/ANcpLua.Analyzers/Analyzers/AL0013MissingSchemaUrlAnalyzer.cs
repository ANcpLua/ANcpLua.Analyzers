using ANcpLua.Analyzers.Core;

namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
/// AL0013: Detects OpenTelemetry configurations that don't set the schema URL.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AL0013MissingSchemaUrlAnalyzer : ALAnalyzer
{
    private static readonly LocalizableResourceString Title = new(
        nameof(Resources.AL0013AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
    private static readonly LocalizableResourceString MessageFormat = new(
        nameof(Resources.AL0013AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
    private static readonly LocalizableResourceString Description = new(
        nameof(Resources.AL0013AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.MissingTelemetrySchemaUrl,
        Title, MessageFormat, DiagnosticCategories.OpenTelemetry,
        DiagnosticSeverity.Info, isEnabledByDefault: true, Description,
        HelpLinkBase + "AL0013.md");

    private static readonly HashSet<string> ResourceConfigMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        "ConfigureResource",
        "SetResourceBuilder",
        "AddResource",
        "WithResource",
        "ConfigureOpenTelemetry"
    };

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;

        var methodName = GetMethodName(invocation);
        if (methodName is null || !ResourceConfigMethods.Contains(methodName))
            return;

        if (!IsLikelyOtelBuilderCall(invocation))
            return;

        var hasSchemaUrl = CheckForSchemaUrl(invocation);

        if (hasSchemaUrl) return;

        var location = GetMethodLocation(invocation);
        context.ReportDiagnostic(Rule, location);
    }

    private static bool IsLikelyOtelBuilderCall(InvocationExpressionSyntax invocation)
    {
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            return false;

        var receiverText = memberAccess.Expression.ToString().ToLowerInvariant();
        return receiverText.Contains("builder") ||
               receiverText.Contains("tracer") ||
               receiverText.Contains("meter") ||
               receiverText.Contains("logger") ||
               receiverText.Contains("otel") ||
               receiverText.Contains("opentelemetry");
    }

    private static bool CheckForSchemaUrl(InvocationExpressionSyntax invocation)
    {
        var descendants = invocation.DescendantNodes();

        foreach (var node in descendants)
        {
            if (node is LiteralExpressionSyntax literal)
            {
                var value = literal.Token.ValueText;
                if (value.Contains("schema", StringComparison.OrdinalIgnoreCase) ||
                    value.Contains("telemetry.schema_url", StringComparison.OrdinalIgnoreCase) ||
                    value.Contains("opentelemetry.io/schemas", StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            if (node is not InvocationExpressionSyntax nestedInvocation) continue;
            var nestedMethod = GetMethodName(nestedInvocation);
            if (nestedMethod?.Contains("Schema", StringComparison.OrdinalIgnoreCase) == true) return true;
        }

        return false;
    }

    private static Location GetMethodLocation(InvocationExpressionSyntax invocation) =>
        invocation.Expression switch
        {
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name.GetLocation(),
            IdentifierNameSyntax identifier => identifier.GetLocation(),
            _ => invocation.GetLocation()
        };

    private static string? GetMethodName(InvocationExpressionSyntax invocation) =>
        invocation.Expression switch
        {
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.Text,
            IdentifierNameSyntax identifier => identifier.Identifier.Text,
            _ => null
        };
}
