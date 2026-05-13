using ANcpLua.Analyzers.Analyzers;

namespace ANcpLua.Analyzers.CodeFixes.CodeFixes;

/// <summary>
///     Code fix for AL0051: Converts if (!Enum.IsDefined) throw to Guard.DefinedEnum().
/// </summary>
/// <remarks>
///     <list type="bullet">
///         <item><c>if (!Enum.IsDefined(typeof(MyEnum), value)) throw new ArgumentException(...)</c> -> <c>Guard.DefinedEnum(value)</c></item>
///         <item><c>if (!Enum.IsDefined&lt;MyEnum&gt;(value)) throw new ArgumentException(...)</c> -> <c>Guard.DefinedEnum(value)</c></item>
///     </list>
/// </remarks>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(Al0051UseGuardDefinedEnumCodeFixProvider))]
[Shared]
public sealed partial class Al0051UseGuardDefinedEnumCodeFixProvider
    : AlCodeFixProvider<IfStatementSyntax> {
    /// <summary>Gets the diagnostic IDs this code fix can fix.</summary>
    public override ImmutableArray<string> FixableDiagnosticIds => [Al0051UseGuardDefinedEnumAnalyzer.DiagnosticId];

    /// <summary>Creates the code action for this fix.</summary>
    protected override CodeAction CreateCodeAction(
        Document document,
        IfStatementSyntax ifStatement,
        SyntaxNode root,
        Diagnostic diagnostic) =>
        CodeAction.Create(
            CodeFixResources.AL0051CodeFixTitle,
            _ => ConvertToGuardDefinedEnum(document, ifStatement, root),
            nameof(Al0051UseGuardDefinedEnumCodeFixProvider));

    private static Task<Document> ConvertToGuardDefinedEnum(
        Document document,
        IfStatementSyntax ifStatement,
        SyntaxNode root) {
        if (!IsSingleThrowBody(ifStatement)) {
            return Task.FromResult(document);
        }

        // Extract the value argument from the condition
        var valueExpression = ExtractValueArgument(ifStatement.Condition);

        // Create: Guard.DefinedEnum(value);
        var guardInvocation = SyntaxFactory.ExpressionStatement(
                SyntaxFactory.InvocationExpression(
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.IdentifierName("Guard"),
                            SyntaxFactory.IdentifierName("DefinedEnum")),
                        SyntaxFactory.ArgumentList(
                            SyntaxFactory.SingletonSeparatedList(
                                SyntaxFactory.Argument(valueExpression.WithoutTrivia())))))
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

    private static ExpressionSyntax ExtractValueArgument(ExpressionSyntax condition) {
        // Unwrap parentheses
        while (condition is ParenthesizedExpressionSyntax paren) {
            condition = paren.Expression;
        }

        // Expect a negation: !Enum.IsDefined(...)
        if (condition is not PrefixUnaryExpressionSyntax { RawKind: (int)SyntaxKind.LogicalNotExpression } negation) {
            return SyntaxFactory.IdentifierName("value");
        }

        var operand = negation.Operand;

        // Unwrap parentheses on operand
        while (operand is ParenthesizedExpressionSyntax parenOp) {
            operand = parenOp.Expression;
        }

        // Expect an invocation: Enum.IsDefined(...)
        if (operand is not InvocationExpressionSyntax invocation) {
            return SyntaxFactory.IdentifierName("value");
        }

        var args = invocation.ArgumentList.Arguments;

        return args.Count switch {
            // Generic version: Enum.IsDefined<T>(value) - 1 argument
            // Non-generic version: Enum.IsDefined(typeof(T), value) - 2 arguments
            1 => args[0].Expression,
            >= 2 => args[1].Expression,
            _ => SyntaxFactory.IdentifierName("value")
        };

        // Fallback
    }
}
