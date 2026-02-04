using ANcpLua.Analyzers.Core;

namespace ANcpLua.Analyzers.CodeFixes.CodeFixes;

/// <summary>
///     Code fix provider for AL0074: Replaces deprecated GenAI attribute names with current ones.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(Al0074DeprecatedGenAiCodeFixProvider))]
[Shared]
public sealed partial class Al0074DeprecatedGenAiCodeFixProvider : CodeFixProvider {
    /// <summary>Gets the diagnostic IDs this provider can fix.</summary>
    public override ImmutableArray<string> FixableDiagnosticIds => [DiagnosticIds.DeprecatedGenAiAttribute];

    /// <summary>Gets the FixAll provider for batch fixing.</summary>
    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    /// <summary>Registers code fixes for the given context.</summary>
    public override async Task RegisterCodeFixesAsync(CodeFixContext context) {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null) {
            return;
        }

        var diagnostic = context.Diagnostics[0];
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        // Find the string literal node
        var node = root.FindNode(diagnosticSpan);
        if (node is not LiteralExpressionSyntax literal) {
            return;
        }

        // Get the replacement from diagnostic properties
        if (!diagnostic.Properties.TryGetValue("Replacement", out var replacement) || replacement is null) {
            return;
        }

        var title = string.Format(CodeFixResources.AL0074CodeFixTitle, replacement);

        context.RegisterCodeFix(
            CodeAction.Create(
                title,
                c => ReplaceAttributeNameAsync(context.Document, literal, replacement, root, c),
                nameof(Al0074DeprecatedGenAiCodeFixProvider)),
            diagnostic);
    }

    private static Task<Document> ReplaceAttributeNameAsync(
        Document document,
        SyntaxNode literal,
        string replacement,
        SyntaxNode root,
        CancellationToken _) {
        // Create new string literal with the replacement value
        var newLiteral = SyntaxFactory.LiteralExpression(
            SyntaxKind.StringLiteralExpression,
            SyntaxFactory.Literal(replacement))
            .WithTriviaFrom(literal);

        var newRoot = root.ReplaceNode(literal, newLiteral);

        return Task.FromResult(document.WithSyntaxRoot(newRoot));
    }
}
