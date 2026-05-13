using ANcpLua.Analyzers.Analyzers;

namespace ANcpLua.Analyzers.CodeFixes.CodeFixes;

/// <summary>
///     Code fix for AL0050: Converts if (guid == Guid.Empty) throw to Guard.NotEmpty().
/// </summary>
/// <remarks>
///     <list type="bullet">
///         <item><c>if (id == Guid.Empty) throw new ArgumentException(...)</c> -> <c>Guard.NotEmpty(id);</c></item>
///     </list>
/// </remarks>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(Al0050UseGuardNotEmptyGuidCodeFixProvider))]
[Shared]
public sealed partial class Al0050UseGuardNotEmptyGuidCodeFixProvider
    : AlCodeFixProvider<IfStatementSyntax> {
    private const string PropertyExpression = "Expression";

    /// <summary>Gets the diagnostic IDs this code fix can fix.</summary>
    public override ImmutableArray<string> FixableDiagnosticIds => [Al0050UseGuardNotEmptyGuidAnalyzer.DiagnosticId];

    /// <summary>Creates the code action for this fix.</summary>
    protected override CodeAction? CreateCodeAction(
        Document document,
        IfStatementSyntax ifStatement,
        SyntaxNode root,
        Diagnostic diagnostic) {
        if (!diagnostic.Properties.TryGetValue(PropertyExpression, out var expressionText) ||
            expressionText is null or "") {
            return null;
        }

        return CodeAction.Create(
            CodeFixResources.AL0050CodeFixTitle,
            ct => ConvertToGuardNotEmpty(document, ifStatement, expressionText, ct),
            nameof(Al0050UseGuardNotEmptyGuidCodeFixProvider));
    }

    private static Task<Document> ConvertToGuardNotEmpty(
        Document document,
        CSharpSyntaxNode ifStatement,
        string expression,
        CancellationToken ct) {
        // Create: Guard.NotEmpty(identifier);
        var newStatement = SyntaxFactory.ExpressionStatement(
                SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName("Guard"),
                        SyntaxFactory.IdentifierName("NotEmpty")),
                    SyntaxFactory.ArgumentList(
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.Argument(SyntaxFactory.ParseExpression(expression))))))
            .WithLeadingTrivia(ifStatement.GetLeadingTrivia())
            .WithTrailingTrivia(ifStatement.GetTrailingTrivia());

        var newRoot = ifStatement.SyntaxTree.GetRoot(ct).ReplaceNode(ifStatement, newStatement);
        return Task.FromResult(document.WithSyntaxRoot(newRoot));
    }
}
