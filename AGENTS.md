# ANcpLua.Analyzers - Agent Contract

Canonical instruction surface for Codex and Claude. Keep `CLAUDE.md` symlinked to this file; edit this file only.

## Repo Role

`ANcpLua.Analyzers` is the concrete analyzer package in the ANcpLua framework chain:

```text
ANcpLua.Roslyn.Utilities -> ANcpLua.NET.Sdk -> ANcpLua.Analyzers -> consumers
```

`ANcpLua.Roslyn.Utilities` owns reusable Roslyn helpers. This repo owns concrete diagnostics, code fixes, docs generation, packaging, and tests. Telemetry-adjacent diagnostics moved to `ANcpLua.OpenTelemetry.SemanticConventions.Analyzers`; do not reintroduce OTel semantic-convention rules here unless explicitly requested. Tool-governance analyzers such as AL1800-AL1802 may remain here because they are not just OTel semantics.

Rule counts drift. Trust `src/ANcpLua.Analyzers/AnalyzerReleases.*.md` and `src/ANcpLua.Analyzers.CodeFixes/CodeFixes/AL*.cs` over prose.

## Commands

```bash
dotnet build ANcpLua.Analyzers.slnx -c Release
dotnet test --project tests/ANcpLua.Analyzers.Tests/ANcpLua.Analyzers.Tests.csproj
dotnet test --project tests/ANcpLua.Analyzers.Tests/ANcpLua.Analyzers.Tests.csproj --filter-method "*AL1000*"
dotnet pack src/ANcpLua.Analyzers/ANcpLua.Analyzers.csproj -c Release -o artifacts -p:PackageId=ANcpLua.Analyzers
```

Use `--filter-method`, not VSTest-style `--filter "FQN~..."`. Tests use xUnit v3 MTP. The meaningful full verifier is Release build plus the analyzer test project.

## Layout

```text
AGENTS.md                                           # canonical instructions
CLAUDE.md                                          # symlink to AGENTS.md
src/ANcpLua.Analyzers/
  AlAnalyzer.cs                                    # base class, CreateRule, HelpLink, action defaults
  Resources.resx                                   # {id}AnalyzerTitle/MessageFormat/Description
  AnalyzerReleases.Shipped.md
  AnalyzerReleases.Unshipped.md
  Analyzers/AL0XXX*.cs                             # one analyzer, or one grouped analyzer, per file
  Analyzers/AsyncContextHelper.cs
src/ANcpLua.Analyzers.CodeFixes/
  CodeFixes/AL0XXX*.cs
  Refactorings/AR0XXX*.cs
tests/ANcpLua.Analyzers.Tests/
  AL0XXX*Tests.cs
```

## Analyzer Shape

Single-rule analyzer:

```csharp
namespace ANcpLua.Analyzers.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al00xxDescriptiveNameAnalyzer : AlAnalyzer {
    private const string DiagnosticId = "AL00XX";

    private static readonly DiagnosticDescriptor Rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.Usage,
        DiagnosticSeverity.Warning);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.InvocationExpression);

    private static void Analyze(SyntaxNodeAnalysisContext context) {
        context.ReportDiagnostic(Rule, location, arg0);
    }
}
```

Naming contract:

| Element | Rule |
| --- | --- |
| File | `Analyzers/AL00XXDescriptiveNameAnalyzer.cs` |
| Class | `Al00xxDescriptiveNameAnalyzer`; use `Al`, not `AL` |
| Modifier | `sealed partial class` |
| Base | `AlAnalyzer` |
| Namespace | `ANcpLua.Analyzers.Analyzers` |
| Diagnostic ID | Per-analyzer `const string`; no central `DiagnosticIds` class |

Diagnostic ID visibility:

- `private` by default.
- `public` only when a sibling code fix references it via `FixableDiagnosticIds => [Al00xxAnalyzer.DiagnosticId]`.
- Do not make IDs public preemptively.

Descriptor construction:

- Single-rule analyzers use `CreateRule(id, category, severity)`.
- Grouped analyzers with multiple IDs in one class may manually create `DiagnosticDescriptor` instances with `LocalizableResourceString`.
- Do not manually construct single-rule descriptors.

Grouped analyzer skeleton:

```csharp
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al1003ToAl1004SpanComparisonAnalyzer : AlAnalyzer {
    public const string DiagnosticIdAl1003 = "AL1003";
    public const string DiagnosticIdAl1004 = "AL1004";

    private static readonly DiagnosticDescriptor RuleAl1003 = new(
        DiagnosticIdAl1003,
        new LocalizableResourceString(nameof(Resources.AL1003AnalyzerTitle), Resources.ResourceManager, typeof(Resources)),
        new LocalizableResourceString(nameof(Resources.AL1003AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources)),
        DiagnosticCategories.Usage,
        DiagnosticSeverity.Warning,
        true,
        new LocalizableResourceString(nameof(Resources.AL1003AnalyzerDescription), Resources.ResourceManager, typeof(Resources)),
        HelpLinkBase);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [RuleAl1003, RuleAl1004];
}
```

## Resources And Releases

Each new diagnostic needs three `Resources.resx` keys:

```text
AL00XXAnalyzerTitle
AL00XXAnalyzerMessageFormat
AL00XXAnalyzerDescription
```

`AnalyzerReleases.Unshipped.md` gets one row per diagnostic under `### New Rules`:

```text
Rule ID | Category | Severity | Notes
--------|----------|----------|-------
AL00XX | Usage | Warning | Al00xxDescriptiveNameAnalyzer
```

Severity names in release files: `Error`, `Warning`, `Info`, `Disabled`.

## Categories And Severity

Known categories from `ANcpLua.Roslyn.Utilities.Sources`:

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

Severity choices:

```csharp
DiagnosticSeverity.Error    // build-breaking
DiagnosticSeverity.Warning  // build-visible
DiagnosticSeverity.Info     // IDE only / hidden by default
```

Named aliases are also available:

```csharp
DiagnosticSeverities.RequiredFix
DiagnosticSeverities.Suggestion
DiagnosticSeverities.HiddenByDefault
```

Changing diagnostic severity or removing a diagnostic is a consumer-visible breaking change.

## Roslyn Utility Rules

Prefer source-package helpers from `ANcpLua.Roslyn.Utilities.Sources`:

```csharp
type.IsEqualTo(other)
type.Implements(interfaceType)
type.InheritsFrom(baseType)
str.StartsWithOrdinal(prefix)
operation.UnwrapAllConversions()
operation.GetOperandName("fallback")
context.ReportDiagnostic(Rule, location, args)
```

Use these instead of manual `SymbolEqualityComparer`, walking `AllInterfaces`, walking `BaseType`, or constructing `Diagnostic.Create` directly. Raw `Diagnostic.Create` is fine only when custom properties are needed.

Use `AsyncContextHelper.IsInsideAsyncContext(node)` for async-context detection. AL1304 and AL1305 use it. Do not duplicate the walk-up loop.

Use `OperationHelper` for argument-exception checks:

```csharp
OperationHelper.IsArgumentNullException(type)
OperationHelper.IsArgumentException(type)
OperationHelper.IsArgumentOutOfRangeException(type)
OperationHelper.IsAnyArgumentException(type)
```

## Type Resolution

Use `RegisterCompilationStartAction` when resolving types:

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

For two or more type resolutions, prefer `TypeCache<TEnum>`:

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

Adopted examples include AL1100, AL1104, AL1701, AL1202, AL1305, and AL1109; re-grep if stale.

## Test Shape

Analyzer test:

```csharp
using AnalyzerTestBase = ANcpLua.Roslyn.Utilities.Testing.AnalyzerTest<ANcpLua.Analyzers.Analyzers.Al00xxDescriptiveNameAnalyzer>;

namespace ANcpLua.Analyzers.Tests;

public sealed partial class Al00xxDescriptiveNameTests : AnalyzerTestBase {
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
| File | `tests/ANcpLua.Analyzers.Tests/AL00XXDescriptiveNameTests.cs` |
| Class | `Al00xxDescriptiveNameTests` |
| Modifier | `sealed partial class` |
| Base | Use `AnalyzerTestBase` alias |
| Namespace | `ANcpLua.Analyzers.Tests` |
| Return type | `Task` |
| Diagnostic span | `[|span|]` |
| Negative test | no marker means zero diagnostics |

Code fix tests inherit `CodeFixTest<TAnalyzer, TCodeFixProvider>`.

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

1. Allocate an unused ID by checking both shipped and unshipped release files.
2. Search existing analyzers to avoid duplicate coverage.
3. Add analyzer file under `src/ANcpLua.Analyzers/Analyzers/`.
4. Add resources.
5. Add release row.
6. Add analyzer tests with positive and negative cases.
7. Add code fix provider and code fix tests only when a safe edit exists.
8. Build and test.

Anti-duplication checks:

- Async checks use `AsyncContextHelper`.
- Type identity uses `.IsEqualTo`.
- Interface checks use `.Implements`.
- Base type checks use `.InheritsFrom`.
- Reporting uses `context.ReportDiagnostic`.
- No hot-path `compilation.GetSemanticModel(otherTree)`.

## Performance Rules

- Prefer operation analysis when semantic context is needed; syntax analysis is fine for pure syntax.
- Use `RegisterCompilationStartAction` for per-compilation type lookup.
- Pre-index cross-tree facts instead of asking for semantic models from unrelated trees in hot paths.
- Use `foreach` over Roslyn collections; avoid LINQ allocation on analyzer hot paths.
- Do not compare type identity via `ToDisplayString()`.

## Banned Or Sensitive Patterns

| Avoid | Use |
| --- | --- |
| `FluentAssertions` | `AwesomeAssertions` |
| `Microsoft.NET.Test.Sdk` | `xunit.v3.mtp-v2` |
| `--filter "FQN~..."` | `--filter-method` |
| `LangVersion` / `Nullable` in csproj | SDK-owned defaults |
| central `DiagnosticIds` class | per-analyzer `const string DiagnosticId` |
| manual interface/base walks | Roslyn.Utilities extensions |
| hot-path LINQ | direct loops |

## Package Shape

```text
ANcpLua.Analyzers.dll            -> analyzers/dotnet/cs/
ANcpLua.Analyzers.CodeFixes.dll  -> analyzers/dotnet/cs/
```

Both assemblies must be in the nupkg for IDE code fix integration. Local development can use `PackageId=Dummy` to avoid a self-reference cycle. CI passes `-p:PackageId=ANcpLua.Analyzers`.

## Version Chain

Version truth usually lives in `ANcpLua.NET.Sdk/src/Build/Common/Version.props`, then flows into consumer projects through the SDK. This repo may also have a local `Version.props` override imported after the SDK copy.

Rules before touching versions:

- Do not point SDK truth at a package version that is not on NuGet; restore fails with `NU1102`.
- A repo-local variable for this repo's own package should point to the last published version, not the version being built.
- CI stamps the new package version at pack time.
- Central Package Management turns transitive downgrade conflicts into hard restore errors; read `NU1109` details before retrying.
- A local override equal to or below SDK truth is stale; prune it when the SDK publishes matching values.
- Publish is tag-driven (`v*`) and gated by tests. If a tag points to a broken commit, use the next patch version rather than force-moving remote tags.
- Verify package versions through the NuGet flat-container API before bumping.

Useful variable names:

| Package | Variable |
| --- | --- |
| Microsoft.CodeAnalysis.CSharp | `$(RoslynVersion)` |
| ANcpLua.Roslyn.Utilities | `$(ANcpLuaRoslynUtilitiesVersion)` |
| ANcpLua.Roslyn.Utilities.Sources | `$(ANcpLuaRoslynUtilitiesSourcesVersion)` |
| ANcpLua.Roslyn.Utilities.Polyfills | `$(ANcpLuaRoslynUtilitiesPolyfillsVersion)` |
| ANcpLua.Roslyn.Utilities.Testing | `$(ANcpLuaRoslynUtilitiesTestingVersion)` |
| xunit.v3.mtp-v2 | `$(XunitV3Version)` |
| AwesomeAssertions | `$(AwesomeAssertionsVersion)` |

As of 2026-05-23, the Roslyn.Utilities chain should stay on the 2.2.x line at or above 2.2.21; AL1010 floating-point detection depends on the `IsConstantZero` regression fix.

## Cross-Repo Bootstrap

The framework repos are coupled:

```text
ANcpLua.Roslyn.Utilities
ANcpLua.NET.Sdk
ANcpLua.Analyzers
ANcpLua.Agents
```

Changing package variables has transitive effects because the SDK injects analyzer packages through `GlobalPackageReference`. Publish lower layers before moving SDK truth upward.

Branch protection, release flow, dependency graph, and shared framework conventions live in `ANcpLua/renovate-config`. This file only records repo-local rules.

