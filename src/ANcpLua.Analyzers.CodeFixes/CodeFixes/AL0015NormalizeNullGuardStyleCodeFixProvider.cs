using ANcpLua.Analyzers.Core;

namespace ANcpLua.Analyzers.CodeFixes.CodeFixes;

/// <summary>
///     Code fix provider for AL0015: Normalizes simple ArgumentNullException null-guards.
///     Converts to either BCL form (ThrowIfNull) or portable form (coalesce assignment).
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AL0015NormalizeNullGuardStyleCodeFixProvider))]
[Shared]
public sealed class AL0015NormalizeNullGuardStyleCodeFixProvider : ALCodeFixProvider<IfStatementSyntax>
{
    public override ImmutableArray<string> FixableDiagnosticIds =>
        [DiagnosticIds.NormalizeNullGuardStyle];

    protected override CodeAction CreateCodeAction(Document document, IfStatementSyntax ifStatement, SyntaxNode root, Diagnostic diagnostic)
    {
        // Extract properties from the diagnostic
        var properties = diagnostic.Properties;
        var identifierName = properties["identifierName"] ?? "";
        var modeStr = properties["mode"] ?? "portable";
        var hasThrowIfNullStr = properties["hasThrowIfNull"] ?? "false";
        var hasThrowIfNull = bool.TryParse(hasThrowIfNullStr, out var result) && result;

        // Only create fix if we have the identifier
        if (string.IsNullOrEmpty(identifierName))
            return CodeAction.Create(CodeFixResources.AL0015CodeFixTitle, _ => Task.FromResult(document), "NoOp");

        return CodeAction.Create(
            CodeFixResources.AL0015CodeFixTitle,
            ct => NormalizeNullGuard(document, ifStatement, root, identifierName, modeStr, hasThrowIfNull, ct),
            nameof(AL0015NormalizeNullGuardStyleCodeFixProvider));
    }

    private static Task<Document> NormalizeNullGuard(
        Document document,
        IfStatementSyntax ifStatement,
        SyntaxNode root,
        string identifierName,
        string mode,
        bool hasThrowIfNull,
        CancellationToken cancellationToken)
    {
        // Create the new statement based on mode
        StatementSyntax newStatement = mode == "bcl" && hasThrowIfNull
            ? CreateBclForm(identifierName)
            : CreatePortableForm(identifierName);

        // Preserve trivia from original if statement
        newStatement = newStatement
            .WithLeadingTrivia(ifStatement.GetLeadingTrivia())
            .WithTrailingTrivia(ifStatement.GetTrailingTrivia());

        var newRoot = root.ReplaceNode(ifStatement, newStatement);
        return Task.FromResult(document.WithSyntaxRoot(newRoot));
    }

    private static StatementSyntax CreateBclForm(string identifierName)
    {
        // ArgumentNullException.ThrowIfNull(x);
        var invocation = SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName("ArgumentNullException"),
                SyntaxFactory.IdentifierName("ThrowIfNull")),
            SyntaxFactory.ArgumentList(
                SyntaxFactory.SeparatedList(new[]
                {
                    SyntaxFactory.Argument(SyntaxFactory.IdentifierName(identifierName))
                })));

        return SyntaxFactory.ExpressionStatement(invocation);
    }

    private static StatementSyntax CreatePortableForm(string identifierName)
    {
        // x = x ?? throw new ArgumentNullException(nameof(x));
        var identifier = SyntaxFactory.IdentifierName(identifierName);

        var newArgEx = SyntaxFactory.ObjectCreationExpression(
            SyntaxFactory.IdentifierName("ArgumentNullException"),
            SyntaxFactory.ArgumentList(
                SyntaxFactory.SeparatedList(new[]
                {
                    SyntaxFactory.Argument(
                        SyntaxFactory.InvocationExpression(
                            SyntaxFactory.IdentifierName("nameof"),
                            SyntaxFactory.ArgumentList(
                                SyntaxFactory.SingletonSeparatedList(
                                    SyntaxFactory.Argument(
                                        SyntaxFactory.IdentifierName(identifierName))))))
                })),
            null);

        var throwExpr = SyntaxFactory.ThrowExpression(newArgEx);

        var coalesceExpr = SyntaxFactory.BinaryExpression(
            SyntaxKind.CoalesceExpression,
            identifier,
            throwExpr);

        var assignment = SyntaxFactory.AssignmentExpression(
            SyntaxKind.SimpleAssignmentExpression,
            identifier,
            coalesceExpr);

        return SyntaxFactory.ExpressionStatement(assignment);
    }
}
