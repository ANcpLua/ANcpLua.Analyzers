namespace ANcpLua.Analyzers.CodeFixes.CodeFixes;

/// <summary>
/// Base class for all ANcpLua code fix providers.
/// </summary>
public abstract class ALCodeFixProvider<TNode> : CodeFixProvider where TNode : CSharpSyntaxNode
{
    public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var diagnostic = context.Diagnostics.Single(d => d.Id == FixableDiagnosticIds.Single());
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        
        if (root is null)
            return;

        var declaration = root.FindNode(diagnostic.Location.SourceSpan) as TNode;
        if (declaration is null)
            return;

        var action = CreateCodeAction(context.Document, declaration, root);
        context.RegisterCodeFix(action, diagnostic);
    }

    protected abstract CodeAction CreateCodeAction(Document document, TNode syntax, SyntaxNode root);
}
