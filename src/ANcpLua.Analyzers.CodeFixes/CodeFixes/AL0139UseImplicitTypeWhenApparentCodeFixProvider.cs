using ANcpLua.Analyzers.Analyzers;

namespace ANcpLua.Analyzers.CodeFixes.CodeFixes;

/// <summary>
///     Code fix for AL0139: converts explicit local types to <c>var</c>.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(Al0139UseImplicitTypeWhenApparentCodeFixProvider))]
[Shared]
public sealed partial class Al0139UseImplicitTypeWhenApparentCodeFixProvider
    : AlCodeFixProvider<TypeSyntax> {
    private const string UseImplicitTypeTitle = "Use implicit type";

    /// <summary>Gets the diagnostic IDs this provider can fix.</summary>
    public override ImmutableArray<string> FixableDiagnosticIds =>
        [Al0139UseImplicitTypeWhenApparentAnalyzer.DiagnosticId];

    /// <inheritdoc />
    protected override CodeAction? CreateCodeAction(
        Document document,
        TypeSyntax syntax,
        SyntaxNode root,
        Diagnostic diagnostic) =>
        CodeAction.Create(
            UseImplicitTypeTitle,
            _ => UseImplicitType(document, root, syntax),
            UseImplicitTypeTitle);

    private static Task<Document> UseImplicitType(
        Document document,
        SyntaxNode root,
        TypeSyntax typeSyntax) {
        var implicitType = SyntaxFactory.IdentifierName(SyntaxFacts.GetText(SyntaxKind.VarKeyword))
            .WithTriviaFrom(typeSyntax);

        return Task.FromResult(document.WithSyntaxRoot(root.ReplaceNode(typeSyntax, implicitType)));
    }
}
