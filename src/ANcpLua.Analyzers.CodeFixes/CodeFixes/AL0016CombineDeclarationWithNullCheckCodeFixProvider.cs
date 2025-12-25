using ANcpLua.Analyzers.Core;

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

    private static Task<Document> CombineDeclarationWithNullCheck(
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
            return Task.FromResult(document);

        // Create the pattern: not { } x
        // This is: UnaryPattern(not) with RecursivePattern that has PropertyPatternClause { } and VariableDesignation x
        var propertyPatternClause = SyntaxFactory.PropertyPatternClause(
            SyntaxFactory.Token(SyntaxKind.OpenBraceToken),
            default,
            SyntaxFactory.Token(SyntaxKind.CloseBraceToken));

        var variableDesignation = SyntaxFactory.SingleVariableDesignation(
            SyntaxFactory.Identifier(variableName));

        var recursivePattern = SyntaxFactory.RecursivePattern(
            null,
            null,
            propertyPatternClause,
            variableDesignation);

        var notPattern = SyntaxFactory.UnaryPattern(
            SyntaxFactory.Token(SyntaxKind.NotKeyword).WithTrailingTrivia(SyntaxFactory.Space),
            recursivePattern);

        // Create the is pattern expression
        var newCondition = SyntaxFactory.IsPatternExpression(
            initializer.WithoutTrivia(),
            SyntaxFactory.Token(SyntaxKind.IsKeyword)
                .WithLeadingTrivia(SyntaxFactory.Space)
                .WithTrailingTrivia(SyntaxFactory.Space),
            notPattern);

        // Create the new if statement using the original statement body
        var newIfStatement = SyntaxFactory.IfStatement(
            newCondition,
            ifStatement.Statement)
            .WithLeadingTrivia(declaration.GetLeadingTrivia())
            .WithTrailingTrivia(ifStatement.GetTrailingTrivia());

        // Replace both statements: remove declaration and replace if statement
        var newRoot = root
            .RemoveNode(declaration, SyntaxRemoveOptions.KeepTrailingTrivia)!
            .ReplaceNode(ifStatement, newIfStatement);

        return Task.FromResult(document.WithSyntaxRoot(newRoot));
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
