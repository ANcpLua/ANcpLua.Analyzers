using ANcpLua.Analyzers.Core;

namespace ANcpLua.Analyzers.CodeFixes.CodeFixes;

/// <summary>
///     Code fix for AL0029: Converts GetAttributes() LINQ patterns to HasAttribute extension.
/// </summary>
/// <remarks>
///     <c>symbol.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == "X")</c> →
///     <c>symbol.HasAttribute("X")</c>
/// </remarks>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(Al0029UseHasAttributeCodeFixProvider))]
[Shared]
public sealed class Al0029UseHasAttributeCodeFixProvider : CodeFixProvider {
    public override ImmutableArray<string> FixableDiagnosticIds => [DiagnosticIds.UseHasAttribute];

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context) {
        if (await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false) is not { } root) {
            return;
        }

        var diagnostic = context.Diagnostics.First();
        var diagnosticSpan = diagnostic.Location.SourceSpan;
        var node = root.FindNode(diagnosticSpan);

        // Handle LINQ invocation pattern: symbol.GetAttributes().Any(...)
        if (node.FirstAncestorOrSelf<InvocationExpressionSyntax>() is { } invocation &&
            TryExtractAttributeInfo(invocation, out var symbolExpr, out var attributeName)) {
            context.RegisterCodeFix(
                CodeAction.Create(
                    CodeFixResources.AL0029CodeFixTitle,
                    _ => ConvertToHasAttribute(context.Document, root, invocation, symbolExpr, attributeName),
                    nameof(Al0029UseHasAttributeCodeFixProvider)),
                diagnostic);
        }
    }

    private static bool TryExtractAttributeInfo(
        InvocationExpressionSyntax invocation,
        [NotNullWhen(true)] out ExpressionSyntax? symbolExpr,
        [NotNullWhen(true)] out string? attributeName) {
        symbolExpr = null;
        attributeName = null;

        // Pattern: symbol.GetAttributes().Any/FirstOrDefault/Where/Count(lambda)
        if (invocation.Expression is not MemberAccessExpressionSyntax { Name.Identifier.Text: "Any" or "FirstOrDefault" or "Where" or "Count" } linqAccess) {
            return false;
        }

        // Get the GetAttributes() call
        if (linqAccess.Expression is not InvocationExpressionSyntax getAttributesInvocation) {
            return false;
        }

        if (getAttributesInvocation.Expression is not MemberAccessExpressionSyntax { Name.Identifier.Text: "GetAttributes" } getAttributesAccess) {
            return false;
        }

        symbolExpr = getAttributesAccess.Expression;

        // Extract attribute name from the lambda argument
        if (invocation.ArgumentList.Arguments.Count is 0) {
            return false;
        }

        var lambdaArg = invocation.ArgumentList.Arguments[0].Expression;
        attributeName = ExtractAttributeNameFromLambda(lambdaArg);

        return attributeName is not null;
    }

    private static string? ExtractAttributeNameFromLambda(ExpressionSyntax? lambda) {
        // Handle: a => a.AttributeClass?.ToDisplayString() == "X"
        // Handle: a => a.AttributeClass?.Name == "X"
        if (lambda is not SimpleLambdaExpressionSyntax { Body: BinaryExpressionSyntax binary }) {
            return null;
        }

        // Find the string literal
        var literal = binary.Left as LiteralExpressionSyntax ?? binary.Right as LiteralExpressionSyntax;
        if (literal is null || !literal.IsKind(SyntaxKind.StringLiteralExpression)) {
            return null;
        }

        return literal.Token.ValueText;
    }

    private static Task<Document> ConvertToHasAttribute(
        Document document,
        SyntaxNode root,
        InvocationExpressionSyntax invocation,
        ExpressionSyntax symbolExpr,
        string attributeName) {
        // Create: symbol.HasAttribute("attributeName")
        var newExpression = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    symbolExpr.WithoutTrivia(),
                    SyntaxFactory.IdentifierName("HasAttribute")),
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.Argument(
                            SyntaxFactory.LiteralExpression(
                                SyntaxKind.StringLiteralExpression,
                                SyntaxFactory.Literal(attributeName))))))
            .WithTriviaFrom(invocation);

        var newRoot = root.ReplaceNode(invocation, newExpression);
        return Task.FromResult(document.WithSyntaxRoot(newRoot));
    }
}
