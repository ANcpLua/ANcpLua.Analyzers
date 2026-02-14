using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Analyzers.Core;

namespace ANcpLua.Analyzers.CodeFixes.CodeFixes;

/// <summary>
///     Code fix provider for AL0012 - replaces deprecated attributes with modern equivalents.
/// </summary>
/// <remarks>
///     Does not extend <see cref="AlCodeFixProvider{TNode}" /> because the diagnostic location
///     may not directly contain the <see cref="LiteralExpressionSyntax" /> - the literal can be
///     nested within attribute arguments, requiring <c>DescendantNodesAndSelf</c> traversal.
/// </remarks>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(Al0012DeprecatedAttributeCodeFixProvider))]
[Shared]
public sealed partial class Al0012DeprecatedAttributeCodeFixProvider : CodeFixProvider {
    public override ImmutableArray<string> FixableDiagnosticIds =>
        [Al0012DeprecatedAttributeAnalyzer.DiagnosticId];

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context) {
        if (await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false) is not
            { } root) {
            return;
        }

        var diagnostic = context.Diagnostics.First();
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        var node = root.FindNode(diagnosticSpan, getInnermostNodeForTie: true);

        if ((node as LiteralExpressionSyntax
             ?? node.DescendantNodesAndSelf().OfType<LiteralExpressionSyntax>().FirstOrDefault()) is not { } literal) {
            return;
        }

        var deprecatedName = literal.Token.ValueText;

        if (!DeprecatedOtelAttributes.Renames.TryGetValue(deprecatedName, out var replacement)) {
            return;
        }

        context.RegisterCodeFix(
            CodeAction.Create(
                string.Format(CodeFixResources.AL0012CodeFixTitle, replacement.Replacement),
                c => ReplaceAttributeAsync(context.Document, literal, replacement.Replacement, c),
                $"UseModernAttribute_{replacement.Replacement}"),
            diagnostic);
    }

    private static async Task<Document> ReplaceAttributeAsync(
        Document document,
        SyntaxNode oldLiteral,
        string newAttributeName,
        CancellationToken cancellationToken) {
        if (await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false) is not { } root) {
            return document;
        }

        var newLiteral = SyntaxFactory.LiteralExpression(
                SyntaxKind.StringLiteralExpression,
                SyntaxFactory.Literal(newAttributeName))
            .WithTriviaFrom(oldLiteral);

        var newRoot = root.ReplaceNode(oldLiteral, newLiteral);

        return document.WithSyntaxRoot(newRoot);
    }
}
