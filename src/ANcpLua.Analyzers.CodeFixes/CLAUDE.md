# ANcpLua.Analyzers.CodeFixes - Code Fix Project

CodeFixProvider and CodeRefactoringProvider implementations.

## Target

- **Framework:** netstandard2.0 (required for Roslyn analyzers)
- **Roslyn:** 5.3.0 (pinned via `$(RoslynVersion)` in Version.props)

## File Structure

```
CodeFixes/
  AL00XXCodeFixProvider.cs      # One code fix per analyzer (when applicable)
  ALCodeFixProvider.cs          # Generic base class with node type parameter
Refactorings/
  AR0001*.cs                    # Code refactorings (not tied to diagnostics)
  AR0002*.cs
CodeFixResources.resx           # Localized code fix titles
```

## Code Fix Base Class Pattern

The base class handles boilerplate: finding the node, registering the fix, providing FixAll support.

```csharp
[ExportCodeFixProvider(LanguageNames.CSharp)]
[Shared]  // Required - MEF exports are shared instances
public sealed class Al00XXCodeFixProvider : AlCodeFixProvider<ClassDeclarationSyntax> {
    // Reference the analyzer's public DiagnosticId directly — there is NO shared
    // DiagnosticIds class (see src/ANcpLua.Analyzers/CLAUDE.md §1 for the rule).
    // A CodeFixProvider referencing a DiagnosticId is precisely what flips that
    // constant from private to public on the analyzer side.
    public override ImmutableArray<string> FixableDiagnosticIds =>
        [Al00XXAnalyzer.DiagnosticId];

    protected override CodeAction? CreateCodeAction(
        Document document,
        ClassDeclarationSyntax syntax,  // Node type from generic parameter
        SyntaxNode root,
        Diagnostic diagnostic) {

        return CodeAction.Create(
            title: CodeFixResources.AL00XX_Title,
            createChangedDocument: ct => FixAsync(document, syntax, root, ct),
            equivalenceKey: Al00XXAnalyzer.DiagnosticId);  // Required for FixAll
    }

    private static Task<Document> FixAsync(
        Document document,
        ClassDeclarationSyntax syntax,
        SyntaxNode root,
        CancellationToken ct) {

        var newSyntax = syntax.WithModifiers(/* ... */);
        var newRoot = root.ReplaceNode(syntax, newSyntax);
        return Task.FromResult(document.WithSyntaxRoot(newRoot));
    }
}
```

## Refactorings (AR*)

Code refactorings are NOT tied to diagnostics - they appear in the lightbulb menu on user request.

```csharp
[ExportCodeRefactoringProvider(LanguageNames.CSharp)]
[Shared]
public sealed class Ar0001MyRefactoring : CodeRefactoringProvider {
    public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context) {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken);
        var node = root?.FindNode(context.Span);

        if (node is not IdentifierNameSyntax identifier) return;

        context.RegisterRefactoring(CodeAction.Create(
            title: "Convert to PascalCase",
            createChangedDocument: ct => ConvertAsync(context.Document, identifier, ct),
            equivalenceKey: "AR0001"));
    }
}
```

## Available Code Fixes

The authoritative list lives in the repo-root [`README.md`](../../README.md#code-fixes) ("Code fixes" section). Do not duplicate it here — past attempts drifted within weeks (e.g., listed AL0010/AL0012 fixes that don't exist, missed AL0045–AL0051 / AL0103 / AL0121 / AL0122 / AL0126 / AL0137 / AL0138 / AL0139 that do).

Source of truth when even README drifts: the actual `CodeFixes/AL*.cs` files in this directory.

## Available Refactorings

| ID     | Description                              |
|--------|------------------------------------------|
| AR0001 | Convert SCREAMING_SNAKE_CASE to PascalCase |
| AR0002 | Make lambda static (refactoring variant) |

## Important Notes

- **Thread safety:** Code fix providers are MEF singletons. All state must be local to methods.
- **[Shared] attribute:** Required on all providers.
- **equivalenceKey:** Required for FixAll to work correctly.
- **Both DLLs packaged:** The nupkg includes both Analyzers.dll and CodeFixes.dll.
