using ANcpLua.Analyzers.Analyzers;

namespace ANcpLua.Analyzers.CodeFixes.CodeFixes;

/// <summary>
///     Code fix for AL1001: Simplifies repeated negated patterns.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(Al1001SimplifyNegatedPatternCodeFixProvider))]
[Shared]
public sealed partial class Al1001SimplifyNegatedPatternCodeFixProvider : AlCodeFixProvider<UnaryPatternSyntax> {
    public override ImmutableArray<string> FixableDiagnosticIds =>
        [Al1001DontRepeatNegatedPatternAnalyzer.DiagnosticId];

    protected override CodeAction? CreateCodeAction(Document document, UnaryPatternSyntax syntax, SyntaxNode root,
        Diagnostic diagnostic) {
        if (syntax.Parent is not ExpressionOrPatternSyntax parent) {
            return null;
        }

        return CodeAction.Create(
            CodeFixResources.AL1001CodeFixTitle,
            _ => RemoveRepeatedNegatedPatterns(document, syntax, parent, root),
            nameof(CodeFixResources.AL1001CodeFixTitle));
    }

    private static Task<Document> RemoveRepeatedNegatedPatterns(
        Document document,
        SyntaxNode notPattern,
        ExpressionOrPatternSyntax parent,
        SyntaxNode root) {
        var notPatterns = notPattern.DescendantNodesAndSelf().OfType<UnaryPatternSyntax>().ToArray();

        var lastPattern = notPatterns[^1];
        var realPattern = notPatterns.Length % 2 is 0
            ? lastPattern.Pattern
            : lastPattern;

        var newParent = parent.ReplaceNode(notPattern, realPattern);
        var newRoot = root.ReplaceNode(parent, newParent);

        return Task.FromResult(document.WithSyntaxRoot(newRoot));
    }
}
