using ANcpLua.Analyzers.Analyzers;

namespace ANcpLua.Analyzers.CodeFixes.CodeFixes;

/// <summary>
///     Code fix provider for AL0124 - removes [AgentTraced] attribute from non-interceptable methods.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(Al0124AgentTracedCodeFixProvider))]
[Shared]
public sealed partial class Al0124AgentTracedCodeFixProvider : CodeFixProvider {
    public override ImmutableArray<string> FixableDiagnosticIds =>
        [Al0124NonInterceptableAgentTracedAnalyzer.DiagnosticId];

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context) {
        if (await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false) is not
            { } root) {
            return;
        }

        foreach (var diagnostic in context.Diagnostics) {
            var token = root.FindToken(diagnostic.Location.SourceSpan.Start);
            if (token.Parent?.FirstAncestorOrSelf<MethodDeclarationSyntax>() is not { } methodDeclaration) {
                continue;
            }

            context.RegisterCodeFix(
                CodeAction.Create(
                    CodeFixResources.AL0124CodeFixTitle,
                    ct => RemoveAgentTracedAttributeAsync(context.Document, methodDeclaration, root, ct),
                    nameof(CodeFixResources.AL0124CodeFixTitle)),
                diagnostic);
        }
    }

    private static Task<Document> RemoveAgentTracedAttributeAsync(
        Document document,
        MethodDeclarationSyntax methodDeclaration,
        SyntaxNode root,
        CancellationToken _) {
        AttributeSyntax? targetAttribute = null;
        AttributeListSyntax? targetList = null;

        foreach (var attrList in methodDeclaration.AttributeLists) {
            foreach (var attr in attrList.Attributes) {
                var name = attr.Name.ToString();
                if (name is "AgentTraced" or "AgentTracedAttribute"
                    or "Qyl.Instrumentation.Instrumentation.AgentTraced"
                    or "Qyl.Instrumentation.Instrumentation.AgentTracedAttribute") {
                    targetAttribute = attr;
                    targetList = attrList;
                    break;
                }
            }

            if (targetAttribute is not null) {
                break;
            }
        }

        if (targetAttribute is null || targetList is null) {
            return Task.FromResult(document);
        }

        SyntaxNode newRoot;
        if (targetList.Attributes.Count == 1) {
            newRoot = root.RemoveNode(targetList, SyntaxRemoveOptions.KeepNoTrivia)!;
        } else {
            newRoot = root.RemoveNode(targetAttribute, SyntaxRemoveOptions.KeepNoTrivia)!;
        }

        return Task.FromResult(document.WithSyntaxRoot(newRoot));
    }
}
