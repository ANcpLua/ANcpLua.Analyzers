using ANcpLua.Analyzers.Analyzers;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Text;

namespace ANcpLua.Analyzers.CodeFixes.CodeFixes;

/// <summary>
///     Code fix for AL1700: Makes lambda/anonymous function static.
///     Uses DocumentBasedFixAllProvider for efficient solution-wide Fix All.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(Al1700StaticLambdaCodeFixProvider))]
[Shared]
public sealed partial class Al1700StaticLambdaCodeFixProvider : CodeFixProvider {
    public override ImmutableArray<string> FixableDiagnosticIds => [Al1700PreferStaticLambdaAnalyzer.DiagnosticId];

    public override FixAllProvider GetFixAllProvider() => Al1700FixAllProvider.Instance;

    public override Task RegisterCodeFixesAsync(CodeFixContext context) {
        foreach (var diagnostic in context.Diagnostics) {
            var span = diagnostic.Location.SourceSpan;

            context.RegisterCodeFix(
                CodeAction.Create(
                    CodeFixResources.AL1700CodeFixTitle,
                    ct => MakeStaticAsync(context.Document, span, ct),
                    nameof(CodeFixResources.AL1700CodeFixTitle)),
                diagnostic);
        }

        return Task.CompletedTask;
    }

    private static async Task<Document> MakeStaticAsync(
        Document document,
        TextSpan span,
        CancellationToken cancellationToken) {
        if (await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false) is not { } root) {
            return document;
        }

        var node = root.FindNode(span, getInnermostNodeForTie: true);
        if ((node as AnonymousFunctionExpressionSyntax
             ?? node.FirstAncestorOrSelf<AnonymousFunctionExpressionSyntax>()) is not { } lambda) {
            return document;
        }

        var staticKeyword = SyntaxFactory.Token(SyntaxKind.StaticKeyword)
            .WithTrailingTrivia(SyntaxFactory.Space);

        var newLambda = lambda switch {
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
    private sealed partial class Al1700FixAllProvider : DocumentBasedFixAllProvider {
        public static readonly Al1700FixAllProvider Instance = new();

        protected override async Task<Document?> FixAllAsync(
            FixAllContext fixAllContext,
            Document document,
            ImmutableArray<Diagnostic> diagnostics) {
            if (diagnostics.IsEmpty || await document.GetSyntaxRootAsync(fixAllContext.CancellationToken).ConfigureAwait(false) is not
                    { } root) {
                return document;
            }

            // Sort by position descending so we fix from bottom to top (preserves earlier spans)
            var sortedDiagnostics = diagnostics.OrderByDescending(static d => d.Location.SourceSpan.Start);

            var editor = new SyntaxEditor(root, document.Project.Solution.Services);
            var staticKeyword = SyntaxFactory.Token(SyntaxKind.StaticKeyword)
                .WithTrailingTrivia(SyntaxFactory.Space);

            foreach (var diagnostic in sortedDiagnostics) {
                var span = diagnostic.Location.SourceSpan;
                var node = root.FindNode(span, getInnermostNodeForTie: true);
                if ((node as AnonymousFunctionExpressionSyntax
                     ?? node.FirstAncestorOrSelf<AnonymousFunctionExpressionSyntax>()) is not { } lambda) {
                    continue;
                }

                var newLambda = lambda switch {
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
