using ANcpLua.Analyzers.Analyzers;

namespace ANcpLua.Analyzers.CodeFixes.CodeFixes;

/// <summary>
///     Code fix for AL0004: Converts Span equality to pattern matching.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(Al0004SpanPatternMatchingCodeFixProvider))]
[Shared]
public sealed partial class Al0004SpanPatternMatchingCodeFixProvider : AlCodeFixProvider<BinaryExpressionSyntax> {
    public override ImmutableArray<string> FixableDiagnosticIds =>
        [Al0004ToAl0005SpanComparisonAnalyzer.DiagnosticIdAl0004];

    protected override CodeAction CreateCodeAction(Document document, BinaryExpressionSyntax syntax, SyntaxNode root,
        Diagnostic diagnostic) =>
        CodeAction.Create(
            CodeFixResources.AL0004CodeFixTitle,
            _ => UsePatternMatching(document, syntax, root),
            nameof(CodeFixResources.AL0004CodeFixTitle));

    private static Task<Document> UsePatternMatching(
        Document document,
        BinaryExpressionSyntax binary,
        SyntaxNode root) {
        PatternSyntax pattern = binary.Right switch {
            LiteralExpressionSyntax => SyntaxFactory.ConstantPattern(binary.Right),
            CollectionExpressionSyntax col => ToListPattern(col.Elements.Cast<ExpressionElementSyntax>()
                .Select(static e => e.Expression)),
            ArrayCreationExpressionSyntax arr => ToListPattern(arr.Initializer?.Expressions ??
                                                               Enumerable.Empty<ExpressionSyntax>()),
            ImplicitArrayCreationExpressionSyntax imp => ToListPattern(imp.Initializer.Expressions),
            _ => throw new InvalidOperationException("Unexpected syntax kind")
        };

        // For != comparisons, wrap pattern in "not" pattern
        var isNotEquals = binary.IsKind(SyntaxKind.NotEqualsExpression);
        if (isNotEquals) {
            pattern = SyntaxFactory.UnaryPattern(
                SyntaxFactory.Token(SyntaxKind.NotKeyword),
                pattern);
        }

        var isPattern = SyntaxFactory.IsPatternExpression(binary.Left, pattern)
            .WithTriviaFrom(binary);
        return Task.FromResult(document.WithSyntaxRoot(root.ReplaceNode(binary, isPattern)));
    }

    private static ListPatternSyntax ToListPattern(IEnumerable<ExpressionSyntax> expressions) =>
        SyntaxFactory.ListPattern(SyntaxFactory.SeparatedList(
            expressions.Select(static PatternSyntax (e) => SyntaxFactory.ConstantPattern(e))));
}
