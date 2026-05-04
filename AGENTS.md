# AGENTS.md - ANcpLua.Analyzers

127 Roslyn diagnostic analyzers (AL0001-AL0131, gaps at AL0097-0100) with 46 code fixes, targeting netstandard2.0.

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
| Microsoft.CodeAnalysis.CSharp | `$(RoslynVersion)` 5.3.0 | Roslyn APIs |
| ANcpLua.Roslyn.Utilities | `$(ANcpLuaRoslynUtilitiesVersion)` 2.0.4 | Binary package |
| ANcpLua.Roslyn.Utilities.Sources | `$(ANcpLuaRoslynUtilitiesSourcesVersion)` 2.0.4 | Compile-time source package |
| ANcpLua.Roslyn.Utilities.Polyfills | `$(ANcpLuaRoslynUtilitiesPolyfillsVersion)` 2.0.4 | netstandard2.0 polyfills |
| ANcpLua.Roslyn.Utilities.Testing | `$(ANcpLuaRoslynUtilitiesTestingVersion)` 2.0.4 | Test infrastructure |
| xunit.v3.mtp-v2 | `$(XunitV3Version)` 3.2.2 | Test framework |
| AwesomeAssertions | `$(AwesomeAssertionsVersion)` 9.4.0 | Assertions |

Re-read `Version.props` before trusting these numbers — CI bumps them under you. Analyzers is on the v2.x Roslyn.Utilities line.

## SDK Integration Note

The SDK auto-injects this analyzer package. To prevent build cycle during development:
- Use `PackageId=Dummy` in local csproj
- CI workflow passes `-p:PackageId=ANcpLua.Analyzers`

## ANcpLua Ecosystem

| Repo | Purpose | NuGet | CI checks required |
|---|---|---|---|
| [ANcpLua.NET.Sdk](https://github.com/ANcpLua/ANcpLua.NET.Sdk) | Opinionated MSBuild SDK — standardized defaults, policy enforcement, analyzer injection | [nuget.org](https://www.nuget.org/packages/ANcpLua.NET.Sdk) | `compute_version`, `lint_config`, `test (ubuntu/windows/macos)`, `create_nuget` |
| [ANcpLua.Analyzers](https://github.com/ANcpLua/ANcpLua.Analyzers) | Custom Roslyn analyzers (auto-injected by the SDK) | [nuget.org](https://www.nuget.org/packages/ANcpLua.Analyzers) | `build`, `test (ubuntu/windows/macos)` |
| [ANcpLua.Roslyn.Utilities](https://github.com/ANcpLua/ANcpLua.Roslyn.Utilities) | Source generator utilities, TryParse extensions, polyfills | [nuget.org](https://www.nuget.org/packages/ANcpLua.Roslyn.Utilities) | `build (ubuntu/windows)`, `version` |
| [ANcpLua.Agents](https://github.com/ANcpLua/ANcpLua.Agents) | MAF runtime helpers + agent test infrastructure | [nuget.org](https://www.nuget.org/packages/ANcpLua.Agents) | `build (ubuntu/windows/macos)`, `version` |

### Branch protection (all 4 repos)

- PR required to merge into `main` (0 approvals, squash preferred)
- Required status checks must pass (CI jobs listed above)
- Branch must be up-to-date with `main` before merge
- Force push and branch deletion blocked on `main`
- Optional checks (CodeRabbit, GitGuardian, Copilot review, auto-merge) do not block merges

### Dependency graph

```
ANcpLua.NET.Sdk
  ├── injects ANcpLua.Analyzers (compile-time)
  └── ships Version.props (version truth for all consumers)

ANcpLua.Analyzers
  └── consumes ANcpLua.Roslyn.Utilities.Sources (source-only, internal)

ANcpLua.Roslyn.Utilities
  └── standalone (no first-party deps)

ANcpLua.Agents
  └── standalone (no first-party deps)
```

### Release flow

Manual-tag-triggers-publish. The workflow ignores `push: main` for publishing — only `push: tags v*` (or `workflow_dispatch`) runs the publish job.

1. PR to `main` via squash merge — `ci.yml` runs build + test; `nuget-publish.yml` does **not** run
2. After merge: `git tag vX.Y.Z && git push --tags` — version comes from `${GITHUB_REF_NAME#v}`
3. Workflow restores, builds, packs, and pushes to NuGet via trusted publishing
4. **No GH release is auto-created** (workflow doesn't call `gh release create`); the tag itself is the marker — create the release manually if needed
5. NuGet indexes in ~4-8 minutes — downstream repos pick up via Renovate

Note: ANcpLua.NET.Sdk uses a different pattern (auto-bump-on-merge + auto-tag); Roslyn.Utilities and Agents use the same manual-tag pattern as this repo, but additionally auto-create the GH release.
