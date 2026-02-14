using ANcpLua.Analyzers.Analyzers;

namespace ANcpLua.Analyzers.CodeFixes.CodeFixes;

/// <summary>
///     Code fix for AL0032: Converts null-coalescing with empty collection to OrEmpty().
/// </summary>
/// <remarks>
///     <list type="bullet">
///         <item><c>collection ?? Array.Empty&lt;T&gt;()</c> → <c>collection.OrEmpty()</c></item>
///         <item><c>collection ?? Enumerable.Empty&lt;T&gt;()</c> → <c>collection.OrEmpty()</c></item>
///         <item><c>collection ?? []</c> → <c>collection.OrEmpty()</c></item>
///     </list>
/// </remarks>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(Al0032UseOrEmptyCodeFixProvider))]
[Shared]
public sealed partial class Al0032UseOrEmptyCodeFixProvider
    : AlCodeFixProvider<BinaryExpressionSyntax> {
    /// <summary>Gets the diagnostic IDs this code fix can fix.</summary>
    public override ImmutableArray<string> FixableDiagnosticIds => [Al0032UseOrEmptyAnalyzer.DiagnosticId];

    /// <summary>Creates the code action for this fix.</summary>
    protected override CodeAction CreateCodeAction(
        Document document,
        BinaryExpressionSyntax coalesce,
        SyntaxNode root,
        Diagnostic diagnostic) =>
        CodeAction.Create(
            CodeFixResources.AL0032CodeFixTitle,
            _ => ConvertToOrEmpty(document, coalesce, root),
            nameof(Al0032UseOrEmptyCodeFixProvider));

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
        return Task.FromResult(document.WithSyntaxRoot(newRoot));
    }
}
