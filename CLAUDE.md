 # CLAUDE.md - ANcpLua.Analyzers

124 Roslyn diagnostic analyzers (AL0001-AL0124) with code fixes, targeting netstandard2.0.

## Commands

```bash
dotnet build ANcpLua.Analyzers.slnx -c Release
dotnet test --project tests/ANcpLua.Analyzers.Tests/ANcpLua.Analyzers.Tests.csproj
dotnet test --project tests/ANcpLua.Analyzers.Tests/ANcpLua.Analyzers.Tests.csproj --filter-method "*AL0001*"
dotnet pack src/ANcpLua.Analyzers/ANcpLua.Analyzers.csproj -c Release -o artifacts -p:PackageId=ANcpLua.Analyzers
```

## Project Structure

```
src/ANcpLua.Analyzers/
  AlAnalyzer.cs                    # Base class (CreateRule, HelpLink, RegisterActions)
  Resources.resx                   # Localized strings ({id}AnalyzerTitle/MessageFormat/Description)
  AnalyzerReleases.Unshipped.md    # Release tracking
  Analyzers/AL0XXX*.cs             # One file per analyzer (or grouped range)
src/ANcpLua.Analyzers.CodeFixes/
  CodeFixes/AL0XXX*.cs             # One file per code fix
  Refactorings/AR0XXX*.cs          # Code refactorings
tests/ANcpLua.Analyzers.Tests/     # Tests both analyzers and code fixes (xunit.v3.mtp-v2)
```

## Analyzer Template

```csharp
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al00xxDescriptiveNameAnalyzer : AlAnalyzer {
    public const string DiagnosticId = "AL00XX";

    private static readonly DiagnosticDescriptor Rule = CreateRule(
        DiagnosticId, DiagnosticCategories.Category, DiagnosticSeverity.Warning);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.Whatever);
}
```

- Each analyzer owns its own `DiagnosticId` as `public const string` — NO shared DiagnosticIds class
- Use `CreateRule()` for single-rule analyzers, manual `new DiagnosticDescriptor(...)` for grouped
- `HelpLink(id)` appends the ID to the Mintlify docs base URL

## TypeCache Pattern (preferred for 2+ type resolutions)

```csharp
private enum KnownType { Task, TaskOfT, ValueTask, ValueTaskOfT }
private static readonly string[] KnownTypeNames = [
    "System.Threading.Tasks.Task", "System.Threading.Tasks.Task`1",
    "System.Threading.Tasks.ValueTask", "System.Threading.Tasks.ValueTask`1" ];

var cache = new TypeCache<KnownType>(
    type => context.Compilation.GetTypeByMetadataName(KnownTypeNames[(int)type]));
```

Adopted in: AL0004, AL0020, AL0026, AL0030, AL0075, AL0105, AL0106.

## Test Pattern

```csharp
public sealed partial class Al00xxTests : AnalyzerTest<Al00xxAnalyzer> {
    [Theory]
    [InlineData("int i", "[|i|] = 10")]
    public Task ShouldReport(string param, string stmt) =>
        VerifyAsync($"public class C({param}) {{ void M() {{ {stmt}; }} }}");
}
```

- `[|span|]` marks expected diagnostic location
- No marker = negative test (expects zero diagnostics)
- Use `--filter-method` (MTP syntax), NOT `--filter "FQN~..."` (VSTest)

## Ecosystem

```
LAYER 0: ANcpLua.Roslyn.Utilities  <- Roslyn helpers (TypeCache, SymbolMatch, extensions)
LAYER 1: ANcpLua.NET.Sdk           <- MSBuild SDK (Version.props source of truth)
LAYER 2: ANcpLua.Analyzers         <- YOU ARE HERE
LAYER 3: qyl, TourPlanner, etc     <- END USERS (auto-injected by SDK)
```

**Breaking change protocol:** Changing diagnostic severity or removing a diagnostic breaks all consumers.

## Key Extensions from Roslyn.Utilities

| Extension | Replaces |
|-----------|----------|
| `symbol.IsEqualTo(other)` | `SymbolEqualityComparer.Default.Equals` |
| `symbol.HasAttribute(type)` | Manual `GetAttributes()` foreach loop |
| `symbol.HasAttributeByShortName("Obsolete")` | Manual name comparison |
| `type.Implements(iface)` | Walking `AllInterfaces` manually |
| `type.InheritsFrom(base)` | Walking `BaseType` chain manually |
| `operation.UnwrapAllConversions()` | Manual conversion unwrapping loop |
| `context.ReportDiagnostic(Rule, location, args)` | `Diagnostic.Create` boilerplate |

## Performance Rules

- `foreach` over Roslyn collections (ImmutableArray) — do NOT convert to LINQ (struct enumerator boxing)
- Use `RegisterCompilationStartAction` + `RegisterOperationAction` — not syntax when operations suffice
- Pre-index field values via `RegisterSyntaxNodeAction` — never call `compilation.GetSemanticModel(otherTree)` in hot paths
- Use `IsEqualTo` for type comparison — never `ToDisplayString()` for type identity

## Banned Patterns

| Pattern | Use Instead |
|---------|-------------|
| `FluentAssertions` | `AwesomeAssertions` |
| `Microsoft.NET.Test.Sdk` | `xunit.v3.mtp-v2` |
| `--filter "FQN~..."` | `--filter-method` (MTP) |
| `LangVersion` / `Nullable` in csproj | SDK-owned |
| `DiagnosticIds.XXX` (shared class) | `public const string DiagnosticId` per analyzer |
| `compilation.GetSemanticModel(otherTree)` in hot path | Pre-index via RegisterSyntaxNodeAction |
| `type.ToDisplayString()` for identity | `type.IsEqualTo(cachedSymbol)` |

## Version Management

- `Version.props` is a symlink from ANcpLua.NET.Sdk — do not edit directly
- `ANcpLuaAnalyzersVersion` in Version.props must be the last PUBLISHED version on NuGet
- CI uses `-p:Version=X.Y.Z` at build/pack time for new versions
- Tag format: `v1.21.0` — triggers publish workflow

## Dependencies (from Version.props)

| Package | Variable | Purpose |
|---------|----------|---------|
| Microsoft.CodeAnalysis.CSharp | `$(RoslynVersion)` 5.3.0 | Roslyn APIs |
| ANcpLua.Roslyn.Utilities.Sources | `$(ANcpLuaRoslynUtilitiesSourcesVersion)` 1.48.0 | Compile-time source package |
| ANcpLua.Roslyn.Utilities.Testing | `$(ANcpLuaRoslynUtilitiesTestingVersion)` 1.48.0 | Test infrastructure |
| xunit.v3.mtp-v2 | `$(XunitV3Version)` 3.2.2 | Test framework |
| AwesomeAssertions | `$(AwesomeAssertionsVersion)` 9.4.0 | Assertions |
