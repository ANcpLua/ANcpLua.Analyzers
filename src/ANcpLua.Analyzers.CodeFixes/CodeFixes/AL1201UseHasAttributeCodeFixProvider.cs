using ANcpLua.Analyzers.Analyzers;

namespace ANcpLua.Analyzers.CodeFixes.CodeFixes;

/// <summary>
///     Code fix for AL1201: Converts GetAttributes() LINQ patterns to HasAttribute extension.
/// </summary>
/// <remarks>
///     <c>symbol.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == "X")</c> →
///     <c>symbol.HasAttribute("X")</c>
/// </remarks>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(Al1201UseHasAttributeCodeFixProvider))]
[Shared]
public sealed partial class Al1201UseHasAttributeCodeFixProvider : CodeFixProvider {
    public override ImmutableArray<string> FixableDiagnosticIds => [Al1201UseHasAttributeAnalyzer.DiagnosticId];

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
                    CodeFixResources.AL1201CodeFixTitle,
                    _ => ConvertToHasAttribute(context.Document, root, invocation, symbolExpr, attributeName),
                    Al1201UseHasAttributeAnalyzer.DiagnosticId),
                diagnostic);
        }
    }

    private static bool TryExtractAttributeInfo(
        InvocationExpressionSyntax invocation,
        [NotNullWhen(true)] out ExpressionSyntax? symbolExpr,
        [NotNullWhen(true)] out string? attributeName) {
        symbolExpr = null;
        attributeName = null;

        // Only fix Any() - other LINQ methods have different return types
        if (invocation.Expression is not MemberAccessExpressionSyntax { Name.Identifier.Text: "Any" } linqAccess) {
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
        SyntaxNode invocation,
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
