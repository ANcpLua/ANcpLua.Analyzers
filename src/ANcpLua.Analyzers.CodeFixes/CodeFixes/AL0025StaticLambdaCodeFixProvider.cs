using ANcpLua.Analyzers.Core;

namespace ANcpLua.Analyzers.CodeFixes.CodeFixes;

/// <summary>
///     Code fix for AL0025: Makes lambda/anonymous function static.
///     Provides Fix All support for applying across document/project/solution.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AL0025StaticLambdaCodeFixProvider))]
[Shared]
public sealed class AL0025StaticLambdaCodeFixProvider : CodeFixProvider {
    private static readonly SyntaxAnnotation Marker = new("AL0025_ToFix");

    public override ImmutableArray<string> FixableDiagnosticIds => [DiagnosticIds.PreferStaticLambda];

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context) {
        if (await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false) is not { } root) {
            return;
        }

        foreach (var diagnostic in context.Diagnostics) {
            var node = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
            var lambda = node as AnonymousFunctionExpressionSyntax
                         ?? node.FirstAncestorOrSelf<AnonymousFunctionExpressionSyntax>();

            if (lambda is null) {
                continue;
            }

            context.RegisterCodeFix(
                CodeAction.Create(
                    CodeFixResources.AL0025CodeFixTitle,
                    ct => MakeStaticAsync(context.Document, lambda, ct),
                    nameof(CodeFixResources.AL0025CodeFixTitle)),
                diagnostic);
        }
    }

    private static async Task<Document> MakeStaticAsync(
        Document document,
        AnonymousFunctionExpressionSyntax lambdaToMark,
        CancellationToken cancellationToken) {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null) {
            return document;
        }

        // Mark the lambda so we can find it after tree modification
        var annotatedLambda = lambdaToMark.WithAdditionalAnnotations(Marker);
        var annotatedRoot = root.ReplaceNode(lambdaToMark, annotatedLambda);
        var annotatedDoc = document.WithSyntaxRoot(annotatedRoot);

        // Get fresh root and find the marked node
        var freshRoot = await annotatedDoc.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (freshRoot is null) {
            return document;
        }

        var markedLambda = freshRoot.GetAnnotatedNodes(Marker).OfType<AnonymousFunctionExpressionSyntax>().FirstOrDefault();
        if (markedLambda is null) {
            return document;
        }

        var staticKeyword = SyntaxFactory.Token(SyntaxKind.StaticKeyword)
            .WithTrailingTrivia(SyntaxFactory.Space);

        AnonymousFunctionExpressionSyntax newLambda = markedLambda switch {
            SimpleLambdaExpressionSyntax simple => simple.AddModifiers(staticKeyword),
            ParenthesizedLambdaExpressionSyntax paren => paren.AddModifiers(staticKeyword),
            AnonymousMethodExpressionSyntax anon => anon.AddModifiers(staticKeyword),
            _ => markedLambda
        };

        // Remove the marker annotation from the final result
        newLambda = newLambda.WithoutAnnotations(Marker);

        var finalRoot = freshRoot.ReplaceNode(markedLambda, newLambda);
        return annotatedDoc.WithSyntaxRoot(finalRoot);
    }
}
