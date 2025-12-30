using ANcpLua.Analyzers.Core;
using Microsoft.CodeAnalysis.Editing;

namespace ANcpLua.Analyzers.CodeFixes.CodeFixes;

/// <summary>
///     Code fix for AL0016: Combines declaration with null-check into "if (M() is not { } x) return;".
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AL0016CombineDeclarationWithNullCheckCodeFixProvider))]
[Shared]
public sealed class AL0016CombineDeclarationWithNullCheckCodeFixProvider
    : ALCodeFixProvider<LocalDeclarationStatementSyntax> {
    public override ImmutableArray<string> FixableDiagnosticIds => [DiagnosticIds.CombineDeclarationWithNullCheck];

    protected override CodeAction CreateCodeAction(
        Document document,
        LocalDeclarationStatementSyntax declaration,
        SyntaxNode root,
        Diagnostic diagnostic) =>
        CodeAction.Create(
            CodeFixResources.AL0016CodeFixTitle,
            ct => CombineAsync(document, declaration, ct),
            nameof(AL0016CombineDeclarationWithNullCheckCodeFixProvider));

    private static async Task<Document> CombineAsync(
        Document document,
        LocalDeclarationStatementSyntax declaration,
        CancellationToken ct) {
        var editor = await DocumentEditor.CreateAsync(document, ct);

        var block = (BlockSyntax)declaration.Parent!;
        var index = block.Statements.IndexOf(declaration);
        var ifStatement = (IfStatementSyntax)block.Statements[index + 1];

        var variable = declaration.Declaration.Variables[0];
        var variableName = variable.Identifier.Text;
        var initializer = variable.Initializer!.Value;

        
        var patternText = $"{initializer.WithoutTrivia().NormalizeWhitespace()} is not {{ }} {variableName}";
        var condition = SyntaxFactory.ParseExpression(patternText);

        
        if (initializer is AssignmentExpressionSyntax or ConditionalExpressionSyntax or LambdaExpressionSyntax) {
            patternText = $"({initializer.WithoutTrivia().NormalizeWhitespace()}) is not {{ }} {variableName}";
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
