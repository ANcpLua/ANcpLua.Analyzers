using ANcpLua.Analyzers.Analyzers;

namespace ANcpLua.Analyzers.CodeFixes.CodeFixes;

/// <summary>
///     Code fix for AL0014: Converts equality comparisons to pattern matching.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AL0014CodeFixProvider))]
[Shared]
public sealed class AL0014CodeFixProvider : ALCodeFixProvider<BinaryExpressionSyntax>
{
    public override ImmutableArray<string> FixableDiagnosticIds =>
        [AL0014PreferPatternMatchingAnalyzer.DiagnosticId];

    protected override CodeAction CreateCodeAction(
        Document document,
        BinaryExpressionSyntax binary,
        SyntaxNode root,
        Diagnostic diagnostic)
    {
        return CodeAction.Create(
            "Use pattern matching",
            _ => ConvertToPatternMatching(document, binary, root),
            nameof(AL0014CodeFixProvider));
    }

    private static Task<Document> ConvertToPatternMatching(
        Document document,
        BinaryExpressionSyntax binary,
        SyntaxNode root)
    {
        var isNegated = binary.IsKind(SyntaxKind.NotEqualsExpression);
        var (expression, literal) = GetExpressionAndLiteral(binary);

        var pattern = CreatePattern(literal.WithoutTrivia(), isNegated);
        var isPattern = SyntaxFactory.IsPatternExpression(
                expression.WithoutTrivia(),
                SyntaxFactory.Token(SyntaxKind.IsKeyword)
                    .WithLeadingTrivia(SyntaxFactory.Space)
                    .WithTrailingTrivia(SyntaxFactory.Space),
                pattern)
            .WithTriviaFrom(binary);

        var newRoot = root.ReplaceNode(binary, isPattern);
        return Task.FromResult(document.WithSyntaxRoot(newRoot));
    }

    private static (ExpressionSyntax Expression, ExpressionSyntax Literal) GetExpressionAndLiteral(
        BinaryExpressionSyntax binary)
    {
        var leftIsLiteral = IsLiteral(binary.Left);
        return leftIsLiteral
            ? (binary.Right, binary.Left)
            : (binary.Left, binary.Right);
    }

    private static bool IsLiteral(ExpressionSyntax expression) =>
        expression.IsKind(SyntaxKind.NullLiteralExpression) ||
        expression is LiteralExpressionSyntax { Token.ValueText: "0" };

    private static PatternSyntax CreatePattern(ExpressionSyntax literal, bool isNegated)
    {
        PatternSyntax constantPattern = SyntaxFactory.ConstantPattern(literal);

        return isNegated
            ? SyntaxFactory.UnaryPattern(
                SyntaxFactory.Token(SyntaxKind.NotKeyword).WithTrailingTrivia(SyntaxFactory.Space),
                constantPattern)
            : constantPattern;
    }
}
