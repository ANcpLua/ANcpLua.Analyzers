using ANcpLua.Analyzers.Core;

namespace ANcpLua.Analyzers.CodeFixes.CodeFixes;

/// <summary>
///     Code fix for AL0045: Converts if (string.IsNullOrEmpty) throw to Guard.NotNullOrEmpty().
/// </summary>
/// <remarks>
///     <list type="bullet">
///         <item>
///             <c>if (string.IsNullOrEmpty(value)) throw new ArgumentNullException(nameof(value))</c>
///             to <c>Guard.NotNullOrEmpty(value);</c>
///         </item>
///         <item>
///             <c>if (string.IsNullOrEmpty(value)) throw new ArgumentException(...)</c>
///             to <c>Guard.NotNullOrEmpty(value);</c>
///         </item>
///     </list>
/// </remarks>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(Al0045UseGuardNotNullOrEmptyCodeFixProvider))]
[Shared]
public sealed partial class Al0045UseGuardNotNullOrEmptyCodeFixProvider
    : AlCodeFixProvider<IfStatementSyntax> {
    /// <summary>Gets the diagnostic IDs this code fix can fix.</summary>
    public override ImmutableArray<string> FixableDiagnosticIds => [DiagnosticIds.UseGuardNotNullOrEmpty];

    /// <summary>Creates the code action for this fix.</summary>
    protected override CodeAction CreateCodeAction(
        Document document,
        IfStatementSyntax ifStatement,
        SyntaxNode root,
        Diagnostic diagnostic) =>
        CodeAction.Create(
            CodeFixResources.AL0045CodeFixTitle,
            _ => ConvertToGuardNotNullOrEmpty(document, ifStatement, root),
            nameof(Al0045UseGuardNotNullOrEmptyCodeFixProvider));

    private static Task<Document> ConvertToGuardNotNullOrEmpty(
        Document document,
        IfStatementSyntax ifStatement,
        SyntaxNode root) {
        // Extract the argument from the condition: string.IsNullOrEmpty(value) -> value
        var argumentExpression = ExtractArgumentFromCondition(ifStatement.Condition);

        // Create: Guard.NotNullOrEmpty(value);
        var guardCall = SyntaxFactory.ExpressionStatement(
                SyntaxFactory.InvocationExpression(
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.IdentifierName("Guard"),
                            SyntaxFactory.IdentifierName("NotNullOrEmpty")),
                        SyntaxFactory.ArgumentList(
                            SyntaxFactory.SingletonSeparatedList(
                                SyntaxFactory.Argument(argumentExpression.WithoutTrivia())))))
            .WithTriviaFrom(ifStatement);

        var newRoot = root.ReplaceNode(ifStatement, guardCall);
        return Task.FromResult(document.WithSyntaxRoot(newRoot));
    }

    private static ExpressionSyntax ExtractArgumentFromCondition(ExpressionSyntax condition) {
        // Unwrap parentheses
        while (condition is ParenthesizedExpressionSyntax paren) {
            condition = paren.Expression;
        }

        // Get string.IsNullOrEmpty(x) -> x
        if (condition is InvocationExpressionSyntax invocation &&
            invocation.ArgumentList.Arguments.Count == 1) {
            return invocation.ArgumentList.Arguments[0].Expression;
        }

        // Fallback - shouldn't happen if analyzer reported correctly
        return SyntaxFactory.IdentifierName("value");
    }
}
