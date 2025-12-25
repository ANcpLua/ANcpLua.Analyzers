using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Composition;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ANcpLua.Analyzers.CodeFixes.Refactorings;

/// <summary>
///     AR0001: Refactoring to convert SCREAMING_SNAKE_CASE identifiers to PascalCase.
/// </summary>
[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(AR0001SnakeCaseToPascalCaseRefactoring))]
[Shared]
public sealed class AR0001SnakeCaseToPascalCaseRefactoring : CodeRefactoringProvider
{
    private static readonly Regex ScreamingSnakeCasePattern = new("^[A-Z0-9_]+$", RegexOptions.Compiled);

    public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
    {
        var document = context.Document;
        var root = await document.GetSyntaxRootAsync(context.CancellationToken);
        var node = root?.FindToken(context.Span.Start).Parent;

        switch (node)
        {
            case BaseTypeDeclarationSyntax type when IsScreamingSnakeCase(type.Identifier.Text):
                RegisterRefactoring(context, document, type.Identifier.Text,
                    (doc, name, ct) => ConvertTypeAsync(doc, type, name, ct));
                break;

            case VariableDeclaratorSyntax
                {
                    Parent: VariableDeclarationSyntax { Parent: FieldDeclarationSyntax field }
                } variable
                when field.Modifiers.Any(m => m.IsKind(SyntaxKind.ConstKeyword)) &&
                     IsScreamingSnakeCase(variable.Identifier.Text):
                RegisterRefactoring(context, document, variable.Identifier.Text,
                    (doc, name, ct) => ConvertVariableAsync(doc, variable, name, ct));
                break;

            case EnumMemberDeclarationSyntax enumMember when IsScreamingSnakeCase(enumMember.Identifier.Text):
                RegisterRefactoring(context, document, enumMember.Identifier.Text,
                    (doc, name, ct) => ConvertEnumMemberAsync(doc, enumMember, name, ct));
                break;

            case DelegateDeclarationSyntax @delegate when IsScreamingSnakeCase(@delegate.Identifier.Text):
                RegisterRefactoring(context, document, @delegate.Identifier.Text,
                    (doc, name, ct) => ConvertDelegateAsync(doc, @delegate, name, ct));
                break;
        }
    }

    private static void RegisterRefactoring(
        CodeRefactoringContext context,
        Document document,
        string identifier,
        Func<Document, string, CancellationToken, Task<Document>> converter)
    {
        var pascalCase = ToPascalCase(identifier);
        context.RegisterRefactoring(CodeAction.Create(
            CodeFixResources.AR0001RefactoringTitle,
            ct => converter(document, pascalCase, ct),
            "ConvertToPascalCase"));
    }

    private static async Task<Document> ConvertTypeAsync(
        Document document,
        BaseTypeDeclarationSyntax type,
        string newName,
        CancellationToken ct)
    {
        var root = await document.GetSyntaxRootAsync(ct) ?? throw new InvalidOperationException();
        var newType = type.WithIdentifier(SyntaxFactory.Identifier(newName));
        return document.WithSyntaxRoot(root.ReplaceNode(type, newType));
    }

    private static async Task<Document> ConvertVariableAsync(
        Document document,
        VariableDeclaratorSyntax variable,
        string newName,
        CancellationToken ct)
    {
        var root = await document.GetSyntaxRootAsync(ct) ?? throw new InvalidOperationException();
        var newVariable = variable.WithIdentifier(SyntaxFactory.Identifier(newName));
        return document.WithSyntaxRoot(root.ReplaceNode(variable, newVariable));
    }

    private static async Task<Document> ConvertEnumMemberAsync(
        Document document,
        EnumMemberDeclarationSyntax enumMember,
        string newName,
        CancellationToken ct)
    {
        var root = await document.GetSyntaxRootAsync(ct) ?? throw new InvalidOperationException();
        var newMember = enumMember.WithIdentifier(SyntaxFactory.Identifier(newName));
        return document.WithSyntaxRoot(root.ReplaceNode(enumMember, newMember));
    }

    private static async Task<Document> ConvertDelegateAsync(
        Document document,
        DelegateDeclarationSyntax @delegate,
        string newName,
        CancellationToken ct)
    {
        var root = await document.GetSyntaxRootAsync(ct) ?? throw new InvalidOperationException();
        var newDelegate = @delegate.WithIdentifier(SyntaxFactory.Identifier(newName));
        return document.WithSyntaxRoot(root.ReplaceNode(@delegate, newDelegate));
    }

    private static bool IsScreamingSnakeCase(string identifier)
    {
        return ScreamingSnakeCasePattern.IsMatch(identifier) && identifier.Contains('_');
    }

    private static string ToPascalCase(string screamingSnake)
    {
        return string.Concat(screamingSnake
            .Split(new[] { '_' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(word => char.ToUpperInvariant(word[0]) + word.Substring(1).ToLowerInvariant()));
    }
}
