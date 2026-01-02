using ANcpLua.Analyzers.Analyzers;

namespace ANcpLua.Analyzers.CodeFixes.CodeFixes;

/// <summary>
///     Code fix for AL0004: Converts Span equality to pattern matching.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AL0004CodeFixProvider))]
[Shared]
public sealed class AL0004CodeFixProvider : ALCodeFixProvider<BinaryExpressionSyntax> {
    public override ImmutableArray<string> FixableDiagnosticIds =>
        [AL0004ToAL0005SpanComparisonAnalyzer.DiagnosticIdAL0004];

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
                .Select(e => e.Expression)),
            ArrayCreationExpressionSyntax arr => ToListPattern(arr.Initializer?.Expressions ?? []),
            ImplicitArrayCreationExpressionSyntax imp => ToListPattern(imp.Initializer.Expressions),
            _ => throw new InvalidOperationException("Unexpected syntax kind")
        };

        var isPattern = SyntaxFactory.IsPatternExpression(binary.Left, pattern);
        return Task.FromResult(document.WithSyntaxRoot(root.ReplaceNode(binary, isPattern)));
    }

    private static ListPatternSyntax ToListPattern(IEnumerable<ExpressionSyntax> expressions) =>
        SyntaxFactory.ListPattern(SyntaxFactory.SeparatedList(
            expressions.Select(PatternSyntax (e) => SyntaxFactory.ConstantPattern(e))));
}
