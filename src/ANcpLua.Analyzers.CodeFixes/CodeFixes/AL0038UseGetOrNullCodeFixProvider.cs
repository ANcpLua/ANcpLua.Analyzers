using ANcpLua.Analyzers.Core;

namespace ANcpLua.Analyzers.CodeFixes.CodeFixes;

/// <summary>
///     Code fix for AL0038: Converts TryGetValue ternary patterns to GetOrNull/GetOrDefault.
/// </summary>
/// <remarks>
///     <list type="bullet">
///         <item><c>dict.TryGetValue(key, out var v) ? v : null</c> → <c>dict.GetOrNull(key)</c></item>
///         <item><c>dict.TryGetValue(key, out var v) ? v : default</c> → <c>dict.GetOrNull(key)</c></item>
///     </list>
/// </remarks>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(Al0038UseGetOrNullCodeFixProvider))]
[Shared]
public sealed partial class Al0038UseGetOrNullCodeFixProvider
    : AlCodeFixProvider<ConditionalExpressionSyntax> {
    /// <summary>Gets the diagnostic IDs this code fix can fix.</summary>
    public override ImmutableArray<string> FixableDiagnosticIds => [DiagnosticIds.UseGetOrNull];

    /// <summary>Creates the code action for this fix.</summary>
    protected override CodeAction CreateCodeAction(
        Document document,
        ConditionalExpressionSyntax conditional,
        SyntaxNode root,
        Diagnostic diagnostic) =>
        CodeAction.Create(
            CodeFixResources.AL0038CodeFixTitle,
            _ => ConvertToExtension(document, conditional, root),
            nameof(Al0038UseGetOrNullCodeFixProvider));

    private static Task<Document> ConvertToExtension(
        Document document,
        ConditionalExpressionSyntax conditional,
        SyntaxNode root) {
        // Extract the TryGetValue invocation from condition
        var condition = conditional.Condition;
        while (condition is ParenthesizedExpressionSyntax paren) {
            condition = paren.Expression;
        }

        if (condition is not InvocationExpressionSyntax tryGetValueInvocation) {
            return Task.FromResult(document);
        }

        // Get dictionary and key
        var (dict, key) = ExtractDictAndKey(tryGetValueInvocation);
        if (dict is null || key is null) {
            return Task.FromResult(document);
        }

        // Determine extension name based on WhenFalse
        var extensionName = IsNullOrDefault(conditional.WhenFalse) ? "GetOrNull" : "GetOrDefault";

        // Create: dict.GetOrNull(key) or dict.GetOrDefault(key)
        var newExpression = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    dict.WithoutTrivia(),
                    SyntaxFactory.IdentifierName(extensionName)),
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.Argument(key.WithoutTrivia()))))
            .WithTriviaFrom(conditional);

        var newRoot = root.ReplaceNode(conditional, newExpression);
        return Task.FromResult(document.WithSyntaxRoot(newRoot));
    }

    private static (ExpressionSyntax? dict, ExpressionSyntax? key) ExtractDictAndKey(
        InvocationExpressionSyntax invocation) {
        // Pattern: dict.TryGetValue(key, out var result)
        if (invocation.Expression is not MemberAccessExpressionSyntax {
            Name.Identifier.Text: "TryGetValue"
        } memberAccess) {
            return (null, null);
        }

        var dict = memberAccess.Expression;

        // Get the first argument (the key)
        if (invocation.ArgumentList.Arguments.Count < 1) {
            return (null, null);
        }

        var key = invocation.ArgumentList.Arguments[0].Expression;
        return (dict, key);
    }

    private static bool IsNullOrDefault(ExpressionSyntax expression) =>
        expression switch {
            LiteralExpressionSyntax literal => literal.Kind() is SyntaxKind.NullLiteralExpression
                or SyntaxKind.DefaultLiteralExpression,
            DefaultExpressionSyntax => true,
            _ => false
        };
}
