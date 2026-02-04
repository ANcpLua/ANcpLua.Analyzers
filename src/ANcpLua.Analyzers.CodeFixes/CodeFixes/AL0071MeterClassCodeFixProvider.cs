using ANcpLua.Analyzers.Core;

namespace ANcpLua.Analyzers.CodeFixes.CodeFixes;

/// <summary>
///     Code fix provider for AL0071: Adds 'partial static' modifiers to [Meter] class.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(Al0071MeterClassCodeFixProvider))]
[Shared]
public sealed partial class Al0071MeterClassCodeFixProvider : CodeFixProvider {
    /// <summary>Gets the diagnostic IDs this provider can fix.</summary>
    public override ImmutableArray<string> FixableDiagnosticIds => [DiagnosticIds.MeterClassMustBePartialStatic];

    /// <summary>Gets the FixAll provider for batch fixing.</summary>
    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    /// <summary>Registers code fixes for the given context.</summary>
    public override async Task RegisterCodeFixesAsync(CodeFixContext context) {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null) {
            return;
        }

        var diagnostic = context.Diagnostics[0];
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        // Find the class declaration identified by the diagnostic
        var node = root.FindToken(diagnosticSpan.Start).Parent;
        var classDeclaration = node?.AncestorsAndSelf().OfType<ClassDeclarationSyntax>().FirstOrDefault();

        if (classDeclaration is null) {
            return;
        }

        context.RegisterCodeFix(
            CodeAction.Create(
                CodeFixResources.AL0071CodeFixTitle,
                c => MakePartialStaticAsync(context.Document, classDeclaration, root, c),
                nameof(Al0071MeterClassCodeFixProvider)),
            diagnostic);
    }

    private static Task<Document> MakePartialStaticAsync(
        Document document,
        ClassDeclarationSyntax classDeclaration,
        SyntaxNode root,
        CancellationToken _) {
        var modifiers = classDeclaration.Modifiers;

        // Check what modifiers we need to add
        var hasPartial = modifiers.Any(SyntaxKind.PartialKeyword);
        var hasStatic = modifiers.Any(SyntaxKind.StaticKeyword);

        var newModifiers = modifiers;

        // Add static modifier if missing (before partial if partial exists, or at end)
        if (!hasStatic) {
            var staticToken = SyntaxFactory.Token(SyntaxKind.StaticKeyword).WithTrailingTrivia(SyntaxFactory.Space);

            // Find position to insert: after access modifiers, before 'partial' or 'class'
            var insertIndex = GetStaticInsertIndex(modifiers);
            newModifiers = newModifiers.Insert(insertIndex, staticToken);
        }

        // Add partial modifier if missing (should be right before 'class')
        if (!hasPartial) {
            var partialToken = SyntaxFactory.Token(SyntaxKind.PartialKeyword).WithTrailingTrivia(SyntaxFactory.Space);
            newModifiers = newModifiers.Add(partialToken);
        }

        var newClassDeclaration = classDeclaration.WithModifiers(newModifiers);
        var newRoot = root.ReplaceNode(classDeclaration, newClassDeclaration);

        return Task.FromResult(document.WithSyntaxRoot(newRoot));
    }

    private static int GetStaticInsertIndex(SyntaxTokenList modifiers) {
        // Insert static after access modifiers (public, private, protected, internal)
        for (var i = 0; i < modifiers.Count; i++) {
            var kind = modifiers[i].Kind();
            if (kind is not (SyntaxKind.PublicKeyword or SyntaxKind.PrivateKeyword or
                SyntaxKind.ProtectedKeyword or SyntaxKind.InternalKeyword)) {
                return i;
            }
        }

        return modifiers.Count;
    }
}
