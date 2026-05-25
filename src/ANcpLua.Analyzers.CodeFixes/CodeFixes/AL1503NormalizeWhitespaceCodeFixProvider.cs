using ANcpLua.Analyzers.Analyzers;

namespace ANcpLua.Analyzers.CodeFixes.CodeFixes;

/// <summary>
///     Code fix provider for AL1503 - removes NormalizeWhitespace() from the call chain.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(Al1503NormalizeWhitespaceCodeFixProvider))]
[Shared]
public sealed partial class Al1503NormalizeWhitespaceCodeFixProvider : CodeFixProvider {
    public override ImmutableArray<string> FixableDiagnosticIds =>
        [Al1503NormalizeWhitespaceAnalyzer.DiagnosticId];

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context) {
        if (await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false) is not
            { } root) {
            return;
        }

        foreach (var diagnostic in context.Diagnostics) {
            var node = root.FindNode(diagnostic.Location.SourceSpan);
            if (node.FirstAncestorOrSelf<InvocationExpressionSyntax>() is not { } invocation) {
                continue;
            }

            if (IsTextOutputInvocation(invocation)) {
                continue;
            }

            context.RegisterCodeFix(
                CodeAction.Create(
                    CodeFixResources.AL1503CodeFixTitle,
                    _ => RemoveNormalizeWhitespaceAsync(context.Document, invocation, root),
                    nameof(CodeFixResources.AL1503CodeFixTitle)),
                diagnostic);
        }
    }

    private static Task<Document> RemoveNormalizeWhitespaceAsync(
        Document document,
        InvocationExpressionSyntax invocation,
        SyntaxNode root) {
        // invocation is: receiver.NormalizeWhitespace(args)
        // We want to replace it with just: receiver
        if (invocation.Expression is MemberAccessExpressionSyntax memberAccess) {
            var receiver = memberAccess.Expression.WithTriviaFrom(invocation);
            var newRoot = root.ReplaceNode(invocation, receiver);
            return Task.FromResult(document.WithSyntaxRoot(newRoot));
        }

        return Task.FromResult(document);
    }

    private static bool IsTextOutputInvocation(InvocationExpressionSyntax invocation) {
        if (invocation.Parent is not InvocationExpressionSyntax parentInvocation) {
            return false;
        }

        if (parentInvocation.Expression is not MemberAccessExpressionSyntax memberAccess) {
            return false;
        }

        if (!IsTextOutputMethod(memberAccess.Name.Identifier.ValueText)) {
            return false;
        }

        var targetExpression = SkipOutputInvocationWrappers(memberAccess.Expression);
        return targetExpression == invocation;
    }

    private static ExpressionSyntax SkipOutputInvocationWrappers(ExpressionSyntax expression) {
        while (true) {
            switch (expression) {
                case ParenthesizedExpressionSyntax parenthesized:
                    expression = parenthesized.Expression;
                    continue;
                case CastExpressionSyntax castExpression:
                    expression = castExpression.Expression;
                    continue;
                default:
                    return expression;
            }
        }
    }

    private static bool IsTextOutputMethod(string methodName) =>
        string.Equals(methodName, "ToFullString", StringComparison.Ordinal) ||
        string.Equals(methodName, "ToString", StringComparison.Ordinal);
}
