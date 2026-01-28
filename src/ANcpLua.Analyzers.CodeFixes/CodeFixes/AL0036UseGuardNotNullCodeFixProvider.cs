using ANcpLua.Analyzers.Core;

namespace ANcpLua.Analyzers.CodeFixes.CodeFixes;

/// <summary>
///     Code fix for AL0036: Converts null-coalescing throw to Guard.NotNull().
/// </summary>
/// <remarks>
///     <list type="bullet">
///         <item><c>value ?? throw new ArgumentNullException(nameof(value))</c> → <c>Guard.NotNull(value)</c></item>
///         <item><c>value ?? throw new ArgumentNullException("value")</c> → <c>Guard.NotNull(value)</c></item>
///     </list>
/// </remarks>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(Al0036UseGuardNotNullCodeFixProvider))]
[Shared]
public sealed partial class Al0036UseGuardNotNullCodeFixProvider
    : AlCodeFixProvider<BinaryExpressionSyntax> {
    /// <summary>Gets the diagnostic IDs this code fix can fix.</summary>
    public override ImmutableArray<string> FixableDiagnosticIds => [DiagnosticIds.UseGuardNotNull];

    /// <summary>Creates the code action for this fix.</summary>
    protected override CodeAction CreateCodeAction(
        Document document,
        BinaryExpressionSyntax coalesce,
        SyntaxNode root,
        Diagnostic diagnostic) =>
        CodeAction.Create(
            CodeFixResources.AL0036CodeFixTitle,
            _ => ConvertToGuardNotNull(document, coalesce, root),
            nameof(Al0036UseGuardNotNullCodeFixProvider));

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
        return Task.FromResult(document.WithSyntaxRoot(newRoot));
    }
}
