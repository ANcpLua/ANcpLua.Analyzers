using ANcpLua.Analyzers.Analyzers;

namespace ANcpLua.Analyzers.CodeFixes.CodeFixes;

/// <summary>
///     Code fix for AL0046: Converts if (string.IsNullOrWhiteSpace(x)) throw to Guard.NotNullOrWhiteSpace(x).
/// </summary>
/// <remarks>
///     <list type="bullet">
///         <item><c>if (string.IsNullOrWhiteSpace(value)) throw new ArgumentNullException(nameof(value))</c> becomes <c>Guard.NotNullOrWhiteSpace(value);</c></item>
///         <item><c>if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException(...)</c> becomes <c>Guard.NotNullOrWhiteSpace(value);</c></item>
///     </list>
/// </remarks>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(Al0046UseGuardNotNullOrWhiteSpaceCodeFixProvider))]
[Shared]
public sealed partial class Al0046UseGuardNotNullOrWhiteSpaceCodeFixProvider
    : AlCodeFixProvider<IfStatementSyntax> {
    /// <summary>Gets the diagnostic IDs this code fix can fix.</summary>
    public override ImmutableArray<string> FixableDiagnosticIds => [Al0046UseGuardNotNullOrWhiteSpaceAnalyzer.DiagnosticId];

    /// <summary>Creates the code action for this fix.</summary>
    protected override CodeAction CreateCodeAction(
        Document document,
        IfStatementSyntax ifStatement,
        SyntaxNode root,
        Diagnostic diagnostic) =>
        CodeAction.Create(
            CodeFixResources.AL0046CodeFixTitle,
            _ => ConvertToGuardNotNullOrWhiteSpace(document, ifStatement, root),
            nameof(Al0046UseGuardNotNullOrWhiteSpaceCodeFixProvider));

    private static Task<Document> ConvertToGuardNotNullOrWhiteSpace(
        Document document,
        IfStatementSyntax ifStatement,
        SyntaxNode root) {
        // Extract the argument from string.IsNullOrWhiteSpace(x)
        if (GetArgumentFromCondition(ifStatement.Condition) is not { } argument) {
            return Task.FromResult(document);
        }

        // Create: Guard.NotNullOrWhiteSpace(value);
        var guardCall = SyntaxFactory.ExpressionStatement(
                SyntaxFactory.InvocationExpression(
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.IdentifierName("Guard"),
                            SyntaxFactory.IdentifierName("NotNullOrWhiteSpace")),
                        SyntaxFactory.ArgumentList(
                            SyntaxFactory.SingletonSeparatedList(
                                SyntaxFactory.Argument(argument.WithoutTrivia())))))
            .WithTriviaFrom(ifStatement);

        var newRoot = root.ReplaceNode(ifStatement, guardCall);
        return Task.FromResult(document.WithSyntaxRoot(newRoot));
    }

    private static ExpressionSyntax? GetArgumentFromCondition(ExpressionSyntax condition) {
        if (condition is InvocationExpressionSyntax {
            ArgumentList.Arguments.Count: 1
        } invocation) {
            return invocation.ArgumentList.Arguments[0].Expression;
        }

        return null;
    }
}
