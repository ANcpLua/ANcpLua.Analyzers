# ANcpLua.Analyzers - Analyzer Project

This project contains all `DiagnosticAnalyzer` implementations (AL0001-AL0035).

## Project Info

| Property    | Value           |
|-------------|-----------------|
| **Target**  | netstandard2.0  |
| **SDK**     | ANcpLua.NET.Sdk |
| **Version** | 1.9.0           |
| **Roslyn**  | 5.0.0           |

## Structure

```
Analyzers/
  AL0001*.cs - AL0035*.cs    # Individual analyzer implementations
Core/
  ALAnalyzer.cs              # Base class + DiagnosticIds + DiagnosticCategories
  DeprecatedOtelAttributes.cs
  WellKnownTypes.cs
Internal/
  RoslynExtensions.cs        # Internal Roslyn helpers
Resources.resx               # Localized strings
```

## Analyzer Categories

| Category          | Rules                  | Description                         |
|-------------------|------------------------|-------------------------------------|
| Design            | AL0001, AL0002, AL0006 | Code design issues                  |
| Usage             | AL0004, AL0005         | API usage patterns                  |
| Reliability       | AL0003                 | Runtime reliability                 |
| Threading         | AL0011                 | Thread safety                       |
| OpenTelemetry     | AL0012, AL0013         | OTel semantic conventions           |
| Style             | AL0014, AL0015, AL0016 | Code style consistency              |
| VersionManagement | AL0017, AL0018, AL0019 | CPM/Version.props                   |
| ASP.NET Core      | AL0020-AL0024          | Form binding                        |
| Performance       | AL0025                 | Static lambdas                      |
| Banned APIs       | AL0026, AL0027         | DateTime.Now, Newtonsoft            |
| Roslyn Utilities  | AL0028-AL0035          | ANcpLua.Roslyn.Utilities extensions |

## Adding a New Analyzer

1. Create `Analyzers/AL00XXDescriptionAnalyzer.cs`
2. Add diagnostic ID to `Core/ALAnalyzer.cs` in `DiagnosticIds`
3. Add localized strings to `Resources.resx`
4. Inherit from `AlAnalyzer` base class
5. Add corresponding code fix in CodeFixes project (if applicable)
6. Add tests in Tests project

## Base Class Pattern

```csharp
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al00XXMyAnalyzer : AlAnalyzer {
    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.MyRule,
        Title, MessageFormat, DiagnosticCategories.Category,
        DiagnosticSeverity.Warning, true, Description,
        HelpLinkBase);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.Whatever);
}
```

## Severity Guidelines

| Severity | Use When                                         |
|----------|--------------------------------------------------|
| Error    | Bug that will cause runtime failure              |
| Warning  | Likely bug or anti-pattern                       |
| Info     | Style suggestion (IDE only, not in build output) |

## Key Dependencies

- Microsoft.CodeAnalysis.CSharp (5.0.0)
- ANcpLua.Roslyn.Utilities.Sources (1.16.0) - compile-time source package