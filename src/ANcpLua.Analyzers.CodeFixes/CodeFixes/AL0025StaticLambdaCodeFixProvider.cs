using ANcpLua.Analyzers.Core;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Text;

namespace ANcpLua.Analyzers.CodeFixes.CodeFixes;

/// <summary>
///     Code fix for AL0025: Makes lambda/anonymous function static.
///     Uses DocumentBasedFixAllProvider for efficient solution-wide Fix All.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AL0025StaticLambdaCodeFixProvider))]
[Shared]
public sealed class AL0025StaticLambdaCodeFixProvider : CodeFixProvider {
    public override ImmutableArray<string> FixableDiagnosticIds => [DiagnosticIds.PreferStaticLambda];

    public override FixAllProvider GetFixAllProvider() => AL0025FixAllProvider.Instance;

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

    internal static async Task<Document> MakeStaticAsync(
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

    /// <summary>
    ///     Custom FixAllProvider that processes all diagnostics in a document at once,
    ///     applying fixes from bottom to top to preserve span positions.
    /// </summary>
    private sealed class AL0025FixAllProvider : DocumentBasedFixAllProvider {
        public static readonly AL0025FixAllProvider Instance = new();

        protected override async Task<Document?> FixAllAsync(
            FixAllContext fixAllContext,
            Document document,
            ImmutableArray<Diagnostic> diagnostics) {
            if (diagnostics.IsEmpty) {
                return document;
            }

            var root = await document.GetSyntaxRootAsync(fixAllContext.CancellationToken).ConfigureAwait(false);
            if (root is null) {
                return document;
            }

            // Sort by position descending so we fix from bottom to top (preserves earlier spans)
            var sortedDiagnostics = diagnostics.OrderByDescending(d => d.Location.SourceSpan.Start);

            var editor = new SyntaxEditor(root, document.Project.Solution.Services);
            var staticKeyword = SyntaxFactory.Token(SyntaxKind.StaticKeyword)
                .WithTrailingTrivia(SyntaxFactory.Space);

            foreach (var diagnostic in sortedDiagnostics) {
                var span = diagnostic.Location.SourceSpan;
                var node = root.FindNode(span, getInnermostNodeForTie: true);
                var lambda = node as AnonymousFunctionExpressionSyntax
                             ?? node.FirstAncestorOrSelf<AnonymousFunctionExpressionSyntax>();

                if (lambda is null) {
                    continue;
                }

                AnonymousFunctionExpressionSyntax newLambda = lambda switch {
                    SimpleLambdaExpressionSyntax simple => simple.AddModifiers(staticKeyword),
                    ParenthesizedLambdaExpressionSyntax paren => paren.AddModifiers(staticKeyword),
                    AnonymousMethodExpressionSyntax anon => anon.AddModifiers(staticKeyword),
                    _ => lambda
                };

                editor.ReplaceNode(lambda, newLambda);
            }

            return document.WithSyntaxRoot(editor.GetChangedRoot());
        }
    }
}
