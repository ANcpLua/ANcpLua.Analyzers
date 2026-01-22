using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Analyzers.Core;
using Microsoft.CodeAnalysis.Editing;

namespace ANcpLua.Analyzers.CodeFixes.Refactorings;

/// <summary>
///     AR0002: Refactoring to make lambdas static with scope selection.
///     Provides options for single lambda, file, project, or solution scope.
///     Note: Uses flat actions instead of submenu due to Rider limitation (RIDER-74933).
/// </summary>
[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(Ar0002MakeStaticLambdaRefactoring))]
[Shared]
public sealed class Ar0002MakeStaticLambdaRefactoring : CodeRefactoringProvider {
    public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context) {
        var document = context.Document;
        if (await document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false) is not { } root) {
            return;
        }

        var node = root.FindNode(context.Span, getInnermostNodeForTie: true);
        var lambda = node as AnonymousFunctionExpressionSyntax
                     ?? node.FirstAncestorOrSelf<AnonymousFunctionExpressionSyntax>();

        if (lambda is null || lambda.Modifiers.Any(SyntaxKind.StaticKeyword)) {
            return;
        }

        // Check if lambda can be made static using the analyzer's logic
        var semanticModel = await document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (semanticModel is null || !Al0025PreferStaticLambdaAnalyzer.CanBeStatic(lambda, semanticModel)) {
            return;
        }

        // Skip if AL0025 diagnostic is active (code fix handles it)
        var diagnostics = semanticModel.GetDiagnostics(lambda.Span, context.CancellationToken);
        if (diagnostics.Any(static d => d.Id == DiagnosticIds.PreferStaticLambda)) {
            return;
        }

        // Register individual actions for Rider compatibility
        // Note: Rider doesn't support nested CodeActions (RIDER-74933)
        // Visual Studio would support nested submenus, but we use flat list for compatibility
        context.RegisterRefactoring(
            CodeAction.Create(
                "Make lambda static",
                ct => MakeStaticSingleAsync(document, lambda, ct),
                "AR0002_MakeStaticSingle"));

        context.RegisterRefactoring(
            CodeAction.Create(
                "Make all lambdas static in file",
                ct => MakeStaticInDocumentAsync(document, ct),
                "AR0002_MakeStaticInFile"));

        context.RegisterRefactoring(
            CodeAction.Create(
                "Make all lambdas static in project",
                ct => MakeStaticInProjectAsync(document.Project, ct),
                "AR0002_MakeStaticInProject"));

        context.RegisterRefactoring(
            CodeAction.Create(
                "Make all lambdas static in solution",
                ct => MakeStaticInSolutionAsync(document.Project.Solution, ct),
                "AR0002_MakeStaticInSolution"));
    }

    private static async Task<Document> MakeStaticSingleAsync(
        Document document,
        AnonymousFunctionExpressionSyntax lambda,
        CancellationToken ct) {
        if (await document.GetSyntaxRootAsync(ct).ConfigureAwait(false) is not { } root) {
            return document;
        }

        var newLambda = AddStaticModifier(lambda);
        return document.WithSyntaxRoot(root.ReplaceNode(lambda, newLambda));
    }

    private static async Task<Document> MakeStaticInDocumentAsync(
        Document document,
        CancellationToken ct) {
        var root = await document.GetSyntaxRootAsync(ct).ConfigureAwait(false);
        var semanticModel = await document.GetSemanticModelAsync(ct).ConfigureAwait(false);
        if (root is null || semanticModel is null) {
            return document;
        }

        var lambdas = FindStaticCandidates(root, semanticModel);
        if (lambdas.IsEmpty) {
            return document;
        }

        var editor = new SyntaxEditor(root, document.Project.Solution.Services);
        foreach (var lambda in lambdas) {
            editor.ReplaceNode(lambda, AddStaticModifier(lambda));
        }

        return document.WithSyntaxRoot(editor.GetChangedRoot());
    }

    private static async Task<Solution> MakeStaticInProjectAsync(
        Project project,
        CancellationToken ct) {
        var solution = project.Solution;

        foreach (var documentId in project.DocumentIds) {
            if (solution.GetDocument(documentId) is not { } document) {
                continue;
            }

            var newDocument = await MakeStaticInDocumentAsync(document, ct).ConfigureAwait(false);
            solution = newDocument.Project.Solution;
        }

        return solution;
    }

    private static async Task<Solution> MakeStaticInSolutionAsync(
        Solution solution,
        CancellationToken ct) {
        foreach (var project in solution.Projects) {
            foreach (var documentId in project.DocumentIds) {
                if (solution.GetDocument(documentId) is not { } document) {
                    continue;
                }

                var newDocument = await MakeStaticInDocumentAsync(document, ct).ConfigureAwait(false);
                solution = newDocument.Project.Solution;
            }
        }

        return solution;
    }

    private static ImmutableArray<AnonymousFunctionExpressionSyntax> FindStaticCandidates(
        SyntaxNode root,
        SemanticModel semanticModel) => [
        .. root.DescendantNodes()
            .OfType<AnonymousFunctionExpressionSyntax>()
            .Where(lambda => !lambda.Modifiers.Any(SyntaxKind.StaticKeyword) &&
                             Al0025PreferStaticLambdaAnalyzer.CanBeStatic(lambda, semanticModel))
    ];

    private static AnonymousFunctionExpressionSyntax AddStaticModifier(AnonymousFunctionExpressionSyntax lambda) {
        var staticKeyword = SyntaxFactory.Token(SyntaxKind.StaticKeyword)
            .WithTrailingTrivia(SyntaxFactory.Space);

        return lambda switch {
            SimpleLambdaExpressionSyntax simple => simple.AddModifiers(staticKeyword),
            ParenthesizedLambdaExpressionSyntax paren => paren.AddModifiers(staticKeyword),
            AnonymousMethodExpressionSyntax anon => anon.AddModifiers(staticKeyword),
            _ => lambda
        };
    }
}
