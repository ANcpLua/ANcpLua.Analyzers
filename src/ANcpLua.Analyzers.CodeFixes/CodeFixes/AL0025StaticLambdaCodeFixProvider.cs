using ANcpLua.Analyzers.Core;

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

    public override async Task RegisterCodeFixesAsync(CodeFixContext context) {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null) {
            return;
        }

        foreach (var diagnostic in context.Diagnostics) {
            // The diagnostic is reported on the arrow token, so we need to find the parent lambda
            var node = root.FindNode(diagnostic.Location.SourceSpan);
            var lambda = node.FirstAncestorOrSelf<AnonymousFunctionExpressionSyntax>()
                         ?? node.Parent as AnonymousFunctionExpressionSyntax;

            if (lambda is null) {
                continue;
            }

            context.RegisterCodeFix(
                CodeAction.Create(
                    CodeFixResources.AL0025CodeFixTitle,
                    _ => MakeStaticAsync(context.Document, lambda, root),
                    nameof(CodeFixResources.AL0025CodeFixTitle)),
                diagnostic);
        }
    }

    private static Task<Document> MakeStaticAsync(
        Document document,
        AnonymousFunctionExpressionSyntax lambda,
        SyntaxNode root) {
        var staticKeyword = SyntaxFactory.Token(SyntaxKind.StaticKeyword)
            .WithTrailingTrivia(SyntaxFactory.Space);

        var newLambda = lambda switch {
            SimpleLambdaExpressionSyntax simple => simple.AddModifiers(staticKeyword),
            ParenthesizedLambdaExpressionSyntax paren => paren.AddModifiers(staticKeyword),
            AnonymousMethodExpressionSyntax anon => anon.AddModifiers(staticKeyword),
            _ => lambda
        };

        var newRoot = root.ReplaceNode(lambda, newLambda);
        return Task.FromResult(document.WithSyntaxRoot(newRoot));
    }
}
