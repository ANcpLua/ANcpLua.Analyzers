using ANcpLua.Analyzers.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Composition;
using System.Threading.Tasks;

namespace ANcpLua.Analyzers.CodeFixes.CodeFixes;

/// <summary>
///     Code fix provider for AL0010 - adds partial modifier to types.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AL0010PartialTypeCodeFixProvider))]
[Shared]
public sealed class AL0010PartialTypeCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds =>
        [DiagnosticIds.TypeShouldBePartial];

    public override FixAllProvider GetFixAllProvider()
    {
        return WellKnownFixAllProviders.BatchFixer;
    }

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null)
            return;

        foreach (var diagnostic in context.Diagnostics)
        {
            var node = root.FindNode(diagnostic.Location.SourceSpan);
            var typeDeclaration = node.FirstAncestorOrSelf<TypeDeclarationSyntax>();

            if (typeDeclaration is null)
                continue;

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
        SyntaxNode root)
    {
        var partialToken = SyntaxFactory.Token(SyntaxKind.PartialKeyword)
            .WithTrailingTrivia(SyntaxFactory.Space);

        var newModifiers = typeDeclaration.Modifiers.Add(partialToken);
        var newTypeDeclaration = typeDeclaration.WithModifiers(newModifiers);

        var newRoot = root.ReplaceNode(typeDeclaration, newTypeDeclaration);
        return Task.FromResult(document.WithSyntaxRoot(newRoot));
    }
}