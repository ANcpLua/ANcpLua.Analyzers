using ANcpLua.Analyzers.Analyzers;

namespace ANcpLua.Analyzers.CodeFixes.CodeFixes;

/// <summary>
///     Code fix for AL0014: Converts equality comparisons to pattern matching.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(Al0014CodeFixProvider))]
[Shared]
public sealed partial class Al0014CodeFixProvider : AlCodeFixProvider<BinaryExpressionSyntax> {
    public override ImmutableArray<string> FixableDiagnosticIds =>
        [Al0014PreferPatternMatchingAnalyzer.DiagnosticId];

    protected override CodeAction CreateCodeAction(Document document,
        BinaryExpressionSyntax binary,
        SyntaxNode root,
        Diagnostic diagnostic) =>
        CodeAction.Create(
            "Use pattern matching",
            _ => ConvertToPatternMatching(document, binary, root),
            nameof(Al0014CodeFixProvider));

    private static Task<Document> ConvertToPatternMatching(
        Document document,
        BinaryExpressionSyntax binary,
        SyntaxNode root) {
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
        BinaryExpressionSyntax binary) {
        var leftIsLiteral = IsLiteral(binary.Left);
        return leftIsLiteral
            ? (binary.Right, binary.Left)
            : (binary.Left, binary.Right);
    }

    private static bool IsLiteral(SyntaxNode expression) =>
        expression.IsKind(SyntaxKind.NullLiteralExpression) ||
        expression is LiteralExpressionSyntax { Token.ValueText: "0" };

    private static PatternSyntax CreatePattern(ExpressionSyntax literal, bool isNegated) {
        PatternSyntax constantPattern = SyntaxFactory.ConstantPattern(literal);

        return isNegated
            ? SyntaxFactory.UnaryPattern(
                SyntaxFactory.Token(SyntaxKind.NotKeyword).WithTrailingTrivia(SyntaxFactory.Space),
                constantPattern)
            : constantPattern;
    }
}
