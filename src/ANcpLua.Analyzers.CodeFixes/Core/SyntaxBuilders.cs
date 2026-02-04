using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ANcpLua.Analyzers;

/// <summary>
/// Fluent builders for common syntax patterns in code fixes.
/// </summary>
internal static partial class SyntaxBuilders
{
    /// <summary>
    /// Builds an extension method invocation: receiver.MethodName(args)
    /// </summary>
    public static InvocationExpressionSyntax ExtensionCall(
        ExpressionSyntax receiver,
        string methodName,
        params ExpressionSyntax[] arguments)
    {
        var memberAccess = SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            receiver,
            SyntaxFactory.IdentifierName(methodName));

        var argList = arguments.Length is 0
            ? SyntaxFactory.ArgumentList()
            : SyntaxFactory.ArgumentList(
                SyntaxFactory.SeparatedList(
                    arguments.Select(SyntaxFactory.Argument)));

        return SyntaxFactory.InvocationExpression(memberAccess, argList);
    }

    /// <summary>
    /// Builds a static method invocation: TypeName.MethodName(args)
    /// </summary>
    public static InvocationExpressionSyntax StaticCall(
        string typeName,
        string methodName,
        params ExpressionSyntax[] arguments)
    {
        var memberAccess = SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            SyntaxFactory.IdentifierName(typeName),
            SyntaxFactory.IdentifierName(methodName));

        var argList = arguments.Length is 0
            ? SyntaxFactory.ArgumentList()
            : SyntaxFactory.ArgumentList(
                SyntaxFactory.SeparatedList(
                    arguments.Select(SyntaxFactory.Argument)));

        return SyntaxFactory.InvocationExpression(memberAccess, argList);
    }

    /// <summary>
    /// Builds a nameof expression: nameof(expression)
    /// </summary>
    public static InvocationExpressionSyntax NameOf(ExpressionSyntax expression) =>
        SyntaxFactory.InvocationExpression(
            SyntaxFactory.IdentifierName("nameof"),
            SyntaxFactory.ArgumentList(
                SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.Argument(expression))));

    /// <summary>
    /// Builds a nameof expression: nameof(name)
    /// </summary>
    public static InvocationExpressionSyntax NameOf(string name) =>
        NameOf(SyntaxFactory.IdentifierName(name));
}
