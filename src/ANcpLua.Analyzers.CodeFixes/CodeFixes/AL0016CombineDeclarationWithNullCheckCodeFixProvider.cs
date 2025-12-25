using ANcpLua.Analyzers.Core;
using Microsoft.CodeAnalysis.Editing;

namespace ANcpLua.Analyzers.CodeFixes.CodeFixes;

/// <summary>
///     Code fix for AL0016: Combines a local variable declaration with a subsequent null-check
///     into a single pattern match statement.
/// </summary>
/// <remarks>
///     Transforms:
///     <c>
///         var x = M();
///         if (x is null) return;
///     </c>
///     Into:
///     <c>
///         if (M() is not { } x) return;
///     </c>
/// </remarks>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AL0016CombineDeclarationWithNullCheckCodeFixProvider))]
[Shared]
public sealed class AL0016CombineDeclarationWithNullCheckCodeFixProvider : ALCodeFixProvider<LocalDeclarationStatementSyntax>
{
    public override ImmutableArray<string> FixableDiagnosticIds =>
        [DiagnosticIds.CombineDeclarationWithNullCheck];

    protected override CodeAction CreateCodeAction(
        Document document,
        LocalDeclarationStatementSyntax declaration,
        SyntaxNode root,
        Diagnostic diagnostic)
    {
        return CodeAction.Create(
            CodeFixResources.AL0016CodeFixTitle,
            ct => CombineDeclarationWithNullCheck(document, declaration, root, ct),
            nameof(AL0016CombineDeclarationWithNullCheckCodeFixProvider));
    }

    private static async Task<Document> CombineDeclarationWithNullCheck(
        Document document,
        LocalDeclarationStatementSyntax declaration,
        SyntaxNode root,
        CancellationToken cancellationToken)
    {
        // Extract the variable name and initializer from the declaration
        var variable = declaration.Declaration.Variables[0];
        var variableName = variable.Identifier.Text;
        var initializer = variable.Initializer!.Value;

        // Find the next statement (the if statement)
        var nextStatement = TryGetNextStatement(declaration);
        if (nextStatement is not IfStatementSyntax ifStatement)
            return document;

        // Create the pattern: not { } x
        // Build inline pattern: is not { } variableName
        // Parse the pattern as a string to ensure correct inline formatting
        var patternText = $"is not {{ }} {variableName}";
        var isPatternExpr = SyntaxFactory.ParseExpression($"_ {patternText}") as IsPatternExpressionSyntax;
        var pattern = isPatternExpr!.Pattern;

        // Create the is pattern expression with the initializer
        var newCondition = SyntaxFactory.IsPatternExpression(
            initializer.WithoutTrivia(),
            SyntaxFactory.Token(SyntaxKind.IsKeyword)
                .WithLeadingTrivia(SyntaxFactory.Space)
                .WithTrailingTrivia(SyntaxFactory.Space),
            pattern);

        // Create the new if statement using the original statement body
        // Build the if statement with explicit tokens to control formatting
        // The statement body needs a space before it, not a newline
        var statementBody = ifStatement.Statement.WithoutLeadingTrivia();
        var newIfStatement = SyntaxFactory.IfStatement(
                SyntaxFactory.Token(SyntaxKind.IfKeyword),
                SyntaxFactory.Token(SyntaxKind.OpenParenToken),
                newCondition,
                SyntaxFactory.Token(SyntaxKind.CloseParenToken).WithTrailingTrivia(SyntaxFactory.Space),
                statementBody,
                null)
            .WithLeadingTrivia(declaration.GetLeadingTrivia())
            .WithTrailingTrivia(ifStatement.GetTrailingTrivia());

        // Use DocumentEditor to handle multiple changes correctly
        // (RemoveNode followed by ReplaceNode doesn't work because nodes become stale)
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken);
        editor.RemoveNode(declaration);
        editor.ReplaceNode(ifStatement, newIfStatement);

        return editor.GetChangedDocument();
    }

    /// <summary>
    /// Gets the next sibling statement after the current local declaration.
    /// </summary>
    private static StatementSyntax? TryGetNextStatement(LocalDeclarationStatementSyntax currentNode)
    {
        // Navigate up to find the containing block
        var containingBlock = currentNode.Parent as BlockSyntax;
        if (containingBlock is null)
            return null;

        // Find the index of the current statement
        var currentIndex = containingBlock.Statements.IndexOf(currentNode);
        if (currentIndex < 0 || currentIndex >= containingBlock.Statements.Count - 1)
            return null;

        // Return the next statement
        return containingBlock.Statements[currentIndex + 1];
    }
}
