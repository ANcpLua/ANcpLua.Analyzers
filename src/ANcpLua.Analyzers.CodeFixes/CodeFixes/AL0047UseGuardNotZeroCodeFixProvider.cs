using ANcpLua.Analyzers.Core;

namespace ANcpLua.Analyzers.CodeFixes.CodeFixes;

/// <summary>
///     Code fix for AL0047: Converts if (x == 0) throw to Guard.NotZero(x).
/// </summary>
/// <remarks>
///     <list type="bullet">
///         <item><c>if (x == 0) throw new ArgumentOutOfRangeException(...)</c> becomes <c>Guard.NotZero(x);</c></item>
///         <item><c>if (0 == x) throw new ArgumentOutOfRangeException(...)</c> becomes <c>Guard.NotZero(x);</c></item>
///     </list>
/// </remarks>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(Al0047UseGuardNotZeroCodeFixProvider))]
[Shared]
public sealed partial class Al0047UseGuardNotZeroCodeFixProvider
    : AlCodeFixProvider<IfStatementSyntax> {
    /// <summary>Property key for the parameter identifier.</summary>
    private const string PropertyIdentifier = "Id";

    /// <summary>Gets the diagnostic IDs this code fix can fix.</summary>
    public override ImmutableArray<string> FixableDiagnosticIds => [DiagnosticIds.UseGuardNotZero];

    /// <summary>Creates the code action for this fix.</summary>
    protected override CodeAction? CreateCodeAction(
        Document document,
        IfStatementSyntax ifStatement,
        SyntaxNode root,
        Diagnostic diagnostic) {
        // Get identifier from diagnostic properties
        if (!diagnostic.Properties.TryGetValue(PropertyIdentifier, out var identifier) ||
            identifier is null or "") {
            return null;
        }

        return CodeAction.Create(
            CodeFixResources.AL0047CodeFixTitle,
            _ => ConvertToGuardNotZero(document, ifStatement, root, identifier),
            nameof(Al0047UseGuardNotZeroCodeFixProvider));
    }

    private static Task<Document> ConvertToGuardNotZero(
        Document document,
        CSharpSyntaxNode ifStatement,
        SyntaxNode root,
        string identifier) {
        // Create: Guard.NotZero(identifier);
        var guardCall = SyntaxFactory.ExpressionStatement(
                SyntaxFactory.InvocationExpression(
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.IdentifierName("Guard"),
                            SyntaxFactory.IdentifierName("NotZero")),
                        SyntaxFactory.ArgumentList(
                            SyntaxFactory.SingletonSeparatedList(
                                SyntaxFactory.Argument(
                                    SyntaxFactory.IdentifierName(identifier))))))
            .WithLeadingTrivia(ifStatement.GetLeadingTrivia())
            .WithTrailingTrivia(ifStatement.GetTrailingTrivia());

        var newRoot = root.ReplaceNode(ifStatement, guardCall);
        return Task.FromResult(document.WithSyntaxRoot(newRoot));
    }
}
