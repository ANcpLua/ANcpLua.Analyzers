using ANcpLua.Analyzers.Analyzers;
// ReSharper disable All

namespace ANcpLua.Analyzers.CodeFixes.CodeFixes;

/// <summary>
///     Code fix for AL1011: Normalizes null-guards to Throw (Throw.IfNull), BCL (ThrowIfNull),
///     or portable (coalesce) form.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(Al1011NormalizeNullGuardStyleCodeFixProvider))]
[Shared]
public sealed partial class Al1011NormalizeNullGuardStyleCodeFixProvider : AlCodeFixProvider<IfStatementSyntax> {
    public override ImmutableArray<string> FixableDiagnosticIds => [Al1011NormalizeNullGuardStyleAnalyzer.DiagnosticId];

    protected override CodeAction? CreateCodeAction(Document document,
        IfStatementSyntax ifStatement,
        SyntaxNode root,
        Diagnostic diagnostic) {
        if (!diagnostic.Properties.TryGetValue(Al1011NormalizeNullGuardStyleAnalyzer.PropertyIdentifier,
                out var identifier) ||
            !diagnostic.Properties.TryGetValue(Al1011NormalizeNullGuardStyleAnalyzer.PropertyTypeName,
                out var typeName) ||
            !diagnostic.Properties.TryGetValue(Al1011NormalizeNullGuardStyleAnalyzer.PropertyStyle, out var style) ||
            identifier is null || typeName is null || style is null) {
            return null;
        }

        var title = style switch {
            "throw" => "Use Throw.IfNull",
            "bcl" => "Use ThrowIfNull",
            _ => "Use coalesce assignment"
        };

        return CodeAction.Create(
            title,
            ct => ApplyFixAsync(document, ifStatement, identifier, typeName, style, ct),
            nameof(Al1011NormalizeNullGuardStyleCodeFixProvider));
    }

    private static Task<Document> ApplyFixAsync(
        Document document,
        CSharpSyntaxNode ifStatement,
        string identifier,
        string typeName,
        string style,
        CancellationToken ct) {
        var newStatement = style switch {
            "throw" => CreateThrowHelperStatement(identifier),
            "bcl" => CreateBclStatement(identifier, typeName),
            _ => CreatePortableStatement(identifier, typeName)
        };

        newStatement = newStatement
            .WithLeadingTrivia(ifStatement.GetLeadingTrivia())
            .WithTrailingTrivia(ifStatement.GetTrailingTrivia());

        var newRoot = ifStatement.SyntaxTree.GetRoot(ct).ReplaceNode(ifStatement, newStatement);
        return Task.FromResult(document.WithSyntaxRoot(newRoot));
    }

    /// <summary>
    ///     Creates: Throw.IfNull(identifier);
    /// </summary>
    private static ExpressionStatementSyntax CreateThrowHelperStatement(string identifier) =>
        SyntaxFactory.ExpressionStatement(
            SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName("Throw"),
                    SyntaxFactory.IdentifierName("IfNull")),
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.Argument(SyntaxFactory.IdentifierName(identifier))))));

    /// <summary>
    ///     Creates: ArgumentNullException.ThrowIfNull(identifier);
    /// </summary>
    private static ExpressionStatementSyntax CreateBclStatement(string identifier, string typeName) =>
        SyntaxFactory.ExpressionStatement(
            SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.ParseName(typeName),
                    SyntaxFactory.IdentifierName("ThrowIfNull")),
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.Argument(SyntaxFactory.IdentifierName(identifier))))));

    /// <summary>
    ///     Creates: identifier = identifier ?? throw new ArgumentNullException(nameof(identifier));
    /// </summary>
    private static ExpressionStatementSyntax CreatePortableStatement(string identifier, string typeName) {
        var idExpr = SyntaxFactory.IdentifierName(identifier);

        var throwExpr = SyntaxFactory.ThrowExpression(
            SyntaxFactory.ObjectCreationExpression(
                SyntaxFactory.ParseTypeName(typeName),
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.Argument(
                            SyntaxFactory.InvocationExpression(
                                SyntaxFactory.IdentifierName("nameof"),
                                SyntaxFactory.ArgumentList(
                                    SyntaxFactory.SingletonSeparatedList(
                                        SyntaxFactory.Argument(idExpr))))))),
                null));

        return SyntaxFactory.ExpressionStatement(
            SyntaxFactory.AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                idExpr,
                SyntaxFactory.BinaryExpression(
                    SyntaxKind.CoalesceExpression,
                    idExpr,
                    throwExpr)));
    }
}
