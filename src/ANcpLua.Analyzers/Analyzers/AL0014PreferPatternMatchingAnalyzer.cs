using ANcpLua.Analyzers.Core;

namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0014: Prefer pattern matching over equality operators for null and zero comparisons.
/// </summary>
/// <remarks>
///     <list type="bullet">
///         <item><c>x == null</c> → <c>x is null</c></item>
///         <item><c>x != null</c> → <c>x is not null</c></item>
///         <item><c>x == 0</c> → <c>x is 0</c></item>
///         <item><c>x != 0</c> → <c>x is not 0</c></item>
///     </list>
///     Pattern matching syntax is more expressive and, for null checks,
///     bypasses any overloaded equality operators ensuring true reference comparison.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AL0014PreferPatternMatchingAnalyzer : ALAnalyzer
{
    public const string DiagnosticId = DiagnosticIds.PreferPatternMatchingForNullAndZero;

    internal const string PropertyIsNullCheck = "IsNullCheck";
    internal const string PropertyIsNegated = "IsNegated";
    internal const string PropertyExpressionIsLeft = "ExpressionIsLeft";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Prefer pattern matching for null and zero comparisons",
        "Use '{0}' instead of '{1}'",
        DiagnosticCategories.Style,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "Pattern matching syntax (is/is not) is more expressive and idiomatic. " +
                     "For null checks, it also bypasses overloaded equality operators.",
        helpLinkUri: HelpLinkBase + "AL0014.md");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    protected override void RegisterActions(AnalysisContext context)
    {
        context.RegisterSyntaxNodeAction(
            AnalyzeBinaryExpression,
            SyntaxKind.EqualsExpression,
            SyntaxKind.NotEqualsExpression);
    }

    private static void AnalyzeBinaryExpression(SyntaxNodeAnalysisContext context)
    {
        var binary = (BinaryExpressionSyntax)context.Node;

        if (IsInsidePatternContext(binary))
            return;

        if (!TryGetComparisonInfo(binary, out var isNullCheck, out var expressionIsLeft))
            return;

        var isNegated = binary.IsKind(SyntaxKind.NotEqualsExpression);
        var expression = expressionIsLeft ? binary.Left : binary.Right;
        var literal = expressionIsLeft ? binary.Right : binary.Left;

        var originalText = $"{expression} {binary.OperatorToken} {literal}";
        var patternKeyword = isNegated ? "is not" : "is";
        var suggestedText = $"{expression} {patternKeyword} {literal}";

        var properties = ImmutableDictionary.CreateBuilder<string, string?>();
        properties.Add(PropertyIsNullCheck, isNullCheck.ToString());
        properties.Add(PropertyIsNegated, isNegated.ToString());
        properties.Add(PropertyExpressionIsLeft, expressionIsLeft.ToString());

        context.ReportDiagnostic(Diagnostic.Create(
            Rule,
            binary.GetLocation(),
            properties.ToImmutable(),
            suggestedText,
            originalText));
    }

    private static bool TryGetComparisonInfo(
        BinaryExpressionSyntax binary,
        out bool isNullCheck,
        out bool expressionIsLeft)
    {
        isNullCheck = false;
        expressionIsLeft = false;

        // Check: expression == null, null == expression
        if (IsNullLiteral(binary.Right))
        {
            isNullCheck = true;
            expressionIsLeft = true;
            return true;
        }

        if (IsNullLiteral(binary.Left))
        {
            isNullCheck = true;
            expressionIsLeft = false;
            return true;
        }

        // Check: expression == 0, 0 == expression
        if (IsZeroLiteral(binary.Right))
        {
            isNullCheck = false;
            expressionIsLeft = true;
            return true;
        }

        if (IsZeroLiteral(binary.Left))
        {
            isNullCheck = false;
            expressionIsLeft = false;
            return true;
        }

        return false;
    }

    private static bool IsInsidePatternContext(SyntaxNode node)
    {
        for (var current = node.Parent; current is not null; current = current.Parent)
        {
            if (current is IsPatternExpressionSyntax or SwitchExpressionSyntax or CasePatternSwitchLabelSyntax)
                return true;
        }

        return false;
    }

    private static bool IsNullLiteral(ExpressionSyntax expression) =>
        expression.IsKind(SyntaxKind.NullLiteralExpression);

    private static bool IsZeroLiteral(ExpressionSyntax expression) =>
        expression is LiteralExpressionSyntax { Token.ValueText: "0" };
}
