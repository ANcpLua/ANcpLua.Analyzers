# AGENTS.md - ANcpLua.Analyzers

44 Roslyn diagnostic analyzers (AL0001-AL0044) with automatic code fixes for modern C# patterns.

## Commands

```bash
# Build
dotnet build ANcpLua.Analyzers.slnx -c Release

# Test (MTP v2 - no -- separator)
dotnet test --project tests/ANcpLua.Analyzers.Tests/ANcpLua.Analyzers.Tests.csproj

# Test with filter (xUnit v3 MTP syntax)
dotnet test --project tests/ANcpLua.Analyzers.Tests/ANcpLua.Analyzers.Tests.csproj --filter-method "*AL0001*"

# Pack
dotnet pack src/ANcpLua.Analyzers/ANcpLua.Analyzers.csproj -c Release -o artifacts -p:PackageId=ANcpLua.Analyzers
```

## Project Structure

```
src/
  ANcpLua.Analyzers/              # DiagnosticAnalyzer implementations
    Analyzers/AL00XX*.cs          # One file per diagnostic
    Core/ALAnalyzer.cs            # Base class + DiagnosticIds + DiagnosticCategories
    Core/WellKnownTypes.cs        # Type metadata cache
    Core/OperationHelper.cs       # Argument exception type checks
  ANcpLua.Analyzers.CodeFixes/    # CodeFixProvider implementations
    CodeFixes/AL00XX*.cs          # One file per code fix
    CodeFixes/ALCodeFixProvider.cs # Base class
    Refactorings/AR00XX*.cs       # Code refactorings
tests/
  ANcpLua.Analyzers.Tests/        # Unit tests (xunit.v3.mtp-v2)
```

## Ecosystem Position

```
LAYER 0: ANcpLua.Roslyn.Utilities  <- SOURCE OF TRUTH for Roslyn helpers
LAYER 1: ANcpLua.NET.Sdk           <- SOURCE OF TRUTH for Version.props
LAYER 2: ANcpLua.Analyzers         <- YOU ARE HERE (receives Version.props sync)
LAYER 3: qyl, other projects       <- END USERS
```

## Diagnostic Categories

| Category          | Rules                | Description                        |
|-------------------|----------------------|------------------------------------|
| Design            | AL0001, AL0002, AL0006 | Primary constructors, patterns   |
| Reliability       | AL0003               | Division by zero                   |
| Usage             | AL0004, AL0005       | Span comparison                    |
| IXmlSerializable  | AL0007-AL0009        | GetSchema implementation           |
| Source Generators | AL0010               | Partial type requirement           |
| Threading         | AL0011               | Lock type (.NET 9+)                |
| OpenTelemetry     | AL0012, AL0013       | Semantic conventions               |
| Style             | AL0014-AL0016        | Pattern matching, null guards      |
| VersionManagement | AL0017-AL0019        | CPM/Version.props                  |
| ASP.NET Core      | AL0020-AL0024        | Form binding                       |
| Performance       | AL0025               | Static lambdas                     |
| Banned APIs       | AL0026, AL0027       | Legacy time/JSON APIs              |
| Roslyn Utilities  | AL0028-AL0040        | ANcpLua.Roslyn.Utilities migration |
| AOT Testing       | AL0041-AL0044        | AotTest/TrimTest attributes        |

## Severity Guidelines

| Severity | MSBuild Behavior | Use When                           |
|----------|------------------|------------------------------------|
| Error    | Fails build      | Definite bug, security issue       |
| Warning  | Shows in output  | Anti-pattern, likely bug           |
| Info     | IDE only         | Style suggestion (hidden by default) |

**Info severity (AL0010, AL0013, AL0015, AL0016, AL0028-AL0035) diagnostics do NOT appear in `dotnet build` output.**

## Key Patterns

### Analyzer Base Class

```csharp
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al00XXMyAnalyzer : AlAnalyzer {
    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.MyRule,
        Title, MessageFormat, DiagnosticCategories.Category,
        DiagnosticSeverity.Warning, isEnabledByDefault: true, Description,
        HelpLinkBase);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.Whatever);
}
```

### Code Fix Base Class

```csharp
[ExportCodeFixProvider(LanguageNames.CSharp)]
[Shared]
public sealed class Al00XXCodeFixProvider : AlCodeFixProvider<SyntaxNodeType> {
    public override ImmutableArray<string> FixableDiagnosticIds => [DiagnosticIds.MyRule];

    protected override CodeAction? CreateCodeAction(Document doc, SyntaxNodeType syntax,
        SyntaxNode root, Diagnostic diagnostic) {
        // Return CodeAction.Create(...) or null
    }
}
```

### Test Pattern

```csharp
public sealed class Al0001Tests : AnalyzerTest<Al0001Analyzer> {
    [Theory]
    [InlineData("int i", "[|i|] = 10")]
    [InlineData("int i", "[|i|]++")]
    public Task ShouldReport(string param, string stmt) =>
        VerifyAsync($"public class C({param}) {{ void M() {{ {stmt}; }} }}");
}
```

## Source of Truth Files

| File                       | Owns                                    |
|----------------------------|-----------------------------------------|
| `Core/ALAnalyzer.cs`       | DiagnosticIds, DiagnosticCategories     |
| `Core/WellKnownTypes.cs`   | Type metadata enum and cache            |
| `Core/OperationHelper.cs`  | Argument exception type checks          |
| `Resources.resx`           | Localized diagnostic strings            |

## Banned Patterns

| Pattern                     | Use Instead                        |
|-----------------------------|------------------------------------|
| `FluentAssertions`          | `AwesomeAssertions`                |
| `Microsoft.NET.Test.Sdk`    | `xunit.v3.mtp-v2`                  |
| `--filter "FQN~..."`        | `--filter-method` (MTP syntax)     |
| `LangVersion` in csproj     | SDK-owned property                 |
| `Nullable` in csproj        | SDK-owned property                 |

## Package Structure

| Component                | Target           | NuGet Location                        |
|--------------------------|------------------|---------------------------------------|
| ANcpLua.Analyzers.dll    | netstandard2.0   | `analyzers/dotnet/cs/`                |
| ANcpLua.Analyzers.CodeFixes.dll | netstandard2.0 | `analyzers/dotnet/cs/`          |

Both DLLs are required in the nupkg for IDE code fix integration.

## Dependencies

| Package                              | Version | Purpose                      |
|--------------------------------------|---------|------------------------------|
| Microsoft.CodeAnalysis.CSharp        | 5.0.0   | Roslyn APIs                  |
| ANcpLua.Roslyn.Utilities.Sources     | 1.21.0  | Compile-time source package  |
| ANcpLua.Roslyn.Utilities.Testing     | 1.21.0  | Test infrastructure          |
| xunit.v3.mtp-v2                      | 3.2.2   | Test framework               |
| AwesomeAssertions                    | 9.3.0   | Fluent assertions            |

## SDK Integration Note

The SDK auto-injects this analyzer package. To prevent build cycle during development:
- Use `PackageId=Dummy` in local csproj
- CI workflow passes `-p:PackageId=ANcpLua.Analyzers`

## Related Projects

- [ANcpLua.NET.Sdk](https://github.com/ANcpLua/ANcpLua.NET.Sdk) - MSBuild SDK (auto-injects this analyzer)
- [ANcpLua.Roslyn.Utilities](https://github.com/ANcpLua/ANcpLua.Roslyn.Utilities) - Shared Roslyn helpers
- [Documentation](https://ancplua.mintlify.app/analyzers/overview)
