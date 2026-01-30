# ANcpLua.Analyzers.CodeFixes - Code Fix Project

CodeFixProvider and CodeRefactoringProvider implementations.

## Target

- **Framework:** netstandard2.0 (required for Roslyn analyzers)
- **Roslyn:** 5.0.0

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
    public override ImmutableArray<string> FixableDiagnosticIds =>
        [DiagnosticIds.MyRule];

    protected override CodeAction? CreateCodeAction(
        Document document,
        ClassDeclarationSyntax syntax,  // Node type from generic parameter
        SyntaxNode root,
        Diagnostic diagnostic) {

        return CodeAction.Create(
            title: CodeFixResources.AL00XX_Title,
            createChangedDocument: ct => FixAsync(document, syntax, root, ct),
            equivalenceKey: DiagnosticIds.MyRule);  // Required for FixAll
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

| Diagnostic | Fix Description                           |
|------------|-------------------------------------------|
| AL0002     | Simplify repeated negated pattern         |
| AL0004     | Convert to `is "constant"` pattern        |
| AL0005     | Convert to `SequenceEqual`                |
| AL0008     | Make GetSchema return null                |
| AL0010     | Add `partial` modifier                    |
| AL0011     | Convert to `Lock` type                    |
| AL0012     | Replace deprecated OTel attribute         |
| AL0014     | Convert to `is null`/`is 0` pattern       |
| AL0015     | Normalize null-guard style                |
| AL0016     | Combine declaration with null-check       |
| AL0025     | Add `static` to lambda                    |
| AL0026     | Replace with TimeProvider                 |
| AL0027     | Replace Newtonsoft with System.Text.Json  |
| AL0028-40  | Replace with Roslyn.Utilities extensions  |

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
