using ANcpLua.Analyzers.Core;

namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
/// AL0012: Detects usage of deprecated OpenTelemetry semantic convention attributes.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AL0012DeprecatedAttributeAnalyzer : ALAnalyzer
{
    private static readonly LocalizableResourceString Title = new(
        nameof(Resources.AL0012AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
    private static readonly LocalizableResourceString MessageFormat = new(
        nameof(Resources.AL0012AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
    private static readonly LocalizableResourceString Description = new(
        nameof(Resources.AL0012AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.DeprecatedSemanticConventionAttribute,
        Title, MessageFormat, DiagnosticCategories.OpenTelemetry,
        DiagnosticSeverity.Warning, isEnabledByDefault: true, Description,
        HelpLinkBase + "AL0012.md");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterSyntaxNodeAction(AnalyzeStringLiteral, SyntaxKind.StringLiteralExpression);

    private static void AnalyzeStringLiteral(SyntaxNodeAnalysisContext context)
    {
        var literal = (LiteralExpressionSyntax)context.Node;
        var value = literal.Token.ValueText;

        if (string.IsNullOrEmpty(value))
            return;

        if (!DeprecatedOtelAttributes.Renames.TryGetValue(value, out var replacement))
            return;

        if (!IsInTelemetryContext(literal))
            return;

        context.ReportDiagnostic(Rule, literal.GetLocation(), value, replacement.Version, replacement.Replacement);
    }

    private static bool IsInTelemetryContext(SyntaxNode node)
    {
        var current = node.Parent;

        while (current is not null)
        {
            switch (current)
            {
                case ElementAccessExpressionSyntax elementAccess:
                    var identifier = GetIdentifierName(elementAccess.Expression);
                    if (identifier is not null && IsLikelyTelemetryContainer(identifier)) return true;
                    break;

                case InvocationExpressionSyntax invocation:
                    var methodName = GetMethodName(invocation);
                    if (methodName is not null && DeprecatedOtelAttributes.AttributeKeyPatterns.Any(p =>
                            methodName.Contains(p, StringComparison.OrdinalIgnoreCase)))
                        return true;
                    break;

                case InitializerExpressionSyntax initializer:
                    if (initializer.Parent is ObjectCreationExpressionSyntax creation)
                    {
                        var typeName = creation.Type.ToString();
                        if (typeName.Contains("Tag") || typeName.Contains("Attribute") ||
                            typeName.Contains("KeyValuePair"))
                            return true;
                    }
                    break;

                case AssignmentExpressionSyntax { Parent: InitializerExpressionSyntax }:
                    return true;
            }

            current = current.Parent;
        }

        return false;
    }

    private static bool IsLikelyTelemetryContainer(string identifier)
    {
        var lowerIdentifier = identifier.ToLowerInvariant();
        return lowerIdentifier.Contains("attribute") ||
               lowerIdentifier.Contains("tag") ||
               lowerIdentifier.Contains("attr") ||
               lowerIdentifier == "attrs";
    }

    private static string? GetMethodName(InvocationExpressionSyntax invocation) =>
        invocation.Expression switch
        {
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.Text,
            IdentifierNameSyntax identifier => identifier.Identifier.Text,
            _ => null
        };

    private static string? GetIdentifierName(ExpressionSyntax expression) =>
        expression switch
        {
            IdentifierNameSyntax identifier => identifier.Identifier.Text,
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.Text,
            _ => null
        };
}
