# AGENTS.md - ANcpLua.Analyzers

90 diagnostics (AL0001–AL0140 with gaps; full list in `AnalyzerReleases.Unshipped.md`) with 38 automatic code fixes, targeting netstandard2.0. The authoritative rule catalog lives in [`README.md`](README.md#full-rule-catalog) — when these counts drift again, trust `AnalyzerReleases.Unshipped.md` and `src/ANcpLua.Analyzers.CodeFixes/CodeFixes/AL*.cs` over any prose in this file.

## Framework conventions

Branch protection, auto-merge, CodeRabbit posture, release flow, dependency
graph, and the cross-repo bootstrap rules for the four ANcpLua framework
repos are documented in one place at
[ANcpLua/renovate-config](https://github.com/ANcpLua/renovate-config#ancplua-framework-conventions--renovate-config).
This file documents conventions specific to this repo only.


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
    private const string DiagnosticId = "AL00XX";

    private static readonly DiagnosticDescriptor Rule = CreateRule(
        DiagnosticId, DiagnosticCategories.Category, DiagnosticSeverity.Warning);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.Whatever);
}
```

- Each analyzer owns its own `DiagnosticId` as `const string` — NO shared DiagnosticIds class
- **Visibility rule:** `public` only if a `CodeFixProvider` references it, otherwise `private` (matches the official Roslyn SDK template)
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

Adopted in: AL0020, AL0024, AL0026, AL0030, AL0105, AL0106. (Re-grep `new TypeCache<` under `src/ANcpLua.Analyzers/Analyzers/` if this list looks suspect — analyzers come and go faster than this prose.)

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

## Severity Guidelines

| Severity | MSBuild Behavior | Use When                           |
|----------|------------------|------------------------------------|
| Error    | Fails build      | Definite bug, security issue       |
| Warning  | Shows in output  | Anti-pattern, likely bug           |
| Info     | IDE only         | Style suggestion (hidden by default) |

**Info severity diagnostics do NOT appear in `dotnet build` output** — IDE-only, won't block CI.

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
| `DiagnosticIds.XXX` (shared class) | `const string DiagnosticId` per analyzer (private unless a fix references it) |
| `compilation.GetSemanticModel(otherTree)` in hot path | Pre-index via RegisterSyntaxNodeAction |
| `type.ToDisplayString()` for identity | `type.IsEqualTo(cachedSymbol)` |

## Package Structure

| Component                        | Target           | NuGet Location                |
|----------------------------------|------------------|-------------------------------|
| ANcpLua.Analyzers.dll            | netstandard2.0   | `analyzers/dotnet/cs/`        |
| ANcpLua.Analyzers.CodeFixes.dll  | netstandard2.0   | `analyzers/dotnet/cs/`        |

Both DLLs are required in the nupkg for IDE code fix integration.

## Version Management

Two `Version.props` files participate, layered via "last wins":

1. **SDK-shipped baseline** — packed inside `ANcpLua.NET.Sdk`, resolved automatically for any project declaring `<Project Sdk="ANcpLua.NET.Sdk">`. Lives at `~/.nuget/packages/ancplua.net.sdk/<ver>/Build/Common/Version.props`.
2. **Local override** — `./Version.props` at this repo root, imported explicitly by `Directory.Packages.props` AFTER the SDK copy. Used to pin versions AHEAD of the currently-published SDK.

Not a symlink — git cannot symlink cleanly across repos. Prune entries from the local file once the SDK publishes with matching versions; drift means stale local overrides, not a broken link.

- `ANcpSdkPackageVersion` is `999.9.9` in the local file (dogfooding sentinel); CI stamps the real version at pack time
- CI uses `-p:Version=X.Y.Z` at build/pack time for new versions
- Tag format: `v1.21.0` — triggers publish workflow

## Cross-Repo Awareness — was passiert, wenn du Versionen anfasst

Diese vier Repos bilden eine Bootstrap-Kette: `Roslyn.Utilities → NET.Sdk → (Analyzers, Agents)`. Truth-Source für Paket-Versionen ist **`ANcpLua.NET.Sdk/src/Build/Common/Version.props`**, in den SDK-NuGet-Packages gepackt und in jedes Consumer-Projekt geladen. Dein lokales `Version.props` (sofern vorhanden) wird *nach* der SDK-Datei importiert (last-wins) — gedacht, um lokal AHEAD der gerade-publizierten SDK zu pinnen.

Bevor du eine Variable in Truth oder im lokalen Override bumpst:

- **Truth fließt durch GlobalPackageReference.** Pakete wie `ANcpLua.Analyzers` werden von der SDK in *jedes* Consumer-Projekt injiziert. Wenn Truth auf eine Version zeigt, die noch nicht auf nuget.org liegt, scheitert jeder Restore mit `NU1102` — auch die SDK-eigenen Tests (sie packen ein Sample.csproj und builden es). Saubere Reihenfolge: zuerst das ausgeschriebene Repo taggen + auf NuGet bringen, dann Truth nachziehen.

- **Self-Reference: die eigene Paket-Version zeigt auf last-PUBLISHED.** Wenn ein lokales `Version.props` eine Variable für das *eigene* Paket des Repos hat (z.B. `ANcpLuaAnalyzersVersion` in `ANcpLua.Analyzers/Version.props`), muss sie auf die zuletzt-publizierte Version zeigen, nicht auf die hochzukommende. csproj/Tests-Files referenzieren das Paket via `PackageReference` und ziehen es beim Restore aus NuGet; während Restore (vor Pack) gibt's die hochzukommende Version noch nicht. CI stampt die neue Version per `-p:Version=X.Y.Z` erst zur Pack-Time.

- **Bumps haben transitive Konsequenzen unter CPM.** Z.B. `Meziantou.Framework.DependencyScanning 2.0.11` zieht `YamlDotNet ≥ 17.0.1`. Bei `ManagePackageVersionsCentrally=true` ist Downgrade ein Hard-Error (`NU1109`). Wenn ein Bump nicht greift, steht der Grund in der Restore-Fehlermeldung — vor dem nächsten Versuch lesen.

- **Lokales Override gleich/unter Truth ist Müll.** Gleich = Doppelpflege, unter = stille Regression. Pruning sinnvoll, sobald die SDK mit matching Werten publisht.

- **Publish triggert auf Tag-Push `v*`, gegated durch Tests.** Ein Tag auf einen build-broken Commit publisht nicht, bleibt aber als Ghost-Tag remote. Statt remote zu re-assignen (≈ Force-Push), nächste Patch-Version verwenden.

- **Verifiziere Versionen vor dem Bump.** Ein Tippfehler (`2.0.20` statt `2.0.11`) bricht die Topo-Kette, weil Truth in alle Konsumenten fließt. NuGet-API: `https://api.nuget.org/v3-flatcontainer/<lowercased-id>/index.json`.

## Dependencies (from Version.props)

| Package | Variable | Purpose |
|---------|----------|---------|
| Microsoft.CodeAnalysis.CSharp | `$(RoslynVersion)` | Roslyn APIs |
| ANcpLua.Roslyn.Utilities | `$(ANcpLuaRoslynUtilitiesVersion)` | Binary package |
| ANcpLua.Roslyn.Utilities.Sources | `$(ANcpLuaRoslynUtilitiesSourcesVersion)` | Compile-time source package |
| ANcpLua.Roslyn.Utilities.Polyfills | `$(ANcpLuaRoslynUtilitiesPolyfillsVersion)` | netstandard2.0 polyfills |
| ANcpLua.Roslyn.Utilities.Testing | `$(ANcpLuaRoslynUtilitiesTestingVersion)` | Test infrastructure |
| xunit.v3.mtp-v2 | `$(XunitV3Version)` | Test framework |
| AwesomeAssertions | `$(AwesomeAssertionsVersion)` | Assertions |

`Version.props` is the source of truth — concrete numbers were intentionally dropped from this table because CI bumps them under you. Currently the Roslyn.Utilities chain is on the 2.2.x line (latest 2.2.21 as of 2026-05-23; `IsConstantZero` regression fix landed there — AL0014 floating-point detection depends on it, do not roll back below 2.2.21).

## SDK Integration Note

The SDK auto-injects this analyzer package. To prevent build cycle during development:
- Use `PackageId=Dummy` in local csproj
- CI workflow passes `-p:PackageId=ANcpLua.Analyzers`
