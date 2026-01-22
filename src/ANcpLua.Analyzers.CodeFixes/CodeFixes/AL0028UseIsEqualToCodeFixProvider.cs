using ANcpLua.Analyzers.Core;

namespace ANcpLua.Analyzers.CodeFixes.CodeFixes;

/// <summary>
///     Code fix for AL0028: Converts SymbolEqualityComparer.Equals to IsEqualTo extension.
/// </summary>
/// <remarks>
///     <c>SymbolEqualityComparer.Default.Equals(a, b)</c> → <c>a.IsEqualTo(b)</c>
/// </remarks>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(Al0028UseIsEqualToCodeFixProvider))]
[Shared]
public sealed partial class Al0028UseIsEqualToCodeFixProvider : AlCodeFixProvider<InvocationExpressionSyntax> {
    public override ImmutableArray<string> FixableDiagnosticIds => [DiagnosticIds.UseIsEqualTo];

    protected override CodeAction CreateCodeAction(
        Document document,
        InvocationExpressionSyntax invocation,
        SyntaxNode root,
        Diagnostic diagnostic) =>
        CodeAction.Create(
            CodeFixResources.AL0028CodeFixTitle,
            _ => ConvertToIsEqualTo(document, invocation, root),
            nameof(Al0028UseIsEqualToCodeFixProvider));

    private static Task<Document> ConvertToIsEqualTo(
        Document document,
        InvocationExpressionSyntax invocation,
        SyntaxNode root) {
        // Get the two arguments from SymbolEqualityComparer.*.Equals(arg0, arg1)
        var args = invocation.ArgumentList.Arguments;
        if (args.Count != 2) {
            return Task.FromResult(document);
        }

        var arg0 = args[0].Expression;
        var arg1 = args[1].Expression;

        // Create: arg0.IsEqualTo(arg1)
        var newExpression = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    arg0.WithoutTrivia(),
                    SyntaxFactory.IdentifierName("IsEqualTo")),
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.Argument(arg1.WithoutTrivia()))))
            .WithTriviaFrom(invocation);

        var newRoot = root.ReplaceNode(invocation, newExpression);
        return Task.FromResult(document.WithSyntaxRoot(newRoot));
    }
}
