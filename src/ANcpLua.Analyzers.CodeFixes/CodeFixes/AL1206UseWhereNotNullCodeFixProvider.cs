using ANcpLua.Analyzers.Analyzers;

namespace ANcpLua.Analyzers.CodeFixes.CodeFixes;

/// <summary>
///     Code fix for AL1206: Converts Where with null check to WhereNotNull().
/// </summary>
/// <remarks>
///     <list type="bullet">
///         <item><c>.Where(x => x != null)</c> → <c>.WhereNotNull()</c></item>
///         <item><c>.Where(x => x is not null)</c> → <c>.WhereNotNull()</c></item>
///     </list>
/// </remarks>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(Al1206UseWhereNotNullCodeFixProvider))]
[Shared]
public sealed partial class Al1206UseWhereNotNullCodeFixProvider
    : AlCodeFixProvider<InvocationExpressionSyntax> {
    private const string ExtensionsNamespace = "ANcpLua.Roslyn.Utilities";

    /// <summary>Gets the diagnostic IDs this code fix can fix.</summary>
    public override ImmutableArray<string> FixableDiagnosticIds => [Al1206UseWhereNotNullAnalyzer.DiagnosticId];

    /// <summary>Creates the code action for this fix.</summary>
    protected override CodeAction CreateCodeAction(
        Document document,
        InvocationExpressionSyntax invocation,
        SyntaxNode root,
        Diagnostic diagnostic) =>
        CodeAction.Create(
            CodeFixResources.AL1206CodeFixTitle,
            _ => ConvertToWhereNotNull(document, invocation, root),
            Al1206UseWhereNotNullAnalyzer.DiagnosticId);

    private static Task<Document> ConvertToWhereNotNull(
        Document document,
        InvocationExpressionSyntax invocation,
        SyntaxNode root) {
        // Get the receiver from member access: source.Where(...)
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess) {
            return Task.FromResult(document);
        }

        var source = memberAccess.Expression;

        // Create: source.WhereNotNull() - preserve source trivia, apply invocation trivia to result
        var newExpression = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    source,
                    SyntaxFactory.IdentifierName("WhereNotNull")))
            .WithTrailingTrivia(invocation.GetTrailingTrivia());

        var newRoot = root.ReplaceNode(invocation, newExpression);
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
