using ANcpLua.Analyzers.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;

namespace ANcpLua.Analyzers.CodeFixes.CodeFixes;

/// <summary>
///     Code fix for AL0002: Simplifies repeated negated patterns.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AL0002CodeFixProvider))]
[Shared]
public sealed class AL0002CodeFixProvider : ALCodeFixProvider<UnaryPatternSyntax>
{
    public override ImmutableArray<string> FixableDiagnosticIds =>
        [AL0002DontRepeatNegatedPatternAnalyzer.DiagnosticId];

    protected override CodeAction CreateCodeAction(Document document, UnaryPatternSyntax syntax, SyntaxNode root)
    {
        return CodeAction.Create(
            CodeFixResources.AL0002CodeFixTitle,
            _ => RemoveRepeatedNegatedPatterns(document, syntax, root),
            nameof(CodeFixResources.AL0002CodeFixTitle));
    }

    private static Task<Document> RemoveRepeatedNegatedPatterns(
        Document document,
        UnaryPatternSyntax notPattern,
        SyntaxNode root)
    {
        var parent = (ExpressionOrPatternSyntax)notPattern.Parent!;
        var notPatterns = notPattern.DescendantNodesAndSelf().OfType<UnaryPatternSyntax>().ToArray();

        // Even count of 'not' patterns: Remove all 'not'
        // Odd count of 'not' patterns: Leave only one 'not'
        var lastPattern = notPatterns[notPatterns.Length - 1];
        PatternSyntax realPattern = notPatterns.Length % 2 == 0
            ? lastPattern.Pattern
            : lastPattern;

        var newParent = parent.ReplaceNode(notPattern, realPattern);
        var newRoot = root.ReplaceNode(parent, newParent);

        return Task.FromResult(document.WithSyntaxRoot(newRoot));
    }
}