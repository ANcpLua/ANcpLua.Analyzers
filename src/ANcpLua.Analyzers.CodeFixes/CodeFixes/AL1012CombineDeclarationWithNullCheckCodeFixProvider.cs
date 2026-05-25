using ANcpLua.Analyzers.Analyzers;
using Microsoft.CodeAnalysis.Editing;

namespace ANcpLua.Analyzers.CodeFixes.CodeFixes;

/// <summary>
///     Code fix for AL1012: Combines declaration with null-check into "if (M() is not { } x) return;".
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(Al1012CombineDeclarationWithNullCheckCodeFixProvider))]
[Shared]
public sealed partial class Al1012CombineDeclarationWithNullCheckCodeFixProvider
    : AlCodeFixProvider<LocalDeclarationStatementSyntax> {
    public override ImmutableArray<string> FixableDiagnosticIds => [Al1012CombineDeclarationWithNullCheckAnalyzer.DiagnosticId];

    protected override CodeAction? CreateCodeAction(Document document,
        LocalDeclarationStatementSyntax declaration,
        SyntaxNode root,
        Diagnostic diagnostic) {
        if (declaration.Parent is not BlockSyntax block) {
            return null;
        }

        var variable = declaration.Declaration.Variables[0];
        if (variable.Initializer is not { Value: var initializerValue }) {
            return null;
        }

        var index = block.Statements.IndexOf(declaration);
        if (index + 1 >= block.Statements.Count || block.Statements[index + 1] is not IfStatementSyntax ifStatement) {
            return null;
        }

        var variableName = variable.Identifier.Text;

        return CodeAction.Create(
            CodeFixResources.AL1012CodeFixTitle,
            ct => CombineAsync(document, declaration, ifStatement, variableName, initializerValue, ct),
            Al1012CombineDeclarationWithNullCheckAnalyzer.DiagnosticId);
    }

    private static async Task<Document> CombineAsync(
        Document document,
        CSharpSyntaxNode declaration,
        IfStatementSyntax ifStatement,
        string variableName,
        ExpressionSyntax initializer,
        CancellationToken ct) {
        var editor = await DocumentEditor.CreateAsync(document, ct);

        var patternText = $"{initializer.WithoutTrivia().ToFullString().Trim()} is not {{ }} {variableName}";
        var condition = SyntaxFactory.ParseExpression(patternText);

        if (initializer is AssignmentExpressionSyntax or ConditionalExpressionSyntax or LambdaExpressionSyntax) {
            patternText = $"({initializer.WithoutTrivia().ToFullString().Trim()}) is not {{ }} {variableName}";
            condition = SyntaxFactory.ParseExpression(patternText);
        }

        var newIf = ifStatement
            .WithCondition(condition)
            .WithLeadingTrivia(declaration.GetLeadingTrivia());

        editor.RemoveNode(declaration);
        editor.ReplaceNode(ifStatement, newIf);

        return editor.GetChangedDocument();
    }
}
