# ANcpLua.Analyzers — Agent Code Generation Contract

> **Purpose:** Zero-ambiguity reference so any AI agent produces compilable analyzer code on the first attempt.

**Target:** netstandard2.0 | Roslyn 5.0.0 | C# 13

---

## 1. Canonical Analyzer Template (Single Rule)

```csharp
namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL00XX: One-sentence description of what this detects.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al00xxDescriptiveNameAnalyzer : AlAnalyzer {
    public const string DiagnosticId = "AL00XX";

    private static readonly DiagnosticDescriptor Rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.CATEGORY,      // see Section 5
        DiagnosticSeverity.Warning);         // or DiagnosticSeverities.X — see Section 6

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    protected override void RegisterActions(AnalysisContext context) {
        // Pick ONE registration strategy:
        context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.Whatever);
        // OR context.RegisterOperationAction(Analyze, OperationKind.Whatever);
        // OR context.RegisterSymbolAction(Analyze, SymbolKind.Whatever);
        // OR context.RegisterCompilationStartAction(OnCompilationStart); — see Section 8
    }

    private static void Analyze(SyntaxNodeAnalysisContext context) {
        // ... analysis logic ...

        // Report using the extension method (preferred for new code):
        context.ReportDiagnostic(Rule, location, messageArgs);

        // OR using raw Roslyn API (both compile, both work):
        // context.ReportDiagnostic(Diagnostic.Create(Rule, location, messageArgs));
    }
}
```

### Naming Contract

| Element | Convention | Example |
|---------|-----------|---------|
| File | `Analyzers/AL00XXDescriptiveNameAnalyzer.cs` | `AL0104PreferAwaitUsingAnalyzer.cs` |
| Class | `Al00xxDescriptiveNameAnalyzer` — note `Al` not `AL` | `Al0104PreferAwaitUsingAnalyzer` |
| Modifier | Always `sealed partial class` | |
| Base | Always `: AlAnalyzer` | |
| Namespace | `ANcpLua.Analyzers.Analyzers` | |
| DiagnosticId | `public const string DiagnosticId = "AL00XX"` — local to each analyzer | |

### CRITICAL: No Central DiagnosticIds Class

Each analyzer owns its own `DiagnosticId` as a `public const string`. There is NO shared `DiagnosticIds` class.

### CRITICAL: Use CreateRule(), NOT new DiagnosticDescriptor()

`CreateRule()` is inherited from `AlAnalyzer`. It constructs `LocalizableResourceString` instances automatically from the ID. Never manually construct `new DiagnosticDescriptor(...)` with `LocalizableResourceString` for single-rule analyzers.

---

## 2. Grouped Analyzer Template (Multiple Rules, One Class)

When one class emits multiple diagnostic IDs (e.g., AL0004 + AL0005):

```csharp
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0004ToAl0005SpanComparisonAnalyzer : AlAnalyzer {
    public const string DiagnosticIdAl0004 = "AL0004";
    public const string DiagnosticIdAl0005 = "AL0005";

    // For grouped analyzers, manually construct LocalizableResourceStrings:
    private static readonly DiagnosticDescriptor RuleAl0004 = new(
        DiagnosticIdAl0004,
        new LocalizableResourceString(nameof(Resources.AL0004AnalyzerTitle), Resources.ResourceManager, typeof(Resources)),
        new LocalizableResourceString(nameof(Resources.AL0004AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources)),
        DiagnosticCategories.Usage, DiagnosticSeverity.Warning, true,
        new LocalizableResourceString(nameof(Resources.AL0004AnalyzerDescription), Resources.ResourceManager, typeof(Resources)),
        HelpLinkBase);

    private static readonly DiagnosticDescriptor RuleAl0005 = new(
        DiagnosticIdAl0005,
        new LocalizableResourceString(nameof(Resources.AL0005AnalyzerTitle), Resources.ResourceManager, typeof(Resources)),
        new LocalizableResourceString(nameof(Resources.AL0005AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources)),
        DiagnosticCategories.Usage, DiagnosticSeverity.Warning, true,
        new LocalizableResourceString(nameof(Resources.AL0005AnalyzerDescription), Resources.ResourceManager, typeof(Resources)),
        HelpLinkBase);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [RuleAl0004, RuleAl0005];

    // ... rest follows same patterns ...
}
```

**Decision rule:** Use `CreateRule()` for single-rule analyzers. Use manual `new DiagnosticDescriptor(...)` for grouped analyzers (multiple rules in one class).

---

## 3. Resource Naming Contract

`CreateRule(id, ...)` constructs resource keys as `{id}AnalyzerTitle`, `{id}AnalyzerMessageFormat`, `{id}AnalyzerDescription`.

For every new analyzer, add three entries to `Resources.resx`:

| Key | Purpose | Example value |
|-----|---------|---------------|
| `AL00XXAnalyzerTitle` | Short title shown in IDE | `Prefer 'await using' for IAsyncDisposable` |
| `AL00XXAnalyzerMessageFormat` | Message with `{0}` placeholders | `Type '{0}' implements IAsyncDisposable; use 'await using'` |
| `AL00XXAnalyzerDescription` | Longer explanation | `When a type implements IAsyncDisposable...` |

---

## 4. AnalyzerReleases.Unshipped.md Entry

Add one line per diagnostic ID under `### New Rules`:

```
Rule ID | Category | Severity | Notes
--------|----------|----------|-------
AL00XX | Category | Severity | Al00xxDescriptiveNameAnalyzer
```

- **Category** matches the `DiagnosticCategories` constant name (e.g., `Reliability`, `ASP.NET Core`)
- **Severity** matches the AnalyzerReleases convention: `Error`, `Warning`, `Info`, `Disabled`
- **Notes** is the class name

---

## 5. DiagnosticCategories (from ANcpLua.Roslyn.Utilities.Sources)

```
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

---

## 6. Severity Options

Two ways to specify severity. Both compile. Both are used in the codebase.

### Raw Roslyn (used by most analyzers)

```csharp
DiagnosticSeverity.Error    // Fails build
DiagnosticSeverity.Warning  // Appears in build output
DiagnosticSeverity.Info     // IDE only, hidden from build
```

### Named Constants (from ANcpLua.Roslyn.Utilities.Sources)

```csharp
DiagnosticSeverities.RequiredFix     // => DiagnosticSeverity.Error
DiagnosticSeverities.Suggestion      // => DiagnosticSeverity.Warning
DiagnosticSeverities.HiddenByDefault // => DiagnosticSeverity.Info
```

Either form is acceptable. Be consistent within a single analyzer.

---

## 7. Extension Methods (from ANcpLua.Roslyn.Utilities.Sources)

These are compile-time source-generated. Prefer them over raw Roslyn equivalents.

### ITypeSymbol Extensions

```csharp
type.IsEqualTo(otherType)        // SymbolEqualityComparer.Default.Equals
type.Implements(interfaceType)   // checks AllInterfaces
type.InheritsFrom(baseType)      // walks BaseType chain
```

### String Extensions

```csharp
str.StartsWithOrdinal(prefix)    // ordinal comparison
```

### Context Extensions (SyntaxNodeAnalysisContext, OperationAnalysisContext)

```csharp
// Shorthand for Diagnostic.Create + report. Works on both context types.
context.ReportDiagnostic(Rule, location);
context.ReportDiagnostic(Rule, location, arg0);
context.ReportDiagnostic(Rule, location, arg0, arg1);
context.ReportDiagnostic(Rule, location, arg0, arg1, arg2);
```

### IOperation Extensions

```csharp
operation.UnwrapAllConversions()        // unwrap implicit/explicit conversions
operation.GetOperandName("fallback")    // human-readable name for diagnostics
```

---

## 8. CompilationStart Pattern (Type Resolution)

When you need to resolve types at compile time (e.g., check if `IAsyncDisposable` exists):

```csharp
protected override void RegisterActions(AnalysisContext context) =>
    context.RegisterCompilationStartAction(OnCompilationStart);

private static void OnCompilationStart(CompilationStartAnalysisContext context) {
    if (context.Compilation.GetTypeByMetadataName("System.IAsyncDisposable") is not { } asyncDisposableType) {
        return; // Type not available in this compilation — bail
    }

    context.RegisterSyntaxNodeAction(
        ctx => Analyze(ctx, asyncDisposableType),
        SyntaxKind.Whatever);
}

private static void Analyze(SyntaxNodeAnalysisContext context, INamedTypeSymbol asyncDisposableType) {
    // Use asyncDisposableType for comparisons
}
```

For multiple types, resolve each separately with pattern matching:

```csharp
private static void OnCompilationStart(CompilationStartAnalysisContext context) {
    var taskType = context.Compilation.GetTypeByMetadataName("System.Threading.Tasks.Task");
    var taskOfTType = context.Compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1");

    if (taskType is null && taskOfTType is null) {
        return;
    }

    context.RegisterSyntaxNodeAction(
        ctx => Analyze(ctx, taskType, taskOfTType),
        SyntaxKind.SimpleMemberAccessExpression);
}
```

---

## 9. Shared Utilities

### AsyncContextHelper

Location: `Analyzers/AsyncContextHelper.cs`

```csharp
AsyncContextHelper.IsInsideAsyncContext(SyntaxNode node)
```

Returns `true` if the node is inside an `async` method, lambda, local function, or top-level statement. Use this instead of duplicating the walk-up-parents loop.

AL0104 and AL0105 both use this shared helper. New analyzers must use `AsyncContextHelper` directly — never duplicate the walk-up loop.

### OperationHelper (domain-specific)

```csharp
OperationHelper.IsArgumentNullException(type)
OperationHelper.IsArgumentException(type)
OperationHelper.IsArgumentOutOfRangeException(type)
OperationHelper.IsAnyArgumentException(type)
```

---

## 10. Canonical Test Template

```csharp
using AnalyzerTestBase = ANcpLua.Roslyn.Utilities.Testing.AnalyzerTest<ANcpLua.Analyzers.Analyzers.Al00xxDescriptiveNameAnalyzer>;

namespace ANcpLua.Analyzers.Tests;

public sealed partial class Al00xxDescriptiveNameTests : AnalyzerTestBase {
    [Fact]
    public Task ShouldReportWhenConditionMet() =>
        VerifyAsync("""
                    using System;

                    public class C {
                        public void M() {
                            [|diagnostic span here|];
                        }
                    }
                    """);

    [Fact]
    public Task ShouldNotReportWhenConditionNotMet() =>
        VerifyAsync("""
                    using System;

                    public class C {
                        public void M() {
                            // no markers = no diagnostic expected
                        }
                    }
                    """);
}
```

### Test Conventions

| Element | Convention |
|---------|-----------|
| File | `tests/ANcpLua.Analyzers.Tests/AL00XXDescriptiveNameTests.cs` |
| Class | `Al00xxDescriptiveNameTests` — matches analyzer naming |
| Modifier | Always `sealed partial class` |
| Base | Always `: AnalyzerTestBase` (via using alias) |
| Namespace | `ANcpLua.Analyzers.Tests` |
| Return type | `Task` (via `VerifyAsync`) |
| Diagnostic marker | `[|span|]` marks the exact text where the diagnostic is expected |
| No marker | Negative test — asserts zero diagnostics |
| Stubs | Use `private const string` for repeated external type stubs (see AL0106 `AspNetCoreStubs` pattern) |

### Stub Pattern for External Types

When your analyzer needs types not in netstandard2.0 (ASP.NET Core, etc.):

```csharp
private const string AspNetCoreStubs = """
                                       namespace Microsoft.AspNetCore.Mvc {
                                           public abstract class ControllerBase { }
                                           public abstract class Controller : ControllerBase { }
                                       }
                                       """;

[Fact]
public Task ShouldReportInController() =>
    VerifyAsync($$"""
                  using System;
                  using System.Threading.Tasks;

                  {{AspNetCoreStubs}}

                  public class MyController : Microsoft.AspNetCore.Mvc.Controller {
                      public async Task<int> GetData() {
                          return await [|Task.Run(() => 42)|];
                      }
                  }
                  """);
```

Note the `$$"""` interpolation (double-dollar) when using `{{StubConstant}}` interpolation in raw string literals.

---

## 11. Anti-Duplication Checklist

Before writing a new analyzer, verify:

- [ ] The diagnostic ID `AL00XX` is not already in `AnalyzerReleases.Unshipped.md`
- [ ] No existing analyzer already covers this pattern (search `Analyzers/*.cs`)
- [ ] Async context checking uses `AsyncContextHelper.IsInsideAsyncContext()` — do NOT copy the loop
- [ ] Type comparisons use `.IsEqualTo()` — do NOT use `SymbolEqualityComparer` directly
- [ ] Interface checks use `.Implements()` — do NOT walk `AllInterfaces` manually
- [ ] Base type checks use `.InheritsFrom()` — do NOT walk `BaseType` chain manually
- [ ] Reporting uses `context.ReportDiagnostic(Rule, location, args)` extension — do NOT construct `Diagnostic.Create` unless you need `ImmutableDictionary` properties

---

## 12. File Structure

```
src/ANcpLua.Analyzers/
  AlAnalyzer.cs                        # Base class (CreateRule, RegisterActions)
  Resources.resx                       # Localized strings ({id}AnalyzerTitle/MessageFormat/Description)
  AnalyzerReleases.Unshipped.md        # Release tracking table
  Analyzers/
    AL0001*.cs through AL0106*.cs      # One file per analyzer (or grouped range)
    AsyncContextHelper.cs              # Shared async-context detection

tests/ANcpLua.Analyzers.Tests/
  AL0001*Tests.cs through AL0106*Tests.cs  # One test class per analyzer
```

---

## 13. New Analyzer Checklist (Step-by-Step)

1. **Allocate ID:** Check `AnalyzerReleases.Unshipped.md` for the next available `AL00XX`
2. **Create analyzer file:** `Analyzers/AL00XXDescriptiveNameAnalyzer.cs`
3. **Add resources:** Three entries in `Resources.resx` (`AL00XXAnalyzerTitle`, `AL00XXAnalyzerMessageFormat`, `AL00XXAnalyzerDescription`)
4. **Add release entry:** One line in `AnalyzerReleases.Unshipped.md`
5. **Create test file:** `tests/ANcpLua.Analyzers.Tests/AL00XXDescriptiveNameTests.cs`
6. **Write positive tests:** At least one `[|marker|]` test per code path
7. **Write negative tests:** At least one no-marker test per exclusion condition
8. **Build and run tests:** `dotnet build` + `dotnet test`

---

## 14. Key Dependencies

- `Microsoft.CodeAnalysis.CSharp` (5.0.0) — Roslyn APIs
- `ANcpLua.Roslyn.Utilities.Sources` — Compile-time source-generated helpers (extensions, categories, severities)
- `ANcpLua.Roslyn.Utilities.Testing` — Test infrastructure (`AnalyzerTest<T>`, `VerifyAsync`)
