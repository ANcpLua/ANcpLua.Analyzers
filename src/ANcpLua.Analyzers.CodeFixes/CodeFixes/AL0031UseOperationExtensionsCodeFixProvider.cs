using ANcpLua.Analyzers.Core;

namespace ANcpLua.Analyzers.CodeFixes.CodeFixes;

/// <summary>
///     Code fix for AL0031: Converts verbose operation patterns to extension methods.
/// </summary>
/// <remarks>
///     <c>invocation.TargetMethod.Name == "name"</c> → <c>invocation.IsMethodNamed("name")</c>
/// </remarks>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(Al0031UseOperationExtensionsCodeFixProvider))]
[Shared]
public sealed partial class Al0031UseOperationExtensionsCodeFixProvider : AlCodeFixProvider<BinaryExpressionSyntax> {
    public override ImmutableArray<string> FixableDiagnosticIds => [DiagnosticIds.UseOperationExtensions];

    protected override CodeAction? CreateCodeAction(
        Document document,
        BinaryExpressionSyntax binary,
        SyntaxNode root,
        Diagnostic diagnostic) {
        // Only handle TargetMethod.Name comparisons (not ConstantValue.HasValue which is more complex)
        if (!TryGetMethodNameComparison(binary, out var invocationExpr, out var methodName)) {
            return null;
        }

        return CodeAction.Create(
            CodeFixResources.AL0031CodeFixTitle,
            _ => ConvertToIsMethodNamed(document, binary, root, invocationExpr, methodName),
            nameof(Al0031UseOperationExtensionsCodeFixProvider));
    }

    private static bool TryGetMethodNameComparison(
        BinaryExpressionSyntax binary,
        [NotNullWhen(true)] out ExpressionSyntax? invocationExpr,
        [NotNullWhen(true)] out string? methodName) {
        invocationExpr = null;
        methodName = null;

        // Look for pattern: X.TargetMethod.Name == "string" or "string" == X.TargetMethod.Name
        var (memberAccess, literal) = GetMemberAccessAndLiteral(binary);
        if (memberAccess is null || literal is null) {
            return false;
        }

        // Check for .TargetMethod.Name pattern
        if (memberAccess is {
                Name.Identifier.Text: "Name",
                Expression: MemberAccessExpressionSyntax { Name.Identifier.Text: "TargetMethod" } targetMethodAccess
            }) {
            invocationExpr = targetMethodAccess.Expression;
            methodName = literal.Token.ValueText;
            return true;
        }

        return false;
    }

    private static (MemberAccessExpressionSyntax? memberAccess, LiteralExpressionSyntax? literal)
        GetMemberAccessAndLiteral(BinaryExpressionSyntax binary) {
        if (binary is {
                Left: MemberAccessExpressionSyntax leftMember,
                Right: LiteralExpressionSyntax rightLiteral
            } &&
            rightLiteral.IsKind(SyntaxKind.StringLiteralExpression)) {
            return (leftMember, rightLiteral);
        }

        if (binary is {
                Right: MemberAccessExpressionSyntax rightMember,
                Left: LiteralExpressionSyntax leftLiteral
            } &&
            leftLiteral.IsKind(SyntaxKind.StringLiteralExpression)) {
            return (rightMember, leftLiteral);
        }

        return (null, null);
    }

    private static Task<Document> ConvertToIsMethodNamed(
        Document document,
        BinaryExpressionSyntax binary,
        SyntaxNode root,
        ExpressionSyntax invocationExpr,
        string methodName) {
        var isNegated = binary.IsKind(SyntaxKind.NotEqualsExpression);

        // Create: invocation.IsMethodNamed("ContainingType", "methodName")
        // Note: Using empty string for containing type since we can't determine it from syntax.
        // User should replace "" with the actual containing type name for stricter matching.
        var isMethodNamedCall = SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                invocationExpr.WithoutTrivia(),
                SyntaxFactory.IdentifierName("IsMethodNamed")),
            SyntaxFactory.ArgumentList(
                SyntaxFactory.SeparatedList([
                    SyntaxFactory.Argument(
                        SyntaxFactory.LiteralExpression(
                            SyntaxKind.StringLiteralExpression,
                            SyntaxFactory.Literal(""))),
                    SyntaxFactory.Argument(
                        SyntaxFactory.LiteralExpression(
                            SyntaxKind.StringLiteralExpression,
                            SyntaxFactory.Literal(methodName)))
                ])));

        ExpressionSyntax newExpression = isNegated
            ? SyntaxFactory.PrefixUnaryExpression(SyntaxKind.LogicalNotExpression, isMethodNamedCall)
            : isMethodNamedCall;

        newExpression = newExpression.WithTriviaFrom(binary);

        var newRoot = root.ReplaceNode(binary, newExpression);
        return Task.FromResult(document.WithSyntaxRoot(newRoot));
    }
}
