using ANcpLua.Analyzers.Analyzers;

namespace ANcpLua.Analyzers.CodeFixes.CodeFixes;

/// <summary>
///     Code fix for AL1214: Converts if (x == 0) throw to Guard.NotZero(x).
/// </summary>
/// <remarks>
///     <list type="bullet">
///         <item><c>if (x == 0) throw new ArgumentOutOfRangeException(...)</c> becomes <c>Guard.NotZero(x);</c></item>
///         <item><c>if (0 == x) throw new ArgumentOutOfRangeException(...)</c> becomes <c>Guard.NotZero(x);</c></item>
///     </list>
/// </remarks>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(Al1214UseGuardNotZeroCodeFixProvider))]
[Shared]
public sealed partial class Al1214UseGuardNotZeroCodeFixProvider
    : AlCodeFixProvider<IfStatementSyntax> {
    private const string ExtensionsNamespace = "ANcpLua.Roslyn.Utilities";

    /// <summary>Property key for the parameter identifier.</summary>
    private const string PropertyIdentifier = "Id";

    /// <summary>Gets the diagnostic IDs this code fix can fix.</summary>
    public override ImmutableArray<string> FixableDiagnosticIds => [Al1214UseGuardNotZeroAnalyzer.DiagnosticId];

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
            CodeFixResources.AL1214CodeFixTitle,
            _ => ConvertToGuardNotZero(document, ifStatement, root, identifier),
            Al1214UseGuardNotZeroAnalyzer.DiagnosticId);
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
