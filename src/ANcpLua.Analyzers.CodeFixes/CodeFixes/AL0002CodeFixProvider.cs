using ANcpLua.Analyzers.Core;

namespace ANcpLua.Analyzers.CodeFixes.CodeFixes;

/// <summary>
///     Code fix for AL0002: Simplifies repeated negated patterns.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(Al0002CodeFixProvider))]
[Shared]
public sealed partial class Al0002CodeFixProvider : AlCodeFixProvider<UnaryPatternSyntax> {
    public override ImmutableArray<string> FixableDiagnosticIds =>
        [DiagnosticIds.DontRepeatNegatedPattern];

    protected override CodeAction? CreateCodeAction(Document document, UnaryPatternSyntax syntax, SyntaxNode root,
        Diagnostic diagnostic) {
        if (syntax.Parent is not ExpressionOrPatternSyntax parent) {
            return null;
        }

        return CodeAction.Create(
            CodeFixResources.AL0002CodeFixTitle,
            _ => RemoveRepeatedNegatedPatterns(document, syntax, parent, root),
            nameof(CodeFixResources.AL0002CodeFixTitle));
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
