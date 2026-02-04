using ANcpLua.Analyzers.Core;

namespace ANcpLua.Analyzers.CodeFixes.CodeFixes;

/// <summary>
///     Code fix for AL0031: Converts verbose operation patterns to extension methods.
/// </summary>
/// <remarks>
///     <list type="bullet">
///         <item><c>invocation.TargetMethod.Name == "name"</c> → <c>invocation.IsMethodNamed("name")</c></item>
///         <item>
///             <c>op.ConstantValue.HasValue &amp;&amp; op.ConstantValue.Value is T name</c> →
///             <c>op.TryGetConstantValue&lt;T&gt;(out var name)</c>
///         </item>
///     </list>
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
        // Pattern 1: TargetMethod.Name == "name" → IsMethodNamed
        if (TryGetMethodNameComparison(binary, out var invocationExpr, out var methodName)) {
            // Only offer fix if we can determine the containing type from the method name
            var containingType = GetContainingTypeFromMethodName(methodName);
            if (containingType is null) {
                return null;
            }

            return CodeAction.Create(
                CodeFixResources.AL0031CodeFixTitle,
                _ => ConvertToIsMethodNamed(document, binary, root, invocationExpr, containingType, methodName),
                nameof(Al0031UseOperationExtensionsCodeFixProvider) + "_IsMethodNamed");
        }

        // Pattern 2: op.ConstantValue.HasValue && op.ConstantValue.Value is T name → TryGetConstantValue<T>
        if (TryGetConstantValuePattern(binary, out var operationExpr, out var typeName, out var variableName)) {
            return CodeAction.Create(
                CodeFixResources.AL0031CodeFixTitleTryGetConstantValue,
                _ => ConvertToTryGetConstantValue(document, binary, root, operationExpr, typeName, variableName),
                nameof(Al0031UseOperationExtensionsCodeFixProvider) + "_TryGetConstantValue");
        }

        return null;
    }

    /// <summary>
    ///     Attempts to determine the containing type name from the method name.
    ///     Returns null if the containing type cannot be determined (code fix should not be offered).
    /// </summary>
    private static string? GetContainingTypeFromMethodName(string methodName) =>
        // Map well-known method names to their containing types.
        // Only offer code fix for methods where we can confidently determine the containing type.
        methodName switch {
            // Object methods
            "ToString" or "GetHashCode" or "Equals" or "ReferenceEquals" or "GetType" => "Object",

            // IDisposable
            "Dispose" => "IDisposable",

            // IAsyncDisposable
            "DisposeAsync" => "IAsyncDisposable",

            // Common collection methods - too ambiguous, don't offer fix
            "Add" or "Remove" or "Clear" or "Contains" => null,

            // Task methods
            "ConfigureAwait" or "GetAwaiter" => "Task",
            "Wait" or "WaitAll" or "WaitAny" or "WhenAll" or "WhenAny" => "Task",

            // String methods
            "IsNullOrEmpty" or "IsNullOrWhiteSpace" or "Format" or "Join" or "Concat" => "String",

            // LINQ methods - these come from Enumerable static class
            "Select" or "Where" or "OrderBy" or "OrderByDescending" or "GroupBy" or "First" or "FirstOrDefault"
                or "Single" or "SingleOrDefault" or "Last" or "LastOrDefault" or "Any" or "All" or "Count"
                or "ToList" or "ToArray" or "ToDictionary" or "Aggregate" or "Sum" or "Max" or "Min" or "Average"
                or "Skip" or "Take" or "SkipWhile" or "TakeWhile" or "Distinct" or "Union" or "Intersect" or "Except"
                or "Zip" or "SelectMany" or "Cast" or "OfType" => "Enumerable",

            // Unknown method - cannot determine containing type, don't offer fix
            _ => null
        };

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
        SyntaxNode binary,
        SyntaxNode root,
        ExpressionSyntax invocationExpr,
        string containingType,
        string methodName) {
        var isNegated = binary.IsKind(SyntaxKind.NotEqualsExpression);

        // Create: invocation.IsMethodNamed("ContainingType", "methodName")
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
                            SyntaxFactory.Literal(containingType))),
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

    /// <summary>
    ///     Detects pattern: op.ConstantValue.HasValue &amp;&amp; op.ConstantValue.Value is T name
    /// </summary>
    private static bool TryGetConstantValuePattern(
        BinaryExpressionSyntax binary,
        [NotNullWhen(true)] out ExpressionSyntax? operationExpr,
        [NotNullWhen(true)] out string? typeName,
        [NotNullWhen(true)] out string? variableName) {
        operationExpr = null;
        typeName = null;
        variableName = null;

        // Must be && expression
        if (!binary.IsKind(SyntaxKind.LogicalAndExpression)) {
            return false;
        }

        // Find the HasValue check and is pattern
        IsPatternExpressionSyntax? isPattern = null;

        if (TryExtractConstantValueHasValue(binary.Left, out var hasValueExpr) &&
            binary.Right is IsPatternExpressionSyntax rightPattern) {
            isPattern = rightPattern;
        } else if (TryExtractConstantValueHasValue(binary.Right, out hasValueExpr) &&
                   binary.Left is IsPatternExpressionSyntax leftPattern) {
            isPattern = leftPattern;
        }

        if (hasValueExpr is null || isPattern is null) {
            return false;
        }

        // Check that is pattern is on .ConstantValue.Value
        if (!TryExtractConstantValueValue(isPattern.Expression, out var valueExpr)) {
            return false;
        }

        // Verify both reference the same operation
        var hasValueOp = GetOperationFromConstantValue(hasValueExpr);
        var valueOp = GetOperationFromConstantValue(valueExpr);

        if (hasValueOp is null || valueOp is null ||
            hasValueOp.ToString() != valueOp.ToString()) {
            return false;
        }

        // Extract type and variable name from the pattern
        if (!TryExtractTypeAndVariable(isPattern.Pattern, out typeName, out variableName)) {
            return false;
        }

        operationExpr = hasValueOp;
        return true;
    }

    private static bool TryExtractConstantValueHasValue(
        ExpressionSyntax expr,
        [NotNullWhen(true)] out ExpressionSyntax? constantValueExpr) {
        constantValueExpr = null;

        // Pattern: X.ConstantValue.HasValue
        if (expr is MemberAccessExpressionSyntax {
            Name.Identifier.Text: "HasValue",
            Expression: MemberAccessExpressionSyntax {
                Name.Identifier.Text: "ConstantValue"
            } constantValue
        }) {
            constantValueExpr = constantValue;
            return true;
        }

        return false;
    }

    private static bool TryExtractConstantValueValue(
        ExpressionSyntax expr,
        [NotNullWhen(true)] out ExpressionSyntax? constantValueExpr) {
        constantValueExpr = null;

        // Pattern: X.ConstantValue.Value
        if (expr is MemberAccessExpressionSyntax {
            Name.Identifier.Text: "Value",
            Expression: MemberAccessExpressionSyntax {
                Name.Identifier.Text: "ConstantValue"
            } constantValue
        }) {
            constantValueExpr = constantValue;
            return true;
        }

        return false;
    }

    private static ExpressionSyntax? GetOperationFromConstantValue(ExpressionSyntax constantValueExpr) {
        // constantValueExpr is X.ConstantValue, we want X
        if (constantValueExpr is MemberAccessExpressionSyntax { Expression: { } opExpr }) {
            return opExpr;
        }

        return null;
    }

    private static bool TryExtractTypeAndVariable(
        PatternSyntax pattern,
        [NotNullWhen(true)] out string? typeName,
        [NotNullWhen(true)] out string? variableName) {
        typeName = null;
        variableName = null;

        // Pattern: is T name (DeclarationPattern)
        if (pattern is DeclarationPatternSyntax { Type: { } type, Designation: SingleVariableDesignationSyntax { Identifier: { } id } }) {
            typeName = type.ToString();
            variableName = id.Text;
            return true;
        }

        // Pattern: is T (without variable) - use "value" as default
        if (pattern is TypePatternSyntax { Type: { } typeOnly }) {
            typeName = typeOnly.ToString();
            variableName = "value";
            return true;
        }

        return false;
    }

    private static Task<Document> ConvertToTryGetConstantValue(
        Document document,
        SyntaxNode binary,
        SyntaxNode root,
        ExpressionSyntax operationExpr,
        string typeName,
        string variableName) {
        // Create: operation.TryGetConstantValue<T>(out var name)
        var tryGetCall = SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                operationExpr.WithoutTrivia(),
                SyntaxFactory.GenericName(
                    SyntaxFactory.Identifier("TryGetConstantValue"),
                    SyntaxFactory.TypeArgumentList(
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.ParseTypeName(typeName))))),
            SyntaxFactory.ArgumentList(
                SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.Argument(
                            SyntaxFactory.DeclarationExpression(
                                SyntaxFactory.IdentifierName("var"),
                                SyntaxFactory.SingleVariableDesignation(
                                    SyntaxFactory.Identifier(variableName))))
                        .WithRefKindKeyword(SyntaxFactory.Token(SyntaxKind.OutKeyword)))));

        var newExpression = tryGetCall.WithTriviaFrom(binary);
        var newRoot = root.ReplaceNode(binary, newExpression);
        return Task.FromResult(document.WithSyntaxRoot(newRoot));
    }
}
