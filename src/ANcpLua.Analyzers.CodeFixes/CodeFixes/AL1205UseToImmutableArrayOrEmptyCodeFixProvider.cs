using ANcpLua.Analyzers.Analyzers;

namespace ANcpLua.Analyzers.CodeFixes.CodeFixes;

/// <summary>
///     Code fix for AL1205: Converts null-conditional ToImmutableArray with fallback to ToImmutableArrayOrEmpty().
/// </summary>
/// <remarks>
///     <c>collection?.ToImmutableArray() ?? ImmutableArray&lt;T&gt;.Empty</c> →
///     <c>collection.ToImmutableArrayOrEmpty()</c>
/// </remarks>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(Al1205UseToImmutableArrayOrEmptyCodeFixProvider))]
[Shared]
public sealed partial class Al1205UseToImmutableArrayOrEmptyCodeFixProvider
    : AlCodeFixProvider<BinaryExpressionSyntax> {
    private const string ExtensionsNamespace = "ANcpLua.Roslyn.Utilities";

    /// <summary>Gets the diagnostic IDs this code fix can fix.</summary>
    public override ImmutableArray<string> FixableDiagnosticIds => [Al1205UseToImmutableArrayOrEmptyAnalyzer.DiagnosticId];

    /// <summary>Creates the code action for this fix.</summary>
    protected override CodeAction CreateCodeAction(
        Document document,
        BinaryExpressionSyntax coalesce,
        SyntaxNode root,
        Diagnostic diagnostic) =>
        CodeAction.Create(
            CodeFixResources.AL1205CodeFixTitle,
            _ => ConvertToExtension(document, coalesce, root),
            Al1205UseToImmutableArrayOrEmptyAnalyzer.DiagnosticId);

    private static Task<Document> ConvertToExtension(
        Document document,
        BinaryExpressionSyntax coalesce,
        SyntaxNode root) {
        // Get the source from the conditional access: source?.ToImmutableArray()
        var source = ExtractSourceFromConditionalAccess(coalesce.Left);

        // Create: source.ToImmutableArrayOrEmpty()
        var newExpression = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    source.WithoutTrivia(),
                    SyntaxFactory.IdentifierName("ToImmutableArrayOrEmpty")))
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

    private static ExpressionSyntax ExtractSourceFromConditionalAccess(ExpressionSyntax expression) {
        return expression switch {
            // Handle: source?.ToImmutableArray()
            ConditionalAccessExpressionSyntax conditionalAccess => conditionalAccess.Expression,
            // Handle parenthesized: (source?.ToImmutableArray())
            ParenthesizedExpressionSyntax paren => ExtractSourceFromConditionalAccess(paren.Expression),
            _ => expression
        };

    }
}
