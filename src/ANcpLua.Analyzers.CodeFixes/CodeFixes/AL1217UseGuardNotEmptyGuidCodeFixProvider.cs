using ANcpLua.Analyzers.Analyzers;

namespace ANcpLua.Analyzers.CodeFixes.CodeFixes;

/// <summary>
///     Code fix for AL1217: Converts if (guid == Guid.Empty) throw to Guard.NotEmpty().
/// </summary>
/// <remarks>
///     <list type="bullet">
///         <item><c>if (id == Guid.Empty) throw new ArgumentException(...)</c> -> <c>Guard.NotEmpty(id);</c></item>
///     </list>
/// </remarks>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(Al1217UseGuardNotEmptyGuidCodeFixProvider))]
[Shared]
public sealed partial class Al1217UseGuardNotEmptyGuidCodeFixProvider
    : AlCodeFixProvider<IfStatementSyntax> {
    private const string ExtensionsNamespace = "ANcpLua.Roslyn.Utilities";

    private const string PropertyExpression = "Expression";

    /// <summary>Gets the diagnostic IDs this code fix can fix.</summary>
    public override ImmutableArray<string> FixableDiagnosticIds => [Al1217UseGuardNotEmptyGuidAnalyzer.DiagnosticId];

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
            CodeFixResources.AL1217CodeFixTitle,
            ct => ConvertToGuardNotEmpty(document, ifStatement, expressionText, ct),
            Al1217UseGuardNotEmptyGuidAnalyzer.DiagnosticId);
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
        newRoot = AddUsingIfMissing(newRoot, ExtensionsNamespace);
        return Task.FromResult(document.WithSyntaxRoot(newRoot));
    }

    private static SyntaxNode AddUsingIfMissing(SyntaxNode root, string namespaceName) {
        if (root is not CompilationUnitSyntax compilationUnit) {
            return root;
        }

        if (compilationUnit.Usings.Any(u => u.Name?.ToString() == namespaceName)) {
            return root;
        }

        var newUsing = SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(namespaceName))
            .WithTrailingTrivia(DetectEndOfLine(compilationUnit));

        return compilationUnit.AddUsings(newUsing);
    }

    private static SyntaxTrivia DetectEndOfLine(CompilationUnitSyntax compilationUnit) {
        // Preserve the file's CRLF/LF convention so the inserted using does not corrupt line endings.
        foreach (var trivia in compilationUnit.DescendantTrivia()) {
            if (trivia.IsKind(SyntaxKind.EndOfLineTrivia)) {
                return trivia;
            }
        }

        return SyntaxFactory.LineFeed;
    }
}
