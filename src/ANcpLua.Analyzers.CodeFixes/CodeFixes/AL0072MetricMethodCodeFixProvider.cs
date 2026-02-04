using ANcpLua.Analyzers.Core;

namespace ANcpLua.Analyzers.CodeFixes.CodeFixes;

/// <summary>
///     Code fix provider for AL0072: Adds 'partial' modifier to metric methods.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(Al0072MetricMethodCodeFixProvider))]
[Shared]
public sealed partial class Al0072MetricMethodCodeFixProvider : CodeFixProvider {
    /// <summary>Gets the diagnostic IDs this provider can fix.</summary>
    public override ImmutableArray<string> FixableDiagnosticIds => [DiagnosticIds.MetricMethodMustBePartial];

    /// <summary>Gets the FixAll provider for batch fixing.</summary>
    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    /// <summary>Registers code fixes for the given context.</summary>
    public override async Task RegisterCodeFixesAsync(CodeFixContext context) {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null) {
            return;
        }

        var diagnostic = context.Diagnostics[0];
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        // Find the method declaration identified by the diagnostic
        var node = root.FindToken(diagnosticSpan.Start).Parent;
        var methodDeclaration = node?.AncestorsAndSelf().OfType<MethodDeclarationSyntax>().FirstOrDefault();

        if (methodDeclaration is null) {
            return;
        }

        context.RegisterCodeFix(
            CodeAction.Create(
                CodeFixResources.AL0072CodeFixTitle,
                c => MakePartialAsync(context.Document, methodDeclaration, root, c),
                nameof(Al0072MetricMethodCodeFixProvider)),
            diagnostic);
    }

    private static Task<Document> MakePartialAsync(
        Document document,
        MethodDeclarationSyntax methodDeclaration,
        SyntaxNode root,
        CancellationToken _) {
        var modifiers = methodDeclaration.Modifiers;

        // Check if partial is already present
        if (modifiers.Any(SyntaxKind.PartialKeyword)) {
            return Task.FromResult(document);
        }

        // Add partial modifier before the return type
        var partialToken = SyntaxFactory.Token(SyntaxKind.PartialKeyword).WithTrailingTrivia(SyntaxFactory.Space);
        var newModifiers = modifiers.Add(partialToken);

        // For partial methods, we also need to:
        // 1. Remove the method body and replace with semicolon
        // 2. Keep the method signature

        var newMethodDeclaration = methodDeclaration
            .WithModifiers(newModifiers)
            .WithBody(null)
            .WithExpressionBody(null)
            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));

        var newRoot = root.ReplaceNode(methodDeclaration, newMethodDeclaration);

        return Task.FromResult(document.WithSyntaxRoot(newRoot));
    }
}
