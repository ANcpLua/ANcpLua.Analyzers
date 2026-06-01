using ANcpLua.Analyzers.Analyzers;

namespace ANcpLua.Analyzers.CodeFixes.CodeFixes;

/// <summary>
///     Code fix for AL1208: Converts null-coalescing throw to Guard.NotNull().
/// </summary>
/// <remarks>
///     <list type="bullet">
///         <item><c>value ?? throw new ArgumentNullException(nameof(value))</c> → <c>Guard.NotNull(value)</c></item>
///         <item><c>value ?? throw new ArgumentNullException("value")</c> → <c>Guard.NotNull(value)</c></item>
///     </list>
/// </remarks>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(Al1208UseGuardNotNullCodeFixProvider))]
[Shared]
public sealed partial class Al1208UseGuardNotNullCodeFixProvider
    : AlCodeFixProvider<BinaryExpressionSyntax> {
    private const string ExtensionsNamespace = "ANcpLua.Roslyn.Utilities";
    /// <summary>Gets the diagnostic IDs this code fix can fix.</summary>
    public override ImmutableArray<string> FixableDiagnosticIds => [Al1208UseGuardNotNullAnalyzer.DiagnosticId];

    /// <summary>Creates the code action for this fix.</summary>
    protected override CodeAction CreateCodeAction(
        Document document,
        BinaryExpressionSyntax coalesce,
        SyntaxNode root,
        Diagnostic diagnostic) =>
        CodeAction.Create(
            CodeFixResources.AL1208CodeFixTitle,
            _ => ConvertToGuardNotNull(document, coalesce, root),
            Al1208UseGuardNotNullAnalyzer.DiagnosticId);

    private static Task<Document> ConvertToGuardNotNull(
        Document document,
        BinaryExpressionSyntax coalesce,
        SyntaxNode root) {
        // Get the value being null-checked
        var value = coalesce.Left;

        // Create: Guard.NotNull(value)
        var newExpression = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName("Guard"),
                    SyntaxFactory.IdentifierName("NotNull")),
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.Argument(value.WithoutTrivia()))))
            .WithTriviaFrom(coalesce);

        var newRoot = root.ReplaceNode(coalesce, newExpression);
        newRoot = AddUsingIfMissing(newRoot, ExtensionsNamespace);
        return Task.FromResult(document.WithSyntaxRoot(newRoot));
    }

    private static SyntaxNode AddUsingIfMissing(SyntaxNode root, string namespaceName) {
        if (root is not CompilationUnitSyntax compilationUnit) {
            return root;
        }

        if (compilationUnit.Usings.Any(u => u.Name?.ToString() == namespaceName)) {
            return root;
        }

        var newUsing = SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(namespaceName))
            .WithTrailingTrivia(DetectEndOfLine(compilationUnit));

        return compilationUnit.AddUsings(newUsing);
    }

    private static SyntaxTrivia DetectEndOfLine(CompilationUnitSyntax compilationUnit) {
        // Preserve the file's CRLF/LF convention so the inserted using does not corrupt line endings.
        foreach (var trivia in compilationUnit.DescendantTrivia()) {
            if (trivia.IsKind(SyntaxKind.EndOfLineTrivia)) {
                return trivia;
            }
        }

        return SyntaxFactory.LineFeed;
    }
}
