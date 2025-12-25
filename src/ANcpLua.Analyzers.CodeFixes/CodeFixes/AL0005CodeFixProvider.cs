using ANcpLua.Analyzers.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Composition;
using System.Threading.Tasks;

namespace ANcpLua.Analyzers.CodeFixes.CodeFixes;

/// <summary>
///     Code fix for AL0005: Converts Span equality to SequenceEqual.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AL0005CodeFixProvider))]
[Shared]
public sealed class AL0005CodeFixProvider : ALCodeFixProvider<BinaryExpressionSyntax>
{
    public override ImmutableArray<string> FixableDiagnosticIds =>
        [AL0004ToAL0005SpanComparisonAnalyzer.DiagnosticIdAL0005];

    protected override CodeAction CreateCodeAction(Document document, BinaryExpressionSyntax syntax, SyntaxNode root)
    {
        return CodeAction.Create(
            CodeFixResources.AL0005CodeFixTitle,
            _ => UseSequenceEqual(document, syntax, root),
            nameof(CodeFixResources.AL0005CodeFixTitle));
    }

    private static Task<Document> UseSequenceEqual(
        Document document,
        BinaryExpressionSyntax binary,
        SyntaxNode root)
    {
        var sequenceEqual = SyntaxFactory.IdentifierName("SequenceEqual");
        var memberAccess = SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            binary.Left,
            sequenceEqual);
        var argument = SyntaxFactory.Argument(binary.Right);
        var argumentList = SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(argument));
        var invocation = SyntaxFactory.InvocationExpression(memberAccess, argumentList);

        return Task.FromResult(document.WithSyntaxRoot(root.ReplaceNode(binary, invocation)));
    }
}