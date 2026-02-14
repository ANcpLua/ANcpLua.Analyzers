using ANcpLua.Analyzers.Analyzers;

namespace ANcpLua.Analyzers.CodeFixes.CodeFixes;

/// <summary>
///     Code fix for AL0049: Converts if (x &lt;= 0) throw to Guard.Positive(x).
/// </summary>
/// <remarks>
///     <list type="bullet">
///         <item><c>if (x &lt;= 0) throw new ArgumentOutOfRangeException(...)</c> to <c>Guard.Positive(x);</c></item>
///         <item><c>if (0 &gt;= x) throw new ArgumentOutOfRangeException(...)</c> to <c>Guard.Positive(x);</c></item>
///     </list>
/// </remarks>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(Al0049UseGuardPositiveCodeFixProvider))]
[Shared]
public sealed partial class Al0049UseGuardPositiveCodeFixProvider
    : AlCodeFixProvider<IfStatementSyntax> {
    /// <summary>Gets the diagnostic IDs this code fix can fix.</summary>
    public override ImmutableArray<string> FixableDiagnosticIds => [Al0049UseGuardPositiveAnalyzer.DiagnosticId];

    /// <summary>Creates the code action for this fix.</summary>
    protected override CodeAction CreateCodeAction(
        Document document,
        IfStatementSyntax ifStatement,
        SyntaxNode root,
        Diagnostic diagnostic) =>
        CodeAction.Create(
            CodeFixResources.AL0049CodeFixTitle,
            _ => ConvertToGuardPositive(document, ifStatement, root),
            nameof(Al0049UseGuardPositiveCodeFixProvider));

    private static Task<Document> ConvertToGuardPositive(
        Document document,
        IfStatementSyntax ifStatement,
        SyntaxNode root) {
        // Extract the identifier from the condition
        var identifier = GetIdentifierFromCondition(ifStatement.Condition);

        // Create: Guard.Positive(identifier);
        var guardCall = SyntaxFactory.ExpressionStatement(
                SyntaxFactory.InvocationExpression(
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.IdentifierName("Guard"),
                            SyntaxFactory.IdentifierName("Positive")))
                    .WithArgumentList(
                        SyntaxFactory.ArgumentList(
                            SyntaxFactory.SingletonSeparatedList(
                                SyntaxFactory.Argument(
                                    SyntaxFactory.IdentifierName(identifier))))))
            .WithLeadingTrivia(ifStatement.GetLeadingTrivia())
            .WithTrailingTrivia(ifStatement.GetTrailingTrivia());

        var newRoot = root.ReplaceNode(ifStatement, guardCall);
        return Task.FromResult(document.WithSyntaxRoot(newRoot));
    }

    private static string GetIdentifierFromCondition(ExpressionSyntax condition) {
        // Handle x <= 0
        if (condition is BinaryExpressionSyntax { Left: var left, Right: var right } bin
            && bin.IsKind(SyntaxKind.LessThanOrEqualExpression)) {
            if (TryGetIdentifierName(left, out var name)) {
                return name;
            }
        }

        // Handle 0 >= x
        if (condition is BinaryExpressionSyntax { Left: var leftGe, Right: var rightGe } binGe
            && binGe.IsKind(SyntaxKind.GreaterThanOrEqualExpression)) {
            if (TryGetIdentifierName(rightGe, out var name)) {
                return name;
            }
        }

        return "value";
    }

    private static bool TryGetIdentifierName(ExpressionSyntax expression, out string name) {
        name = "";

        switch (expression) {
            case IdentifierNameSyntax id:
                name = id.Identifier.Text;
                return true;
            case MemberAccessExpressionSyntax { Name: IdentifierNameSyntax memberId }:
                name = memberId.Identifier.Text;
                return true;
            default:
                return false;
        }
    }
}
