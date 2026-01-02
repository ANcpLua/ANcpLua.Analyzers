using ANcpLua.Analyzers.Analyzers;

namespace ANcpLua.Analyzers.CodeFixes.CodeFixes;

/// <summary>
///     Code fix for AL0002: Simplifies repeated negated patterns.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AL0002CodeFixProvider))]
[Shared]
public sealed class AL0002CodeFixProvider : ALCodeFixProvider<UnaryPatternSyntax> {
    public override ImmutableArray<string> FixableDiagnosticIds =>
        [AL0002DontRepeatNegatedPatternAnalyzer.DiagnosticId];

    protected override CodeAction CreateCodeAction(Document document, UnaryPatternSyntax syntax, SyntaxNode root,
        Diagnostic diagnostic) =>
        CodeAction.Create(
            CodeFixResources.AL0002CodeFixTitle,
            _ => RemoveRepeatedNegatedPatterns(document, syntax, root),
            nameof(CodeFixResources.AL0002CodeFixTitle));

    private static Task<Document> RemoveRepeatedNegatedPatterns(
        Document document,
        SyntaxNode notPattern,
        SyntaxNode root) {
        var parent = (ExpressionOrPatternSyntax)notPattern.Parent!;
        var notPatterns = notPattern.DescendantNodesAndSelf().OfType<UnaryPatternSyntax>().ToArray();


        var lastPattern = notPatterns[notPatterns.Length - 1];
        var realPattern = notPatterns.Length % 2 is 0
            ? lastPattern.Pattern
            : lastPattern;

        var newParent = parent.ReplaceNode(notPattern, realPattern);
        var newRoot = root.ReplaceNode(parent, newParent);

        return Task.FromResult(document.WithSyntaxRoot(newRoot));
    }
}
