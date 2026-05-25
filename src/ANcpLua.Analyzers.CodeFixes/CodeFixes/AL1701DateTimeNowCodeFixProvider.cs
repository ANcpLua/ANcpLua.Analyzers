using ANcpLua.Analyzers.Analyzers;

namespace ANcpLua.Analyzers.CodeFixes.CodeFixes;

/// <summary>
///     Code fix for AL1701: Converts DateTime.Now/UtcNow to TimeProvider methods.
/// </summary>
/// <remarks>
///     <c>DateTime.Now</c> → <c>TimeProvider.System.GetLocalNow().DateTime</c>
///     <c>DateTime.UtcNow</c> → <c>TimeProvider.System.GetUtcNow().DateTime</c>
/// </remarks>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(Al1701DateTimeNowCodeFixProvider))]
[Shared]
public sealed partial class Al1701DateTimeNowCodeFixProvider : AlCodeFixProvider<MemberAccessExpressionSyntax> {
    public override ImmutableArray<string> FixableDiagnosticIds => [Al1701AvoidDateTimeNowAnalyzer.DiagnosticId];

    protected override CodeAction CreateCodeAction(
        Document document,
        MemberAccessExpressionSyntax memberAccess,
        SyntaxNode root,
        Diagnostic diagnostic) =>
        CodeAction.Create(
            CodeFixResources.AL1701CodeFixTitle,
            _ => ConvertToTimeProvider(document, memberAccess, root, diagnostic),
            diagnostic.Id);

    private static Task<Document> ConvertToTimeProvider(
        Document document,
        MemberAccessExpressionSyntax memberAccess,
        SyntaxNode root,
        Diagnostic diagnostic) {
        // Determine the replacement method based on the property name
        if (memberAccess.Name.Identifier.Text switch {
            "Now" => "GetLocalNow",
            "UtcNow" => "GetUtcNow",
            _ => null
        } is not { } methodName) {
            return Task.FromResult(document);
        }

        // Check if source is DateTimeOffset (from diagnostic properties)
        var isOffset = diagnostic.Properties.TryGetValue(
            Analyzers.Al1701AvoidDateTimeNowAnalyzer.PropertyIsDateTimeOffset, out var value)
            && value == "True";

        // Create: TimeProvider.System.GetLocalNow() or TimeProvider.System.GetUtcNow()
        ExpressionSyntax newExpression = SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName("TimeProvider"),
                    SyntaxFactory.IdentifierName("System")),
                SyntaxFactory.IdentifierName(methodName)));

        // For non-offset types, append .DateTime to convert DateTimeOffset to expected type
        if (!isOffset) {
            newExpression = SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                newExpression,
                SyntaxFactory.IdentifierName("DateTime"));
        }

        newExpression = newExpression.WithTriviaFrom(memberAccess);
        var newRoot = root.ReplaceNode(memberAccess, newExpression);
        return Task.FromResult(document.WithSyntaxRoot(newRoot));
    }
}
