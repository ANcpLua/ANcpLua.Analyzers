using ANcpLua.Analyzers.Core;

namespace ANcpLua.Analyzers.CodeFixes.CodeFixes;

/// <summary>
///     Code fix for AL0026: Converts DateTime.Now/UtcNow to TimeProvider methods.
/// </summary>
/// <remarks>
///     <c>DateTime.Now</c> → <c>TimeProvider.System.GetLocalNow().DateTime</c>
///     <c>DateTime.UtcNow</c> → <c>TimeProvider.System.GetUtcNow().DateTime</c>
/// </remarks>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(Al0026DateTimeNowCodeFixProvider))]
[Shared]
public sealed partial class Al0026DateTimeNowCodeFixProvider : AlCodeFixProvider<MemberAccessExpressionSyntax> {
    public override ImmutableArray<string> FixableDiagnosticIds => [DiagnosticIds.AvoidDateTimeNow];

    protected override CodeAction CreateCodeAction(
        Document document,
        MemberAccessExpressionSyntax memberAccess,
        SyntaxNode root,
        Diagnostic diagnostic) =>
        CodeAction.Create(
            CodeFixResources.AL0026CodeFixTitle,
            _ => ConvertToTimeProvider(document, memberAccess, root),
            nameof(Al0026DateTimeNowCodeFixProvider));

    private static Task<Document> ConvertToTimeProvider(
        Document document,
        MemberAccessExpressionSyntax memberAccess,
        SyntaxNode root) {
        // Determine the replacement method based on the property name
        if (memberAccess.Name.Identifier.Text switch {
            "Now" => "GetLocalNow",
            "UtcNow" => "GetUtcNow",
            _ => null
        } is not { } methodName) {
            return Task.FromResult(document);
        }

        // Create: TimeProvider.System.GetLocalNow().DateTime or TimeProvider.System.GetUtcNow().DateTime
        var newExpression = SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.IdentifierName("TimeProvider"),
                            SyntaxFactory.IdentifierName("System")),
                        SyntaxFactory.IdentifierName(methodName))),
                SyntaxFactory.IdentifierName("DateTime"))
            .WithTriviaFrom(memberAccess);

        var newRoot = root.ReplaceNode(memberAccess, newExpression);
        return Task.FromResult(document.WithSyntaxRoot(newRoot));
    }
}
