namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0134: Detects telemetry attribute keys that are not part of the official OpenTelemetry semantic conventions.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0134UnknownTelemetryAttributeKeyAnalyzer : AlAnalyzer {
    private const string DiagnosticId = "AL0134";

    private static readonly DiagnosticDescriptor Rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.OpenTelemetry,
        DiagnosticSeverity.Warning);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterSyntaxNodeAction(AnalyzeStringLiteral, SyntaxKind.StringLiteralExpression);

    private static void AnalyzeStringLiteral(SyntaxNodeAnalysisContext context) {
        var literal = (LiteralExpressionSyntax)context.Node;
        var key = literal.Token.ValueText;

        if (string.IsNullOrWhiteSpace(key)) {
            return;
        }

        if (!TryGetTelemetryKeyContext(literal, out _)) {
            return;
        }

        if (OpenTelemetryOfficialSemconvCatalog.IsOfficialAttributeKey(key)) {
            return;
        }

        context.ReportDiagnostic(Rule, literal.GetLocation(), key);
    }

    private static bool TryGetTelemetryKeyContext(LiteralExpressionSyntax literal, out string? methodOrContainerName) {
        methodOrContainerName = null;

        if (literal.Parent is ArgumentSyntax argument &&
            argument.Parent is ArgumentListSyntax argumentList &&
            argumentList.Parent is InvocationExpressionSyntax invocation &&
            argumentList.Arguments.Count > 0 &&
            argumentList.Arguments[0] == argument &&
            IsTelemetryInvocation(invocation, out methodOrContainerName)) {
            return true;
        }

        if (literal.Parent is ArgumentSyntax elementArgument &&
            elementArgument.Parent is BracketedArgumentListSyntax bracketed &&
            bracketed.Parent is ElementAccessExpressionSyntax elementAccess &&
            bracketed.Arguments.Count > 0 &&
            bracketed.Arguments[0] == elementArgument &&
            GetIdentifierName(elementAccess.Expression) is { } identifier &&
            IsLikelyTelemetryContainer(identifier)) {
            methodOrContainerName = identifier;
            return true;
        }

        return false;
    }

    private static bool IsTelemetryInvocation(InvocationExpressionSyntax invocation, out string? methodOrContainerName) {
        methodOrContainerName = GetMethodName(invocation);
        if (methodOrContainerName is null) {
            return false;
        }

        if (methodOrContainerName is "SetTag" or "AddTag" or "SetAttribute" or "AddAttribute" or "SetCustomProperty") {
            return true;
        }

        if (invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
            memberAccess.Name.Identifier.Text == "Add" &&
            GetIdentifierName(memberAccess.Expression) is { } identifier &&
            IsLikelyTelemetryContainer(identifier)) {
            methodOrContainerName = identifier;
            return true;
        }

        return false;
    }

    private static bool IsLikelyTelemetryContainer(string identifier) =>
        identifier.ContainsIgnoreCase("ATTRIBUTE")
        || identifier.ContainsIgnoreCase("TAG")
        || identifier.ContainsIgnoreCase("ATTR")
        || identifier.ContainsIgnoreCase("SPAN")
        || identifier.ContainsIgnoreCase("ACTIVITY")
        || identifier.EqualsIgnoreCase("ATTRS");

    private static string? GetMethodName(InvocationExpressionSyntax invocation) =>
        invocation.Expression switch {
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.Text,
            IdentifierNameSyntax identifier => identifier.Identifier.Text,
            _ => null
        };

    private static string? GetIdentifierName(ExpressionSyntax expression) =>
        expression switch {
            IdentifierNameSyntax identifier => identifier.Identifier.Text,
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.Text,
            _ => null
        };
}
