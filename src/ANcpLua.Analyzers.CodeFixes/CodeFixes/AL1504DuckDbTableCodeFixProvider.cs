using ANcpLua.Analyzers.Analyzers;

namespace ANcpLua.Analyzers.CodeFixes.CodeFixes;

/// <summary>
///     Code fix provider for AL1504 - adds partial modifier to [DuckDbTable] types.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(Al1504DuckDbTableCodeFixProvider))]
[Shared]
public sealed partial class Al1504DuckDbTableCodeFixProvider : CodeFixProvider {
    public override ImmutableArray<string> FixableDiagnosticIds =>
        [Al1504DuckDbTableMustBePartialAnalyzer.DiagnosticId];

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context) {
        if (await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false) is not
            { } root) {
            return;
        }

        foreach (var diagnostic in context.Diagnostics) {
            var node = root.FindNode(diagnostic.Location.SourceSpan);
            if (node.FirstAncestorOrSelf<TypeDeclarationSyntax>() is not { } typeDeclaration) {
                continue;
            }

            context.RegisterCodeFix(
                CodeAction.Create(
                    CodeFixResources.AL1504CodeFixTitle,
                    _ => MakePartialAsync(context.Document, typeDeclaration, root),
                    diagnostic.Id),
                diagnostic);
        }
    }

    private static Task<Document> MakePartialAsync(
        Document document,
        TypeDeclarationSyntax typeDeclaration,
        SyntaxNode root) {
        var partialToken = SyntaxFactory.Token(SyntaxKind.PartialKeyword)
            .WithTrailingTrivia(SyntaxFactory.Space);

        var newModifiers = typeDeclaration.Modifiers.Add(partialToken);
        var newTypeDeclaration = typeDeclaration.WithModifiers(newModifiers);

        var newRoot = root.ReplaceNode(typeDeclaration, newTypeDeclaration);
        return Task.FromResult(document.WithSyntaxRoot(newRoot));
    }
}
