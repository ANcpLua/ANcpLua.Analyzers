using ANcpLua.Analyzers.Core;

namespace ANcpLua.Analyzers.CodeFixes.CodeFixes;

/// <summary>
///     Code fix provider for AL0012 - replaces deprecated attributes with modern equivalents.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AL0012DeprecatedAttributeCodeFixProvider))]
[Shared]
public sealed class AL0012DeprecatedAttributeCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds =>
        [DiagnosticIds.DeprecatedSemanticConventionAttribute];

    public override FixAllProvider GetFixAllProvider()
    {
        return WellKnownFixAllProviders.BatchFixer;
    }

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
            .ConfigureAwait(false);

        if (root is null)
            return;

        var diagnostic = context.Diagnostics.First();
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        var node = root.FindNode(diagnosticSpan, getInnermostNodeForTie: true);

        var literal = node as LiteralExpressionSyntax
                      ?? node.DescendantNodesAndSelf().OfType<LiteralExpressionSyntax>().FirstOrDefault();

        if (literal is null)
            return;

        var deprecatedName = literal.Token.ValueText;

        if (!DeprecatedOtelAttributes.Renames.TryGetValue(deprecatedName, out var replacement))
            return;

        context.RegisterCodeFix(
            CodeAction.Create(
                string.Format(CodeFixResources.AL0012CodeFixTitle, replacement.Replacement),
                c => ReplaceAttributeAsync(context.Document, literal, replacement.Replacement, c),
                $"UseModernAttribute_{replacement.Replacement}"),
            diagnostic);
    }

    private static async Task<Document> ReplaceAttributeAsync(
        Document document,
        LiteralExpressionSyntax oldLiteral,
        string newAttributeName,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        if (root is null)
            return document;

        var newLiteral = SyntaxFactory.LiteralExpression(
                SyntaxKind.StringLiteralExpression,
                SyntaxFactory.Literal(newAttributeName))
            .WithTriviaFrom(oldLiteral);

        var newRoot = root.ReplaceNode(oldLiteral, newLiteral);

        return document.WithSyntaxRoot(newRoot);
    }
}