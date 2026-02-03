using ANcpLua.Analyzers.Analyzers;

namespace ANcpLua.Analyzers.CodeFixes.CodeFixes;

/// <summary>
///     Code fix for AL0005: Converts Span equality to SequenceEqual.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(Al0005CodeFixProvider))]
[Shared]
public sealed partial class Al0005CodeFixProvider : AlCodeFixProvider<BinaryExpressionSyntax> {
    public override ImmutableArray<string> FixableDiagnosticIds =>
        [Al0004ToAl0005SpanComparisonAnalyzer.DiagnosticIdAl0005];

    protected override CodeAction CreateCodeAction(Document document, BinaryExpressionSyntax syntax, SyntaxNode root,
        Diagnostic diagnostic) =>
        CodeAction.Create(
            CodeFixResources.AL0005CodeFixTitle,
            _ => UseSequenceEqual(document, syntax, root),
            nameof(CodeFixResources.AL0005CodeFixTitle));

    private static Task<Document> UseSequenceEqual(
        Document document,
        BinaryExpressionSyntax binary,
        SyntaxNode root) {
        var sequenceEqual = SyntaxFactory.IdentifierName("SequenceEqual");
        var memberAccess = SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            binary.Left,
            sequenceEqual);
        var argument = SyntaxFactory.Argument(binary.Right);
        var argumentList = SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(argument));
        ExpressionSyntax invocation = SyntaxFactory.InvocationExpression(memberAccess, argumentList);

        // For != comparisons, negate the result
        var isNotEquals = binary.IsKind(SyntaxKind.NotEqualsExpression);
        if (isNotEquals) {
            invocation = SyntaxFactory.PrefixUnaryExpression(
                SyntaxKind.LogicalNotExpression,
                invocation);
        }

        invocation = invocation.WithTriviaFrom(binary);
        return Task.FromResult(document.WithSyntaxRoot(root.ReplaceNode(binary, invocation)));
    }
}
