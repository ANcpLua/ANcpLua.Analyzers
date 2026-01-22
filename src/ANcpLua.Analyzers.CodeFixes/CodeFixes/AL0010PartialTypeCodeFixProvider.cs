using ANcpLua.Analyzers.Core;

namespace ANcpLua.Analyzers.CodeFixes.CodeFixes;

/// <summary>
///     Code fix provider for AL0010 - adds partial modifier to types.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(Al0010PartialTypeCodeFixProvider))]
[Shared]
public sealed partial class Al0010PartialTypeCodeFixProvider : CodeFixProvider {
    public override ImmutableArray<string> FixableDiagnosticIds =>
        [DiagnosticIds.TypeShouldBePartial];

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
                    CodeFixResources.AL0010CodeFixTitle,
                    _ => MakePartialAsync(context.Document, typeDeclaration, root),
                    nameof(CodeFixResources.AL0010CodeFixTitle)),
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
