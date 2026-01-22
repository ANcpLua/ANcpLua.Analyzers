namespace ANcpLua.Analyzers.CodeFixes.CodeFixes;

/// <summary>
///     Base class for all ANcpLua code fix providers.
/// </summary>
public abstract partial class AlCodeFixProvider<TNode> : CodeFixProvider where TNode : CSharpSyntaxNode {
    public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context) {
        var diagnostic = context.Diagnostics.First(d => FixableDiagnosticIds.Contains(d.Id));
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

        if (root?.FindNode(diagnostic.Location.SourceSpan) is not TNode declaration) {
            return;
        }

        var action = CreateCodeAction(context.Document, declaration, root, diagnostic);
        if (action is not null) {
            context.RegisterCodeFix(action, diagnostic);
        }
    }

    protected abstract CodeAction? CreateCodeAction(Document document, TNode syntax, SyntaxNode root,
        Diagnostic diagnostic);
}
