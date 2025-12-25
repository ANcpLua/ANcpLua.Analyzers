using ANcpLua.Analyzers.Core;

namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0012: Detects usage of deprecated OpenTelemetry semantic convention attributes.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AL0012DeprecatedAttributeAnalyzer : ALAnalyzer {
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

    protected override void RegisterActions(AnalysisContext context) {
        context.RegisterSyntaxNodeAction(AnalyzeStringLiteral, SyntaxKind.StringLiteralExpression);
    }

    private static void AnalyzeStringLiteral(SyntaxNodeAnalysisContext context) {
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

    private static bool IsInTelemetryContext(SyntaxNode node) {
        var current = node.Parent;

        while (current is not null) {
            if (IsTelemetryElementAccess(current) ||
                IsTelemetryInvocation(current) ||
                IsTelemetryInitializer(current) ||
                current is AssignmentExpressionSyntax { Parent: InitializerExpressionSyntax }) {
                return true;
            }

            current = current.Parent;
        }

        return false;
    }

    private static bool IsTelemetryElementAccess(SyntaxNode node) {
        return node is ElementAccessExpressionSyntax elementAccess &&
               GetIdentifierName(elementAccess.Expression) is { } identifier &&
               IsLikelyTelemetryContainer(identifier);
    }

    private static bool IsTelemetryInvocation(SyntaxNode node) {
        return node is InvocationExpressionSyntax invocation &&
               GetMethodName(invocation) is { } methodName &&
               DeprecatedOtelAttributes.AttributeKeyPatterns.Any(p =>
                   methodName.Contains(p, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsTelemetryInitializer(SyntaxNode node) {
        return node is InitializerExpressionSyntax { Parent: ObjectCreationExpressionSyntax creation } &&
               IsTelemetryTypeName(creation.Type.ToString());
    }

    private static bool IsTelemetryTypeName(string typeName) {
        return typeName.Contains("Tag") ||
               typeName.Contains("Attribute") ||
               typeName.Contains("KeyValuePair");
    }

    private static bool IsLikelyTelemetryContainer(string identifier) {
        var lowerIdentifier = identifier.ToLowerInvariant();
        return lowerIdentifier.Contains("attribute") ||
               lowerIdentifier.Contains("tag") ||
               lowerIdentifier.Contains("attr") ||
               lowerIdentifier == "attrs";
    }

    private static string? GetMethodName(InvocationExpressionSyntax invocation) {
        return invocation.Expression switch {
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.Text,
            IdentifierNameSyntax identifier => identifier.Identifier.Text,
            _ => null
        };
    }

    private static string? GetIdentifierName(ExpressionSyntax expression) {
        return expression switch {
            IdentifierNameSyntax identifier => identifier.Identifier.Text,
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.Text,
            _ => null
        };
    }
}
