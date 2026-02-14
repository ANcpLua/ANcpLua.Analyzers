using ANcpLua.Analyzers.Analyzers;

namespace ANcpLua.Analyzers.CodeFixes.CodeFixes;

/// <summary>
///     Code fix provider for AL0074: Replaces deprecated GenAI attribute names with current ones.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(Al0074DeprecatedGenAiCodeFixProvider))]
[Shared]
public sealed partial class Al0074DeprecatedGenAiCodeFixProvider
    : AlCodeFixProvider<LiteralExpressionSyntax> {
    /// <summary>Gets the diagnostic IDs this provider can fix.</summary>
    public override ImmutableArray<string> FixableDiagnosticIds => [Al0074DeprecatedGenAiAttributeAnalyzer.DiagnosticId];

    /// <summary>Creates the code action for this fix.</summary>
    protected override CodeAction? CreateCodeAction(
        Document document,
        LiteralExpressionSyntax literal,
        SyntaxNode root,
        Diagnostic diagnostic) {
        if (!diagnostic.Properties.TryGetValue("Replacement", out var replacement) || replacement is null) {
            return null;
        }

        return CodeAction.Create(
            string.Format(CodeFixResources.AL0074CodeFixTitle, replacement),
            _ => ReplaceAttributeNameAsync(document, literal, replacement, root),
            nameof(Al0074DeprecatedGenAiCodeFixProvider));
    }

    private static Task<Document> ReplaceAttributeNameAsync(
        Document document,
        SyntaxNode literal,
        string replacement,
        SyntaxNode root) {
        var newLiteral = SyntaxFactory.LiteralExpression(
            SyntaxKind.StringLiteralExpression,
            SyntaxFactory.Literal(replacement))
            .WithTriviaFrom(literal);

        var newRoot = root.ReplaceNode(literal, newLiteral);
        return Task.FromResult(document.WithSyntaxRoot(newRoot));
    }
}
