using ANcpLua.Analyzers.Analyzers;
using Microsoft.CodeAnalysis.Editing;

namespace ANcpLua.Analyzers.CodeFixes.Refactorings;

/// <summary>
///     AR0002: Refactoring to make lambdas static with scope selection.
///     Provides "Make static in file/project/solution" submenu.
/// </summary>
[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(Ar0002MakeStaticLambdaRefactoring))]
[Shared]
public sealed class Ar0002MakeStaticLambdaRefactoring : CodeRefactoringProvider {
    public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context) {
        var document = context.Document;
        var root = await document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null) {
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
        if (semanticModel is null || !AL0025PreferStaticLambdaAnalyzer.CanBeStatic(lambda, semanticModel)) {
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
        var root = await document.GetSyntaxRootAsync(ct).ConfigureAwait(false);
        if (root is null) {
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
            var document = solution.GetDocument(documentId);
            if (document is null) {
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
                var document = solution.GetDocument(documentId);
                if (document is null) {
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
        SemanticModel semanticModel) =>
        [.. root.DescendantNodes()
            .OfType<AnonymousFunctionExpressionSyntax>()
            .Where(lambda => !lambda.Modifiers.Any(SyntaxKind.StaticKeyword) &&
                             AL0025PreferStaticLambdaAnalyzer.CanBeStatic(lambda, semanticModel))];

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
