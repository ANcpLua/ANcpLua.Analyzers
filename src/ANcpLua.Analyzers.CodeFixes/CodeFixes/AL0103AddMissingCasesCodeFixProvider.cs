using ANcpLua.Analyzers.Analyzers;

namespace ANcpLua.Analyzers.CodeFixes.CodeFixes;

/// <summary>
///     Code fix for AL0103 (ClosedTypeHierarchySwitchAnalyzer).
///     Inserts missing sealed-subtype arms into <c>switch</c> expressions
///     and case sections into <c>switch</c> statements, before any
///     discard (<c>_</c>) or <c>default</c> fallback.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(Al0103AddMissingCasesCodeFixProvider))]
[Shared]
public sealed partial class Al0103AddMissingCasesCodeFixProvider : AlCodeFixProvider<CSharpSyntaxNode> {
    /// <inheritdoc />
    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
        [Al0103ClosedTypeHierarchySwitchAnalyzer.DiagnosticId];

    /// <inheritdoc />
    protected override CodeAction? CreateCodeAction(
        Document document, CSharpSyntaxNode syntax, SyntaxNode root, Diagnostic diagnostic) {
        if (!diagnostic.Properties.TryGetValue(
                Al0103ClosedTypeHierarchySwitchAnalyzer.MissingTypesProperty, out var raw) ||
            string.IsNullOrEmpty(raw)) {
            return null;
        }

        return syntax switch {
            SwitchExpressionSyntax switchExpr => CodeAction.Create(
                CodeFixResources.AL0103CodeFixTitle,
                _ => FixSwitchExpression(document, root, switchExpr, raw!),
                nameof(CodeFixResources.AL0103CodeFixTitle)),

            SwitchStatementSyntax switchStmt => CodeAction.Create(
                CodeFixResources.AL0103CodeFixTitle,
                _ => FixSwitchStatement(document, root, switchStmt, raw!),
                nameof(CodeFixResources.AL0103CodeFixTitle)),

            _ => null
        };
    }

    static Task<Document> FixSwitchExpression(
        Document document, SyntaxNode root,
        SwitchExpressionSyntax switchExpr, string missingTypes) {
        var typeNames = missingTypes.Split('|');
        var arms = switchExpr.Arms.ToList();

        // Insert before discard arm if present, otherwise append
        var insertIndex = arms.FindIndex(static a => a.Pattern is DiscardPatternSyntax);
        if (insertIndex < 0) insertIndex = arms.Count;

        foreach (var fqn in typeNames) {
            arms.Insert(insertIndex++, SyntaxFactory.SwitchExpressionArm(
                SyntaxFactory.DeclarationPattern(
                    SyntaxFactory.ParseTypeName(fqn),
                    SyntaxFactory.DiscardDesignation()),
                SyntaxFactory.ThrowExpression(
                    SyntaxFactory.ObjectCreationExpression(
                            SyntaxFactory.ParseTypeName("System.NotImplementedException"))
                        .WithArgumentList(SyntaxFactory.ArgumentList()))));
        }

        var newRoot = root.ReplaceNode(switchExpr, switchExpr.WithArms(SyntaxFactory.SeparatedList(arms)));
        return Task.FromResult(document.WithSyntaxRoot(newRoot));
    }

    static Task<Document> FixSwitchStatement(
        Document document, SyntaxNode root,
        SwitchStatementSyntax switchStmt, string missingTypes) {
        var typeNames = missingTypes.Split('|');
        var sections = switchStmt.Sections.ToList();

        // Insert before default section if present, otherwise append
        var insertIndex = sections.FindIndex(static s => s.Labels.Any(static l => l is DefaultSwitchLabelSyntax));
        if (insertIndex < 0) insertIndex = sections.Count;

        foreach (var fqn in typeNames) {
            sections.Insert(insertIndex++, SyntaxFactory.SwitchSection(
                SyntaxFactory.SingletonList<SwitchLabelSyntax>(
                    SyntaxFactory.CasePatternSwitchLabel(
                        SyntaxFactory.DeclarationPattern(
                            SyntaxFactory.ParseTypeName(fqn),
                            SyntaxFactory.DiscardDesignation()),
                        SyntaxFactory.Token(SyntaxKind.ColonToken))),
                SyntaxFactory.SingletonList<StatementSyntax>(
                    SyntaxFactory.ThrowStatement(
                        SyntaxFactory.ObjectCreationExpression(
                                SyntaxFactory.ParseTypeName("System.NotImplementedException"))
                            .WithArgumentList(SyntaxFactory.ArgumentList())))));
        }

        var newRoot = root.ReplaceNode(switchStmt, switchStmt.WithSections(SyntaxFactory.List(sections)));
        return Task.FromResult(document.WithSyntaxRoot(newRoot));
    }
}
