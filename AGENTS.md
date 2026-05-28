# ANcpLua.Analyzers - Agent Contract

Canonical instruction surface for Codex and Claude. Keep the repo-root `CLAUDE.md`
symlinked to this file; edit this file for repo-wide agent rules. Nested
`CLAUDE.md` files are project-local notes and are not the root contract.

## Working Rule

Do not use this file as permission for generic cleanup or best-practice churn.
Cleanup means removing stale, wrong, duplicate, or unowned repo state that is
proven from the checkout. Preserve unrelated dirty files unless the user
explicitly asks to handle them.

## Repo Role

`ANcpLua.Analyzers` is the concrete analyzer package in the ANcpLua framework
chain:

```text
ANcpLua.Roslyn.Utilities -> ANcpLua.NET.Sdk -> ANcpLua.Analyzers -> consumers
```

`ANcpLua.Roslyn.Utilities` owns reusable Roslyn helpers. This repo owns concrete
diagnostics, code fixes, docs generation, packaging, and tests. OTel semantic
convention diagnostics moved out of this repo; do not reintroduce them here
unless explicitly requested. Agent/tool-governance analyzers such as
`AL1800`-`AL1802` may remain here because they are not OTel semantic-convention
rules.

Current active rule IDs live in the `AL1000..AL1899` domain bands. `AL0xxx` is
reserved for sibling analyzer packages and migration mapping only. Rule counts
drift; trust `src/ANcpLua.Analyzers/AnalyzerReleases.*.md`, analyzer classes,
and the docs generator output over prose.

## Commands

```bash
dotnet build ANcpLua.Analyzers.slnx -c Release
dotnet test --project tests/ANcpLua.Analyzers.Tests/ANcpLua.Analyzers.Tests.csproj
dotnet test --project tests/ANcpLua.Analyzers.Tests/ANcpLua.Analyzers.Tests.csproj --filter-method "*AL1000*"
dotnet run --project tools/ANcpLua.Analyzers.DocsGenerator --configuration Release -- --check
dotnet run --project tools/ANcpLua.Analyzers.DocsGenerator --configuration Release -- --enforce-ids
dotnet pack src/ANcpLua.Analyzers/ANcpLua.Analyzers.csproj -c Release -o artifacts -p:PackageId=ANcpLua.Analyzers
```

Use `--filter-method`, not VSTest-style `--filter "FQN~..."`. Tests use xUnit
v3 MTP. The meaningful full verifier is Release build plus the analyzer test
project; the Release build also runs the analyzer consistency/docs drift guard
unless `-p:SkipAnalyzerConsistencyCheck=true` is passed.

## Layout

```text
AGENTS.md
CLAUDE.md                                          # repo-root symlink to AGENTS.md
Directory.Build.props                             # common package/build metadata
Directory.Packages.props                          # CPM versions; imports Version.props
Version.props                                     # local overrides ahead of SDK truth only
docs/
  ANcpLua.Analyzers.md                            # generated catalog index
  ANcpLua.Analyzers.sarif                         # generated SARIF manifest
  editorconfig/*.editorconfig                     # generated severity profiles
  rules/AL1XXX_*.md                               # generated per-rule help pages
src/ANcpLua.Analyzers/
  AlAnalyzer.cs                                   # base class, CreateRule, RuleDocs help links
  RuleDocs.cs                                     # help-link URL composition
  Resources.resx                                  # AL1XXXAnalyzerTitle/MessageFormat/Description
  AnalyzerReleases.Shipped.md
  AnalyzerReleases.Unshipped.md
  Analyzers/AL1XXX*.cs                            # one analyzer, grouped analyzer, or helper
  build/ANcpLua.Analyzers.props                   # CompilerVisibleProperty surface
  buildTransitive/ANcpLua.Analyzers.props         # AlAnalysisMode profile hook
src/ANcpLua.Analyzers.CodeFixes/
  CodeFixes/AL1XXX*.cs
  Refactorings/AR0XXX*.cs
tools/ANcpLua.Analyzers.DocsGenerator/
  *.cs                                            # catalog/docs/editorconfig/SARIF generator
tests/ANcpLua.Analyzers.Tests/
  AL1XXX*Tests.cs
  AnalyzerConventionTests.cs
```

## Analyzer Shape

Single-rule analyzer:

```csharp
namespace ANcpLua.Analyzers.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al1xxxDescriptiveNameAnalyzer : AlAnalyzer {
    public const string DiagnosticId = "AL1XXX";

    private static readonly DiagnosticDescriptor s_rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.Usage,
        DiagnosticSeverity.Warning);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_rule];

    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.InvocationExpression);

    private static void Analyze(SyntaxNodeAnalysisContext context) {
        context.ReportDiagnostic(s_rule, location, arg0);
    }
}
```

Naming contract:

| Element | Rule |
| --- | --- |
| File | `Analyzers/AL1XXXDescriptiveNameAnalyzer.cs` |
| Class | `Al1xxxDescriptiveNameAnalyzer`; use `Al`, not `AL` |
| Modifier | `sealed partial class` |
| Base | Prefer `AlAnalyzer`; direct `DiagnosticAnalyzer` is acceptable for compilation/additional-file analyzers that need custom initialization, properties, or diagnostic tags |
| Namespace | `ANcpLua.Analyzers.Analyzers` |
| Diagnostic ID | Per-analyzer `const string`; no central `DiagnosticIds` class |

Diagnostic ID visibility:

- `private` by default.
- `public` only when a sibling code fix references it via
  `FixableDiagnosticIds => [Al1xxxAnalyzer.DiagnosticId]`.
- Do not make IDs public preemptively.

Descriptor construction:

- Single-rule `AlAnalyzer` analyzers use `CreateRule(id, category, severity)`.
- `CreateRule` derives the help-link URL through `RuleDocs`; do not hardcode
  help links.
- Hand-built descriptors use `RuleDocs.HelpLinkAuto(id)` unless there is a
  proven reason to provide an explicit symbolic name.
- `WellKnownDiagnosticTags.CompilationEnd` and custom diagnostic properties are
  valid reasons to construct `DiagnosticDescriptor`/`Diagnostic.Create`
  manually.

Grouped analyzer skeleton:

```csharp
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al1100ToAl1104FormBindingAnalyzer : AlAnalyzer {
    private const string DiagnosticIdAl1100 = "AL1100";
    private const string DiagnosticIdAl1101 = "AL1101";

    private static readonly DiagnosticDescriptor s_ruleAl1100 = CreateRule(
        DiagnosticIdAl1100,
        DiagnosticCategories.AspNetCore,
        DiagnosticSeverity.Error);

    private static readonly DiagnosticDescriptor s_ruleAl1101 = CreateRule(
        DiagnosticIdAl1101,
        DiagnosticCategories.AspNetCore,
        DiagnosticSeverity.Error);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [s_ruleAl1100, s_ruleAl1101];
}
```

## Resources, Releases, And Docs

Each new diagnostic needs three `Resources.resx` keys:

```text
AL1XXXAnalyzerTitle
AL1XXXAnalyzerMessageFormat
AL1XXXAnalyzerDescription
```

`AnalyzerReleases.Unshipped.md` gets one row per diagnostic under `### New Rules`:

```text
Rule ID | Category | Severity | Notes
--------|----------|----------|-------
AL1XXX | Usage | Warning | Al1xxxDescriptiveNameAnalyzer
```

Severity names in release files: `Error`, `Warning`, `Info`, `Disabled`.

Generated surfaces:

- `docs/ANcpLua.Analyzers.md`
- `docs/rules/AL1XXX_*.md`
- `docs/editorconfig/*.editorconfig`
- `docs/ANcpLua.Analyzers.sarif`
- `docs/migration-catalog.md`

Use the docs generator for those files:

```bash
dotnet run --project tools/ANcpLua.Analyzers.DocsGenerator --configuration Release
dotnet run --project tools/ANcpLua.Analyzers.DocsGenerator --configuration Release -- --check
dotnet run --project tools/ANcpLua.Analyzers.DocsGenerator --configuration Release -- --enforce-ids
dotnet run --project tools/ANcpLua.Analyzers.DocsGenerator --configuration Release -- --enforce-ids apply
```

`AnalyzerConventionTests` validates naming, diagnostic IDs, help links, and the
old-to-new ID migration catalog. When removing a rule, delete the analyzer,
code fix, tests, resources, release row, generated docs/editorconfig/SARIF
entries, README/catalog mentions, and migration leftovers that no longer apply.

`AlIdMigrationCatalog` in the docs-generator tool is the old-to-new ID mapping
source for `docs/migration-catalog.md`. Do not keep a second hand-maintained
renumber plan beside it.

## Categories And Severity

Known categories from `ANcpLua.Roslyn.Utilities` include:

```csharp
DiagnosticCategories.Design
DiagnosticCategories.Reliability
DiagnosticCategories.Threading
DiagnosticCategories.Usage
DiagnosticCategories.Style
DiagnosticCategories.AotTesting
DiagnosticCategories.AspNetCore
DiagnosticCategories.OpenTelemetry
DiagnosticCategories.GenAI
DiagnosticCategories.Metrics
DiagnosticCategories.Configuration
DiagnosticCategories.VersionManagement
DiagnosticCategories.RoslynUtilities
```

The OpenTelemetry category may exist in the shared helper surface, but this repo
should not add OTel semantic-convention rules without an explicit request.

Severity choices:

```csharp
DiagnosticSeverity.Error    // build-breaking
DiagnosticSeverity.Warning  // build-visible
DiagnosticSeverity.Info     // IDE/info profile
```

Named aliases are also available:

```csharp
DiagnosticSeverities.RequiredFix
DiagnosticSeverities.Suggestion
DiagnosticSeverities.HiddenByDefault
```

Changing diagnostic severity, ID, category, or removing a diagnostic is
consumer-visible and must be treated as release-surface work.

## Roslyn Utility Rules

Prefer helpers from `ANcpLua.Roslyn.Utilities`:

```csharp
type.IsEqualTo(other)
type.Implements(interfaceType)
type.InheritsFrom(baseType)
str.StartsWithOrdinal(prefix)
str.EndsWithOrdinal(suffix)
str.EqualsOrdinal(other)
operation.UnwrapAllConversions()
operation.GetOperandName("fallback")
context.ReportDiagnostic(s_rule, location, args)
```

Use these instead of manual `SymbolEqualityComparer`, walking `AllInterfaces`,
walking `BaseType`, or constructing `Diagnostic.Create` directly. Raw
`Diagnostic.Create` is fine when custom properties, fallback locations, or
diagnostic tags are needed.

Use `AsyncContextHelper.IsInsideAsyncContext(node)` for async-context detection.
AL1304 and AL1305 use it. Do not duplicate the walk-up loop.

Use `OperationHelper` for argument-exception checks:

```csharp
OperationHelper.IsArgumentNullException(type)
OperationHelper.IsArgumentException(type)
OperationHelper.IsArgumentOutOfRangeException(type)
OperationHelper.IsAnyArgumentException(type)
```

## Type Resolution

Use `RegisterCompilationStartAction` when resolving compilation types:

```csharp
protected override void RegisterActions(AnalysisContext context) =>
    context.RegisterCompilationStartAction(OnCompilationStart);

private static void OnCompilationStart(CompilationStartAnalysisContext context) {
    if (context.Compilation.GetTypeByMetadataName("System.IAsyncDisposable") is not { } asyncDisposableType) {
        return;
    }

    context.RegisterSyntaxNodeAction(
        ctx => Analyze(ctx, asyncDisposableType),
        SyntaxKind.UsingStatement);
}
```

For two or more type resolutions, prefer `TypeCache<TEnum>` and a local
metadata-name map:

```csharp
private enum KnownType { Task, TaskOfT, ValueTask, ValueTaskOfT }

private static readonly string[] KnownTypeNames = [
    "System.Threading.Tasks.Task",
    "System.Threading.Tasks.Task`1",
    "System.Threading.Tasks.ValueTask",
    "System.Threading.Tasks.ValueTask`1"
];

var cache = new TypeCache<KnownType>(
    type => context.Compilation.GetTypeByMetadataName(KnownTypeNames[(int)type]));
```

Adopted examples include AL1100, AL1104, AL1701, AL1202, AL1305, and AL1109;
re-grep because this list can drift.

## Test Shape

Analyzer test:

```csharp
using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

public sealed partial class Al1xxxDescriptiveNameTests
    : AnalyzerTest<Al1xxxDescriptiveNameAnalyzer> {
    [Fact]
    public Task ShouldReportWhenConditionMet() =>
        VerifyAsync("""
                    public class C {
                        public void M() {
                            [|Bad()|];
                        }
                    }
                    """);

    [Fact]
    public Task ShouldNotReportWhenConditionNotMet() =>
        VerifyAsync("""
                    public class C {
                        public void M() {
                            Good();
                        }
                    }
                    """);
}
```

Test contract:

| Element | Rule |
| --- | --- |
| File | `tests/ANcpLua.Analyzers.Tests/AL1XXXDescriptiveNameTests.cs` |
| Class | `Al1xxxDescriptiveNameTests` or grouped per-ID test classes for grouped analyzers |
| Modifier | `sealed partial class` |
| Base | `AnalyzerTest<TAnalyzer>`, `CodeFixTest<TAnalyzer, TCodeFixProvider>`, or `CodeFixTestWithEditorConfig<...>` |
| Namespace | `ANcpLua.Analyzers.Tests` |
| Return type | `Task` |
| Diagnostic span | `[|span|]` or `{|AL1XXX:span|}` |
| Negative test | no marker means zero diagnostics |

External stubs should be local constants:

```csharp
private const string AspNetCoreStubs = """
                                       namespace Microsoft.AspNetCore.Mvc {
                                           public abstract class ControllerBase { }
                                           public abstract class Controller : ControllerBase { }
                                       }
                                       """;
```

Use `$$"""` raw interpolation when inserting `{{StubConstant}}`.

## New Analyzer Checklist

1. Allocate an unused `AL1xxx` ID in the correct domain band by checking both
   shipped and unshipped release files, existing analyzers, generated docs, and
   the migration catalog.
2. Search existing analyzers to avoid duplicate coverage or reintroducing
   migrated OTel semantic-convention rules.
3. Add analyzer file under `src/ANcpLua.Analyzers/Analyzers/`.
4. Add resources.
5. Add release row.
6. Add analyzer tests with positive and negative cases.
7. Add code fix provider and code fix tests only when a safe edit exists.
8. Regenerate/check docs with `tools/ANcpLua.Analyzers.DocsGenerator`.
9. Run Release build and focused/full analyzer tests.

Anti-duplication checks:

- Async checks use `AsyncContextHelper`.
- Type identity uses `.IsEqualTo`.
- Interface checks use `.Implements`.
- Base type checks use `.InheritsFrom`.
- Reporting uses `context.ReportDiagnostic` unless custom properties/tags force
  raw `Diagnostic.Create`.
- No hot-path `compilation.GetSemanticModel(otherTree)`.

## Performance Rules

- Prefer operation analysis when semantic context is needed; syntax analysis is
  fine for pure syntax.
- Use `RegisterCompilationStartAction` for per-compilation type lookup.
- Pre-index cross-tree facts instead of asking for semantic models from unrelated
  trees in hot paths.
- Use `foreach` over Roslyn collections; avoid LINQ allocation on analyzer hot
  paths.
- Do not compare type identity via `ToDisplayString()`.

## Banned Or Sensitive Patterns

| Avoid | Use |
| --- | --- |
| `FluentAssertions` | `AwesomeAssertions` |
| `Microsoft.NET.Test.Sdk` | `xunit.v3.mtp-v2` / SDK-injected MTP packages |
| `--filter "FQN~..."` | `--filter-method` |
| `LangVersion` / `Nullable` in shipping csproj files | SDK-owned defaults |
| central `DiagnosticIds` class | per-analyzer `const string DiagnosticId` |
| manual interface/base walks | Roslyn.Utilities extensions |
| hot-path LINQ | direct loops |

The tools docs-generator project intentionally uses plain `Microsoft.NET.Sdk`
and pins its Roslyn package inline because CPM evaluation from `tools/` does not
pick up the repo version variables. Do not "fix" that into the normal SDK
pattern without revalidating the tool bootstrap.

## Package Shape

```text
ANcpLua.Analyzers.dll            -> analyzers/dotnet/cs/
ANcpLua.Analyzers.CodeFixes.dll  -> analyzers/dotnet/cs/
ANcpLua.Roslyn.Utilities.dll     -> analyzers/dotnet/cs/
build/ANcpLua.Analyzers.props    -> build/
buildTransitive/ANcpLua.Analyzers.props
docs/editorconfig/*.editorconfig -> buildTransitive/editorconfig/
```

Both analyzer assemblies must be in the nupkg for IDE code fix integration.
`ANcpLua.Roslyn.Utilities.dll` is bundled for analyzer runtime dependencies.
`buildTransitive/ANcpLua.Analyzers.props` exposes `<AlAnalysisMode>` and imports
the generated editorconfig profiles for consumers.

Local development uses the project default `PackageId=Dummy` to avoid the SDK's
self-reference cycle. CI/release pack uses `-p:PackageId=ANcpLua.Analyzers`.

## Version Chain

Version truth usually lives in the published `ANcpLua.NET.Sdk` package's
`Build/Common/Version.props`, then flows into consumer projects through the SDK.
This repo's `Version.props` is a local override imported by
`Directory.Packages.props` after the SDK copy.

Rules before touching versions:

- Do not point SDK truth at a package version that is not on NuGet; restore
  fails with `NU1102`.
- A repo-local variable for this repo's own package should point to the last
  published version, not the version being built.
- `VersionPrefix`/`VersionSuffix` in `Directory.Build.props` describe local pack
  output; they are not the self-reference package version.
- CI stamps the new package version at pack time from the `v*` tag.
- Central Package Management turns transitive downgrade conflicts into hard
  restore errors; read `NU1109` details before retrying.
- A local override equal to or below SDK truth is stale; prune it when the SDK
  publishes matching values.
- Publish is tag-driven (`v*`) and gated by tests. If a tag points to a broken
  commit, use the next patch version rather than force-moving remote tags.
- Verify package versions through the NuGet flat-container API before bumping.

Useful variable names:

| Package | Variable |
| --- | --- |
| Microsoft.CodeAnalysis.CSharp / Workspaces | `$(RoslynVersion)` |
| ANcpLua.Analyzers | `$(ANcpLuaAnalyzersVersion)` |
| ANcpLua.Roslyn.Utilities | `$(ANcpLuaRoslynUtilitiesVersion)` |
| ANcpLua.Roslyn.Utilities.Sources | `$(ANcpLuaRoslynUtilitiesSourcesVersion)` |
| ANcpLua.Roslyn.Utilities.Polyfills | `$(ANcpLuaRoslynUtilitiesPolyfillsVersion)` |
| ANcpLua.Roslyn.Utilities.Testing | `$(ANcpLuaRoslynUtilitiesTestingVersion)` |
| xunit.v3.mtp-v2 | `$(XunitV3Version)` |
| AwesomeAssertions | `$(AwesomeAssertionsVersion)` |

As of the current local `Version.props`, the Roslyn.Utilities chain is on the
`2.2.x` line. Do not downgrade below the line that contains the AL1010
`IsConstantZero` regression fix.

## Cross-Repo Bootstrap

The framework repos are coupled:

```text
ANcpLua.Roslyn.Utilities
ANcpLua.NET.Sdk
ANcpLua.Analyzers
ANcpLua.Agents
```

Changing package variables has transitive effects because the SDK injects
analyzer packages through `GlobalPackageReference`. Publish lower layers before
moving SDK truth upward.

Branch protection, release flow, dependency graph, and shared framework
conventions live in `ANcpLua/renovate-config`. This file only records repo-local
rules.
