using System.Text.RegularExpressions;

// CA1308: ToLowerInvariant is intentional here - we need lowercase output for PascalCase conversion
#pragma warning disable CA1308 // Normalize strings to uppercase

namespace ANcpLua.Analyzers.CodeFixes.Refactorings;

/// <summary>
///     AR0001: Refactoring to convert SCREAMING_SNAKE_CASE identifiers to PascalCase.
/// </summary>
[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(Ar0001SnakeCaseToPascalCaseRefactoring))]
[Shared]
public sealed partial class Ar0001SnakeCaseToPascalCaseRefactoring : CodeRefactoringProvider {
    private static readonly Regex s_screamingSnakeCasePattern = new("^[A-Z0-9_]+$", RegexOptions.Compiled);

    public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context) {
        var document = context.Document;
        var root = await document.GetSyntaxRootAsync(context.CancellationToken);
        var node = root?.FindToken(context.Span.Start).Parent;

        switch (node) {
            case BaseTypeDeclarationSyntax type when IsScreamingSnakeCase(type.Identifier.Text):
                RegisterRefactoring(context, document, type.Identifier.Text,
                    (doc, name, ct) => RenameNodeAsync(doc, type, name, static (n, id) => n.WithIdentifier(id), ct));
                break;

            case VariableDeclaratorSyntax {
                Parent: VariableDeclarationSyntax { Parent: FieldDeclarationSyntax field }
            } variable
                when field.Modifiers.Any(static m => m.IsKind(SyntaxKind.ConstKeyword)) &&
                     IsScreamingSnakeCase(variable.Identifier.Text):
                RegisterRefactoring(context, document, variable.Identifier.Text,
                    (doc, name, ct) =>
                        RenameNodeAsync(doc, variable, name, static (n, id) => n.WithIdentifier(id), ct));
                break;

            case EnumMemberDeclarationSyntax enumMember when IsScreamingSnakeCase(enumMember.Identifier.Text):
                RegisterRefactoring(context, document, enumMember.Identifier.Text,
                    (doc, name, ct) =>
                        RenameNodeAsync(doc, enumMember, name, static (n, id) => n.WithIdentifier(id), ct));
                break;

            case DelegateDeclarationSyntax @delegate when IsScreamingSnakeCase(@delegate.Identifier.Text):
                RegisterRefactoring(context, document, @delegate.Identifier.Text,
                    (doc, name, ct) =>
                        RenameNodeAsync(doc, @delegate, name, static (n, id) => n.WithIdentifier(id), ct));
                break;
        }
    }

    private static void RegisterRefactoring(
        CodeRefactoringContext context,
        Document document,
        string identifier,
        Func<Document, string, CancellationToken, Task<Document>> converter) {
        var pascalCase = ToPascalCase(identifier);
        context.RegisterRefactoring(CodeAction.Create(
            CodeFixResources.AR0001RefactoringTitle,
            ct => converter(document, pascalCase, ct),
            "ConvertToPascalCase"));
    }

    private static async Task<Document> RenameNodeAsync<T>(
        Document document,
        T node,
        string newName,
        Func<T, SyntaxToken, T> withIdentifier,
        CancellationToken ct) where T : SyntaxNode {
        var root = await document.GetSyntaxRootAsync(ct) ?? throw new InvalidOperationException();
        var newNode = withIdentifier(node, SyntaxFactory.Identifier(newName));
        return document.WithSyntaxRoot(root.ReplaceNode(node, newNode));
    }

    private static bool IsScreamingSnakeCase(string identifier) =>
#pragma warning disable AL0039 // CodeFixes project doesn't reference ANcpLua.Roslyn.Utilities
        s_screamingSnakeCasePattern.IsMatch(identifier) && identifier.Contains('_', StringComparison.Ordinal);
#pragma warning restore AL0039

    private static string ToPascalCase(string screamingSnake) =>
        string.Concat(screamingSnake
            .Split(['_'], StringSplitOptions.RemoveEmptyEntries)
            .Select(static word => char.ToUpperInvariant(word[0]) + word[1..].ToLowerInvariant()));
}
