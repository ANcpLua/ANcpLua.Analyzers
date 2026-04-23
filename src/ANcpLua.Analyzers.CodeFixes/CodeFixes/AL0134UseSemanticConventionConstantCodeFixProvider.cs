using ANcpLua.Analyzers.Analyzers;

namespace ANcpLua.Analyzers.CodeFixes.CodeFixes;

/// <summary>
///     Code fix for AL0134 — replaces a hardcoded semantic convention string literal with
///     the matching typed constant reference. The constant's fully-scoped name is carried
///     from the analyzer via <see cref="Diagnostic.Properties"/> so the fix does not need
///     to rebuild the catalog.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(Al0134UseSemanticConventionConstantCodeFixProvider))]
[Shared]
public sealed partial class Al0134UseSemanticConventionConstantCodeFixProvider : CodeFixProvider {
    public override ImmutableArray<string> FixableDiagnosticIds =>
        [Al0134UseSemanticConventionConstantAnalyzer.DiagnosticId];

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context) {
        if (await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false) is not { } root) {
            return;
        }

        var diagnostic = context.Diagnostics.First();

        if (!diagnostic.Properties.TryGetValue(
                Al0134UseSemanticConventionConstantAnalyzer.ConstantPropertyKey,
                out var qualified)
            || string.IsNullOrEmpty(qualified)) {
            return;
        }

        var node = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);

        if ((node as LiteralExpressionSyntax
             ?? node.DescendantNodesAndSelf().OfType<LiteralExpressionSyntax>().FirstOrDefault()) is not { } literal) {
            return;
        }

        context.RegisterCodeFix(
            CodeAction.Create(
                string.Format(CodeFixResources.AL0134CodeFixTitle, qualified),
                ct => ReplaceAsync(context.Document, literal, qualified!, ct),
                $"{Al0134UseSemanticConventionConstantAnalyzer.DiagnosticId}_{qualified}"),
            diagnostic);
    }

    private static async Task<Document> ReplaceAsync(
        Document document,
        LiteralExpressionSyntax literal,
        string qualified,
        CancellationToken cancellationToken) {
        if (await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false) is not { } root) {
            return document;
        }

        var replacement = SyntaxFactory.ParseExpression(qualified).WithTriviaFrom(literal);
        var newRoot = root.ReplaceNode(literal, replacement);
        return document.WithSyntaxRoot(newRoot);
    }
}
