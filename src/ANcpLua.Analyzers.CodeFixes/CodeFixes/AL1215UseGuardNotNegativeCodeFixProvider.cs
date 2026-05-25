using ANcpLua.Analyzers.Analyzers;

namespace ANcpLua.Analyzers.CodeFixes.CodeFixes;

/// <summary>
///     Code fix for AL1215: Converts if (x less than 0) throw to Guard.NotNegative(x).
/// </summary>
/// <remarks>
///     <list type="bullet">
///         <item><c>if (x &lt; 0) throw new ArgumentOutOfRangeException(nameof(x))</c> → <c>Guard.NotNegative(x)</c></item>
///         <item><c>if (0 &gt; x) throw new ArgumentOutOfRangeException(nameof(x))</c> → <c>Guard.NotNegative(x)</c></item>
///     </list>
/// </remarks>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(Al1215UseGuardNotNegativeCodeFixProvider))]
[Shared]
public sealed partial class Al1215UseGuardNotNegativeCodeFixProvider
    : AlCodeFixProvider<IfStatementSyntax> {
    /// <summary>Gets the diagnostic IDs this code fix can fix.</summary>
    public override ImmutableArray<string> FixableDiagnosticIds => [Al1215UseGuardNotNegativeAnalyzer.DiagnosticId];

    /// <summary>Creates the code action for this fix.</summary>
    protected override CodeAction CreateCodeAction(
        Document document,
        IfStatementSyntax ifStatement,
        SyntaxNode root,
        Diagnostic diagnostic) =>
        CodeAction.Create(
            CodeFixResources.AL1215CodeFixTitle,
            _ => ConvertToGuardNotNegative(document, ifStatement, root),
            Al1215UseGuardNotNegativeAnalyzer.DiagnosticId);

    private static Task<Document> ConvertToGuardNotNegative(
        Document document,
        IfStatementSyntax ifStatement,
        SyntaxNode root) {
        if (!IsSingleThrowBody(ifStatement)) {
            return Task.FromResult(document);
        }

        // Extract the operand from the condition
        var operandExpression = ExtractOperand(ifStatement.Condition);

        // Create: Guard.NotNegative(operand);
        var guardInvocation = SyntaxFactory.ExpressionStatement(
                SyntaxFactory.InvocationExpression(
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.IdentifierName("Guard"),
                            SyntaxFactory.IdentifierName("NotNegative")),
                        SyntaxFactory.ArgumentList(
                            SyntaxFactory.SingletonSeparatedList(
            SyntaxFactory.Argument(operandExpression.WithoutTrivia())))))
            .WithTriviaFrom(ifStatement);

        var newRoot = root.ReplaceNode(ifStatement, guardInvocation);
        return Task.FromResult(document.WithSyntaxRoot(newRoot));
    }

    private static bool IsSingleThrowBody(IfStatementSyntax ifStatement) =>
        ifStatement.Else is null &&
        ifStatement.Statement switch {
            ThrowStatementSyntax => true,
            BlockSyntax block => block.Statements.Count is 1 && block.Statements[0] is ThrowStatementSyntax,
            _ => false
        };

    private static ExpressionSyntax ExtractOperand(ExpressionSyntax condition) {
        // Unwrap parentheses
        while (condition is ParenthesizedExpressionSyntax paren) {
            condition = paren.Expression;
        }

        if (condition is not BinaryExpressionSyntax binary) {
            // Fallback - shouldn't happen if analyzer is correct
            return SyntaxFactory.IdentifierName("value");
        }

        // Handle x < 0 -> return x
        // Handle 0 > x -> return x
        return binary.Kind() switch {
            SyntaxKind.LessThanExpression when IsZeroLiteral(binary.Right) => binary.Left,
            SyntaxKind.GreaterThanExpression when IsZeroLiteral(binary.Left) => binary.Right,
            _ => binary.Left // Fallback
        };
    }

    private static bool IsZeroLiteral(ExpressionSyntax expression) {
        // Unwrap parentheses
        while (expression is ParenthesizedExpressionSyntax paren) {
            expression = paren.Expression;
        }

        return expression is LiteralExpressionSyntax { Token.ValueText: "0" };
    }
}
