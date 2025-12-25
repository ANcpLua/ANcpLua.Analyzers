using ANcpLua.Analyzers.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;

namespace ANcpLua.Analyzers.CodeFixes.CodeFixes;

/// <summary>
///     Code fix provider for AL0008 - makes GetSchema return null with expression body.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AL0008IXmlSerializableCodeFixProvider))]
[Shared]
public sealed class AL0008IXmlSerializableCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds =>
        [DiagnosticIds.GetSchemaMustReturnNull];

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
            if (diagnostic.Id != DiagnosticIds.GetSchemaMustReturnNull)
                continue;

            var node = root.FindNode(diagnostic.Location.SourceSpan);
            var target = node as CSharpSyntaxNode
                         ?? node.FirstAncestorOrSelf<MethodDeclarationSyntax>() as CSharpSyntaxNode
                         ?? node.FirstAncestorOrSelf<BlockSyntax>() as CSharpSyntaxNode
                         ?? node.FirstAncestorOrSelf<ArrowExpressionClauseSyntax>();

            if (target is null)
                continue;

            context.RegisterCodeFix(
                CodeAction.Create(
                    CodeFixResources.AL0008CodeFixTitle,
                    _ => FixAsync(context.Document, target, root),
                    nameof(CodeFixResources.AL0008CodeFixTitle)),
                diagnostic);
        }
    }

    private static Task<Document> FixAsync(Document document, CSharpSyntaxNode node, SyntaxNode root)
    {
        var newRoot = node switch
        {
            MethodDeclarationSyntax method when method.Modifiers.Any(SyntaxKind.AbstractKeyword)
                => RemoveAbstractAndAddNullBody(method, root),
            BlockSyntax block => ReplaceBlockWithNullArrow(block, root),
            ArrowExpressionClauseSyntax arrow => ReplaceArrowWithNull(arrow, root),
            _ => root
        };

        return Task.FromResult(document.WithSyntaxRoot(newRoot));
    }

    private static SyntaxNode RemoveAbstractAndAddNullBody(MethodDeclarationSyntax method, SyntaxNode root)
    {
        var abstractKeyword = method.Modifiers.First(t => t.IsKind(SyntaxKind.AbstractKeyword));
        var newModifiers = method.Modifiers.Remove(abstractKeyword);

        var newMethod = method
            .WithModifiers(newModifiers)
            .WithSemicolonToken(default)
            .WithExpressionBody(CreateNullArrowExpression())
            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
            .WithLeadingTrivia(method.GetLeadingTrivia())
            .WithTrailingTrivia(method.GetTrailingTrivia());

        return root.ReplaceNode(method, newMethod);
    }

    private static SyntaxNode ReplaceBlockWithNullArrow(BlockSyntax block, SyntaxNode root)
    {
        if (block.Parent is not MethodDeclarationSyntax method)
            return root;

        var newMethod = method
            .WithBody(null)
            .WithExpressionBody(CreateNullArrowExpression())
            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
            .WithLeadingTrivia(method.GetLeadingTrivia())
            .WithTrailingTrivia(method.GetTrailingTrivia());

        return root.ReplaceNode(method, newMethod);
    }

    private static SyntaxNode ReplaceArrowWithNull(ArrowExpressionClauseSyntax arrow, SyntaxNode root)
    {
        return root.ReplaceNode(arrow, CreateNullArrowExpression());
    }

    private static ArrowExpressionClauseSyntax CreateNullArrowExpression()
    {
        return SyntaxFactory.ArrowExpressionClause(
            SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression));
    }
}