using ANcpLua.Analyzers.Core;
using Microsoft.CodeAnalysis.Text;

namespace ANcpLua.Analyzers.CodeFixes.CodeFixes;

/// <summary>
///     Code fix for AL0025: Makes lambda/anonymous function static.
///     Provides Fix All support for applying across document/project/solution.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AL0025StaticLambdaCodeFixProvider))]
[Shared]
public sealed class AL0025StaticLambdaCodeFixProvider : CodeFixProvider {
    public override ImmutableArray<string> FixableDiagnosticIds => [DiagnosticIds.PreferStaticLambda];

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override Task RegisterCodeFixesAsync(CodeFixContext context) {
        foreach (var diagnostic in context.Diagnostics) {
            var span = diagnostic.Location.SourceSpan;

            context.RegisterCodeFix(
                CodeAction.Create(
                    CodeFixResources.AL0025CodeFixTitle,
                    ct => MakeStaticAsync(context.Document, span, ct),
                    nameof(CodeFixResources.AL0025CodeFixTitle)),
                diagnostic);
        }

        return Task.CompletedTask;
    }

    private static async Task<Document> MakeStaticAsync(
        Document document,
        TextSpan span,
        CancellationToken cancellationToken) {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null) {
            return document;
        }

        var node = root.FindNode(span, getInnermostNodeForTie: true);
        var lambda = node as AnonymousFunctionExpressionSyntax
                     ?? node.FirstAncestorOrSelf<AnonymousFunctionExpressionSyntax>();

        if (lambda is null) {
            return document;
        }

        var staticKeyword = SyntaxFactory.Token(SyntaxKind.StaticKeyword)
            .WithTrailingTrivia(SyntaxFactory.Space);

        AnonymousFunctionExpressionSyntax newLambda = lambda switch {
            SimpleLambdaExpressionSyntax simple => simple.AddModifiers(staticKeyword),
            ParenthesizedLambdaExpressionSyntax paren => paren.AddModifiers(staticKeyword),
            AnonymousMethodExpressionSyntax anon => anon.AddModifiers(staticKeyword),
            _ => lambda
        };

        var newRoot = root.ReplaceNode(lambda, newLambda);
        return document.WithSyntaxRoot(newRoot);
    }
}
