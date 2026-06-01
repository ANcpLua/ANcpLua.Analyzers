using ANcpLua.Analyzers.Analyzers;

namespace ANcpLua.Analyzers.CodeFixes.CodeFixes;

/// <summary>
///     Code fix for AL1204: Converts null-coalescing with empty collection to OrEmpty().
/// </summary>
/// <remarks>
///     <list type="bullet">
///         <item><c>collection ?? Array.Empty&lt;T&gt;()</c> → <c>collection.OrEmpty()</c></item>
///         <item><c>collection ?? Enumerable.Empty&lt;T&gt;()</c> → <c>collection.OrEmpty()</c></item>
///         <item><c>collection ?? []</c> → <c>collection.OrEmpty()</c></item>
///     </list>
/// </remarks>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(Al1204UseOrEmptyCodeFixProvider))]
[Shared]
public sealed partial class Al1204UseOrEmptyCodeFixProvider
    : AlCodeFixProvider<BinaryExpressionSyntax> {
    private const string ExtensionsNamespace = "ANcpLua.Roslyn.Utilities";

    /// <summary>Gets the diagnostic IDs this code fix can fix.</summary>
    public override ImmutableArray<string> FixableDiagnosticIds => [Al1204UseOrEmptyAnalyzer.DiagnosticId];

    /// <summary>Creates the code action for this fix.</summary>
    protected override CodeAction CreateCodeAction(
        Document document,
        BinaryExpressionSyntax coalesce,
        SyntaxNode root,
        Diagnostic diagnostic) =>
        CodeAction.Create(
            CodeFixResources.AL1204CodeFixTitle,
            _ => ConvertToOrEmpty(document, coalesce, root),
            Al1204UseOrEmptyAnalyzer.DiagnosticId);

    private static Task<Document> ConvertToOrEmpty(
        Document document,
        BinaryExpressionSyntax coalesce,
        SyntaxNode root) {
        // Get the left operand (the collection being null-checked)
        var collection = coalesce.Left;

        // Create: collection.OrEmpty()
        var newExpression = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    collection.WithoutTrivia(),
                    SyntaxFactory.IdentifierName("OrEmpty")))
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
