using ANcpLua.Analyzers.Analyzers;

namespace ANcpLua.Analyzers.CodeFixes.CodeFixes;

/// <summary>
///     Code fix for AL1010: Converts equality comparisons to pattern matching.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(Al1010PatternMatchingCodeFixProvider))]
[Shared]
public sealed partial class Al1010PatternMatchingCodeFixProvider : AlCodeFixProvider<BinaryExpressionSyntax> {
    public override ImmutableArray<string> FixableDiagnosticIds =>
        [Al1010PreferPatternMatchingAnalyzer.DiagnosticId];

    protected override CodeAction CreateCodeAction(Document document,
        BinaryExpressionSyntax binary,
        SyntaxNode root,
        Diagnostic diagnostic) =>
        CodeAction.Create(
            CodeFixResources.AL1003CodeFixTitle,
            _ => ConvertToPatternMatching(document, binary, root),
            Al1010PreferPatternMatchingAnalyzer.DiagnosticId);

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
        expression is LiteralExpressionSyntax { Token.Value: var value } &&
        IsZeroValue(value);

    private static bool IsZeroValue(object? value) =>
        value is 0 or 0L or 0U or 0UL or (short)0 or (ushort)0 or (byte)0 or (sbyte)0
            or 0f or 0d or 0m;

    private static PatternSyntax CreatePattern(ExpressionSyntax literal, bool isNegated) {
        PatternSyntax constantPattern = SyntaxFactory.ConstantPattern(literal);

        return isNegated
            ? SyntaxFactory.UnaryPattern(
                SyntaxFactory.Token(SyntaxKind.NotKeyword).WithTrailingTrivia(SyntaxFactory.Space),
                constantPattern)
            : constantPattern;
    }
}
