# ANcpLua.Analyzers.CodeFixes - Code Fix Project

This project contains all `CodeFixProvider` and `CodeRefactoringProvider` implementations.

## Project Info

| Property    | Value           |
|-------------|-----------------|
| **Target**  | netstandard2.0  |
| **SDK**     | ANcpLua.NET.Sdk |
| **Version** | 1.9.0           |
| **Roslyn**  | 5.0.0           |

## Structure

```
CodeFixes/
  AL0002CodeFixProvider.cs
  AL0004CodeFixProvider.cs
  AL0005CodeFixProvider.cs
  AL0008IXmlSerializableCodeFixProvider.cs
  AL0010PartialTypeCodeFixProvider.cs
  AL0011LockTypeCodeFixProvider.cs
  AL0012DeprecatedAttributeCodeFixProvider.cs
  AL0014CodeFixProvider.cs
  AL0015NormalizeNullGuardStyleCodeFixProvider.cs
  AL0016CombineDeclarationWithNullCheckCodeFixProvider.cs
  AL0025StaticLambdaCodeFixProvider.cs
  AL0026DateTimeNowCodeFixProvider.cs
  AL0027UseSystemTextJsonCodeFixProvider.cs
  AL0028UseIsEqualToCodeFixProvider.cs
  AL0029UseHasAttributeCodeFixProvider.cs
  AL0030UseTypeHierarchyCodeFixProvider.cs
  AL0031UseOperationExtensionsCodeFixProvider.cs
  AL0039UseStringComparisonExtensionsCodeFixProvider.cs
  ALCodeFixProvider.cs           # Base class
Refactorings/
  AR0001SnakeCaseToPascalCaseRefactoring.cs
  AR0002MakeStaticLambdaRefactoring.cs
CodeFixResources.resx            # Localized strings
```

## Available Code Fixes

| Rule   | Fix Description                               |
|--------|-----------------------------------------------|
| AL0002 | Simplify repeated negated pattern             |
| AL0004 | Convert to pattern matching `is "..."`        |
| AL0005 | Convert to `SequenceEqual`                    |
| AL0008 | Make GetSchema return null                    |
| AL0010 | Add `partial` modifier                        |
| AL0011 | Convert to `Lock` type                        |
| AL0012 | Replace deprecated OTel attribute             |
| AL0014 | Convert to `is null`/`is 0` pattern           |
| AL0015 | Normalize null-guard style                    |
| AL0016 | Combine declaration with null-check           |
| AL0025 | Add `static` to lambda                        |
| AL0026 | Replace DateTime.Now with TimeProvider        |
| AL0027 | Replace Newtonsoft.Json with System.Text.Json |
| AL0028 | Replace with `IsEqualTo`                      |
| AL0029 | Replace with `HasAttribute`                   |
| AL0030 | Replace with `Implements`/`InheritsFrom`      |
| AL0031 | Replace with operation extensions             |
| AL0039 | Replace with StringComparison extensions      |

## Refactorings (AR*)

| Rule   | Description                                      |
|--------|--------------------------------------------------|
| AR0001 | Convert SCREAMING_SNAKE_CASE to PascalCase       |
| AR0002 | Make lambda static (refactoring, not diagnostic) |

## Base Class Pattern

```csharp
[ExportCodeFixProvider(LanguageNames.CSharp)]
[Shared]
public sealed class Al00XXCodeFixProvider : AlCodeFixProvider {
    public override ImmutableArray<string> FixableDiagnosticIds => [DiagnosticIds.MyRule];

    protected override string Title => CodeFixResources.AL00XX_Title;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context) {
        // Implementation
    }
}
```

## Important Notes

- Code fixes must be thread-safe (MEF exports are shared)
- Use `[Shared]` attribute on all providers
- Both Analyzers.dll and CodeFixes.dll are packaged in the nupkg
- Reference `CodeFixResources.resx` for localized strings