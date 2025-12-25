using ANcpLua.Analyzers.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;

namespace ANcpLua.Analyzers.CodeFixes.CodeFixes;

/// <summary>
///     Code fix for AL0004: Converts Span equality to pattern matching.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AL0004CodeFixProvider))]
[Shared]
public sealed class AL0004CodeFixProvider : ALCodeFixProvider<BinaryExpressionSyntax>
{
    public override ImmutableArray<string> FixableDiagnosticIds =>
        [AL0004ToAL0005SpanComparisonAnalyzer.DiagnosticIdAL0004];

    protected override CodeAction CreateCodeAction(Document document, BinaryExpressionSyntax syntax, SyntaxNode root)
    {
        return CodeAction.Create(
            CodeFixResources.AL0004CodeFixTitle,
            _ => UsePatternMatching(document, syntax, root),
            nameof(CodeFixResources.AL0004CodeFixTitle));
    }

    private static Task<Document> UsePatternMatching(
        Document document,
        BinaryExpressionSyntax binary,
        SyntaxNode root)
    {
        var isPatternExpression = binary.Right.Kind() switch
        {
            SyntaxKind.StringLiteralExpression => ProcessStringLiteral(binary),
            SyntaxKind.CollectionExpression => ProcessCollection(binary),
            SyntaxKind.ArrayCreationExpression => ProcessArrayCreation(binary),
            SyntaxKind.ImplicitArrayCreationExpression => ProcessImplicitArrayCreation(binary),
            _ => throw new InvalidOperationException("Unexpected syntax kind")
        };

        return Task.FromResult(document.WithSyntaxRoot(root.ReplaceNode(binary, isPatternExpression)));
    }

    private static IsPatternExpressionSyntax ProcessStringLiteral(BinaryExpressionSyntax binary)
    {
        return SyntaxFactory.IsPatternExpression(binary.Left, SyntaxFactory.ConstantPattern(binary.Right));
    }

    private static IsPatternExpressionSyntax ProcessCollection(BinaryExpressionSyntax binary)
    {
        var collection = (CollectionExpressionSyntax)binary.Right;
        var patterns = collection.Elements
            .Cast<ExpressionElementSyntax>()
            .Select(e => (PatternSyntax)SyntaxFactory.ConstantPattern(e.Expression));

        return SyntaxFactory.IsPatternExpression(
            binary.Left,
            SyntaxFactory.ListPattern(SyntaxFactory.SeparatedList(patterns)));
    }

    private static IsPatternExpressionSyntax ProcessArrayCreation(BinaryExpressionSyntax binary)
    {
        var arrayCreation = (ArrayCreationExpressionSyntax)binary.Right;
        var patterns = arrayCreation.Initializer?.Expressions
                           .Select(e => (PatternSyntax)SyntaxFactory.ConstantPattern(e))
                       ?? [];

        return SyntaxFactory.IsPatternExpression(
            binary.Left,
            SyntaxFactory.ListPattern(SyntaxFactory.SeparatedList(patterns)));
    }

    private static IsPatternExpressionSyntax ProcessImplicitArrayCreation(BinaryExpressionSyntax binary)
    {
        var implicitArray = (ImplicitArrayCreationExpressionSyntax)binary.Right;
        var patterns = implicitArray.Initializer.Expressions
            .Select(e => (PatternSyntax)SyntaxFactory.ConstantPattern(e));

        return SyntaxFactory.IsPatternExpression(
            binary.Left,
            SyntaxFactory.ListPattern(SyntaxFactory.SeparatedList(patterns)));
    }
}