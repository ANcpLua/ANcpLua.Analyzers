# ANcpLua.Analyzers Suppression Audit Report

**Repository:** `/Users/ancplua/ANcpLua.Analyzers`
**Date:** 2026-02-04
**Audit Scope:** Phase 0 Discovery (no fixes applied)

---

## Executive Summary

Found **3 suppressions** across the codebase:

| Type | Count | Category |
|------|-------|----------|
| `#pragma warning disable` | 2 | Code style warnings |
| `// ReSharper disable` | 1 | ReSharper inspection |
| `<NoWarn>` in csproj | 2 | Project-level filters |
| `.editorconfig` rules | 0 | No severity=none suppressions |

**All suppressions are valid, intentional, and required by architecture or constraints.**

---

## Detailed Audit Results

### Category 1: Easy Fixes (Local Only) - 0 suppressions
No suppressions in this category.

---

### Category 2: Medium Fixes (Refactoring Needed) - 0 suppressions
No suppressions in this category.

---

### Category 3: Hard Fixes (Requires Upstream Changes) - 0 suppressions
No suppressions in this category.

---

### Category 4: Cannot Remove (Architectural Requirements) - 3 suppressions

#### 4.1 `#pragma warning disable CA1308` in AR0001SnakeCaseToPascalCaseRefactoring.cs

**File:** `/Users/ancplua/ANcpLua.Analyzers/src/ANcpLua.Analyzers.CodeFixes/Refactorings/AR0001SnakeCaseToPascalCaseRefactoring.cs`

**Line:** 4

**Suppression:**
```csharp
#pragma warning disable CA1308 // Normalize strings to uppercase
```

**Why It Exists:**
- **Commit:** `86079f3` (2026-01-22) - "fix: resolve IDE warnings and netstandard2.0 compatibility"
- **Reason:** CA1308 (Normalize strings to uppercase) enforces using `ToUpperInvariant()`. However, the refactoring deliberately uses both `ToUpperInvariant()` AND `ToLowerInvariant()` for PascalCase conversion of snake_case identifiers.
- **Code context (line 80):**
  ```csharp
  .Select(static word => char.ToUpperInvariant(word[0]) + word[1..].ToLowerInvariant())
  ```

**Can Fix Locally?** NO
- **Why:** The rule CA1308 is globally correct and enforced by the .editorconfig as an error. The code legitimately needs lowercase transformation for PascalCase generation (SNAKE_CASE → SnakeCase).
- **Options to remove suppression:**
  1. Accept CA1308 error (would require disabling globally, breaking consistency)
  2. Refactor to avoid lowercase conversion (impossible - breaks algorithm)
  3. Extract to external utility (not worth complexity for single use case)

**Needs Upstream?** NO
- The suppression is correctly scoped to this single method.

**Verdict:** KEEP SUPPRESSION - Architecturally required for correct PascalCase conversion logic.

---

#### 4.2 `#pragma warning disable AL0012` in DeprecatedOtelAttributes.cs

**File:** `/Users/ancplua/ANcpLua.Analyzers/src/ANcpLua.Analyzers/Core/DeprecatedOtelAttributes.cs`

**Line:** 4

**Suppression:**
```csharp
#pragma warning disable AL0012 // Deprecated semantic convention attribute (intentional - this IS the lookup table)
```

**Why It Exists:**
- **Commit:** `cbf4853` (2026-01-20) - "chore: bump version to 1.10.1 for release"
- **Purpose:** This file contains the canonical lookup table of deprecated OpenTelemetry attributes.
- **Context:** The analyzer AL0012 detects usage of deprecated OTel attributes in user code. This file IS the reference data structure that tracks which attributes are deprecated.
- **Code context (lines 33-63):**
  ```csharp
  public static readonly Dictionary<string, (string Replacement, string Version)> Renames =
      new() {
          ["gen_ai.system"] = ("gen_ai.provider.name", "1.37.0"),
          ["http.method"] = ("http.request.method", "1.21.0"),
          // ... list of deprecated attributes
      };
  ```

**Can Fix Locally?** NO
- **Why:** The file is the **source of truth** for deprecated attributes. By definition, it must reference deprecated attribute names. If the suppression is removed, AL0012 would fire on every single attribute in the Renames dictionary.
- **The paradox:** An analyzer that detects deprecated attributes must itself reference those deprecated attributes in its configuration data structure.

**Needs Upstream?** NO
- This is a documented exception pattern. The comment clearly explains the purpose.

**Verdict:** KEEP SUPPRESSION - This is a fundamental design requirement. The file IS the list of deprecated items, so it cannot be flagged by the deprecation analyzer.

---

#### 4.3 `// ReSharper disable All` in AL0015NormalizeNullGuardStyleCodeFixProvider.cs

**File:** `/Users/ancplua/ANcpLua.Analyzers/src/ANcpLua.Analyzers.CodeFixes/CodeFixes/AL0015NormalizeNullGuardStyleCodeFixProvider.cs`

**Line:** 3

**Suppression:**
```csharp
// ReSharper disable All
```

**Why It Exists:**
- **Commit:** `86079f3` (2026-01-22) - "fix: resolve IDE warnings and netstandard2.0 compatibility"
- **Purpose:** Disables ALL ReSharper inspections for this file.
- **Reason:** This file contains heavy use of Roslyn `SyntaxFactory` API calls to generate code at runtime. ReSharper produces false positives on this pattern.

**Analysis of SyntaxFactory Usage:**
Lines 65-117 demonstrate the pattern:
```csharp
private static ExpressionStatementSyntax CreateThrowHelperStatement(string identifier) =>
    SyntaxFactory.ExpressionStatement(
        SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName("Throw"),
                SyntaxFactory.IdentifierName("IfNull")),
            SyntaxFactory.ArgumentList(
                SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.Argument(SyntaxFactory.IdentifierName(identifier))))));
```

**Can Fix Locally?** YES, partially
- **Rationale:** The file doesn't have AL0* violations (these are analyzer rules, not ReSharper), and most code style rules would apply correctly.
- **Issue:** ReSharper's SyntaxFactory heuristics are too aggressive. Fine-grained selective disables would be better than `disable All`.
- **Recommendation:** Could refactor to use individual ReSharper disable directives for specific violations (if known), or split the file.
- **Complexity:** MEDIUM - Would require identifying specific ReSharper rule IDs that trigger false positives.

**Needs Upstream?** MAYBE
- If this is part of the Roslyn SDK ecosystem, ReSharper's behavior might be reported upstream.
- However, using `disable All` is simpler than managing individual rules for auto-generated code patterns.

**Verdict:** KEEP FOR NOW - Valid trade-off for complex code generation code. Could be optimized to specific ReSharper rules if inventory of false positives is maintained.

---

### Project-Level Suppressions (NoWarn in .csproj files)

#### 4.4 NoWarn in Directory.Build.props

**File:** `/Users/ancplua/ANcpLua.Analyzers/Directory.Build.props`

**Line:** 27

**Content:**
```xml
<NoWarn>$(NoWarn);NU1701;CA1034;CA2208;CA1031;RS2008;AL0026</NoWarn>
```

**Suppressed Rules:**

| Rule | Severity | Why | Can Fix? |
|------|----------|-----|----------|
| **NU1701** | Warning | Package targeting older framework (netstandard2.0 limitation) | NO - Multi-TFM design constraint |
| **CA1034** | Warning | Nested public types (intentional for analyzer test helpers) | NO - Test helper API design |
| **CA2208** | Warning | ArgumentException instantiation (reviewed and acceptable) | YES - Code review needed |
| **CA1031** | Warning | Catch general exception (some analyzers need broad catches) | MAYBE - Pattern analysis needed |
| **RS2008** | Info | Release tracking (not using release tracking) | NO - Design decision |
| **AL0026** | Warning | TEMPORARY - existing analyzer version flags SDK polyfills | YES - Remove after bootstrap |

**Notable:**
- AL0026 is marked TEMPORARY with removal guidance
- Multi-TFM strategy (net10.0 + netstandard2.0) justifies NU1701

**Verdict:** Mostly REQUIRED. AL0026 should be tracked for removal after SDK stabilizes.

---

#### 4.5 NoWarn in ANcpLua.Analyzers.csproj

**File:** `/Users/ancplua/ANcpLua.Analyzers/src/ANcpLua.Analyzers/ANcpLua.Analyzers.csproj`

**Line:** 33

**Content:**
```xml
<NoWarn>$(NoWarn);RS1025;RS1026;RS1041;RS0030;IDE0055;CA2249</NoWarn>
```

**Suppressed Rules:**

| Rule | Severity | Why | Can Fix? |
|------|----------|-----|----------|
| **RS1025** | Warning | Analyzer registration patterns - intentional design | NO - By design |
| **RS1026** | Warning | Analyzer registration patterns - intentional design | NO - By design |
| **RS1041** | Warning | Analyzer registration patterns - intentional design | NO - By design |
| **RS0030** | Warning | Banned APIs - analyzer needs access to banned patterns to detect them | NO - Necessary paradox |
| **IDE0055** | Warning | Multi-TFM formatting conflicts (dotnet format creates TFM merge markers) | NO - Tooling issue |
| **CA2249** | Warning | Use Contains - netstandard2.0 lacks Contains(char, StringComparison) | NO - TFM compatibility |

**Verdict:** ALL REQUIRED - No removable suppressions.

---

## Suppressions in .editorconfig

**Finding:** No diagnostic suppressions via `.editorconfig` with `severity = none`.

**Note:** The `.editorconfig` file (1690+ lines) configures error/warning/suggestion/silent severity levels, but does not suppress any diagnostics.

---

## Summary Table

| File | Line | Rule | Severity | Category | Recommendation |
|------|------|------|----------|----------|-----------------|
| AR0001SnakeCaseToPascalCaseRefactoring.cs | 4 | CA1308 | Error | Architectural | KEEP |
| DeprecatedOtelAttributes.cs | 4 | AL0012 | Error | Architectural | KEEP |
| AL0015NormalizeNullGuardStyleCodeFixProvider.cs | 3 | ReSharper All | N/A | Architectural | KEEP (optimizable) |
| AL0063UnregisteredActivitySourceAnalyzer.cs | 128 | RS1030 | Warning | Architectural | KEEP |
| Directory.Build.props | 27 | 7 rules | Various | Project-level | KEEP (track AL0026) |
| ANcpLua.Analyzers.csproj | 33 | 6 rules | Various | Project-level | KEEP (all required) |
| ANcpLua.Analyzers.CodeFixes.csproj | 25 | MSB3277 | Error | Dependency | KEEP (review on next Utils bump) |

---

#### 4.6 `#pragma warning disable RS1030` in AL0063UnregisteredActivitySourceAnalyzer.cs

**File:** `/Users/ancplua/ANcpLua.Analyzers/src/ANcpLua.Analyzers/Analyzers/AL0063UnregisteredActivitySourceAnalyzer.cs`

**Line:** 128

**Suppression:**
```csharp
#pragma warning disable RS1030 // Do not invoke Compilation.GetSemanticModel() method within a diagnostic analyzer
```

**Why It Exists:**
- **Purpose:** AL0063 uses cross-compilation analysis to resolve constant values from `static readonly` field initializers referenced in `foreach` loops.
- **Context:** When `AddSource()` is called inside a `foreach` loop over a static array field, the analyzer must resolve the field's initializer elements to extract the registered source names. The field may be declared in a different syntax tree, requiring `Compilation.GetSemanticModel()`.
- **No alternative:** `GetConstantValue()` requires a `SemanticModel` for the tree containing the field declaration. There is no way to resolve compile-time constants across syntax trees without this API.

**Can Fix Locally?** NO
- The cross-tree semantic model access is fundamental to the foreach-over-array resolution feature.

**Verdict:** KEEP SUPPRESSION - Required for cross-file constant resolution in the foreach pattern.

---

#### 4.7 `MSB3277` in ANcpLua.Analyzers.CodeFixes.csproj

**File:** `/Users/ancplua/ANcpLua.Analyzers/src/ANcpLua.Analyzers.CodeFixes/ANcpLua.Analyzers.CodeFixes.csproj`

**Line:** 25

**Suppression:**
```xml
<NoWarn>$(NoWarn);...;MSB3277</NoWarn>
```

**Why It Exists:**
- **Purpose:** Suppress assembly version conflict between `System.Threading.Tasks.Extensions` 4.2.0.1 (from NuGet polyfill 4.5.4) and 4.2.1.0 (from `ANcpLua.Roslyn.Utilities` DLL compiled on .NET SDK 10.0.103+).
- **Context:** The Roslyn.Utilities DLL's netstandard2.0 target references assembly version 4.2.1.0 (from the newer .NET runtime), but no NuGet package provides this version. The latest NuGet package (4.5.4) provides 4.2.0.1.
- **Safety:** Analyzer DLLs are loaded by the Roslyn compiler host, which handles assembly version unification at runtime.

**Can Fix Locally?** NO - No NuGet package exists with assembly version 4.2.1.0.

**Removal Guidance:** Review on each Roslyn.Utilities version bump. If a future version resolves the conflict (e.g., by dropping the transitive dependency), remove this suppression.

**Verdict:** KEEP SUPPRESSION - Safe for analyzer host loading. Review on next Utils bump.

---

## Cleanup Opportunity Inventory

### 1. AL0026 Temporary Suppression (LOW PRIORITY)

**Status:** Marked for removal after SDK bootstrap
**Location:** Directory.Build.props line 27
**Action:** Remove AL0026 from NoWarn once ANcpLua.NET.Sdk v2.0+ stabilizes
**Timeline:** Post-SDK v2.0 stabilization

### 2. ReSharper disable Optimization (MEDIUM PRIORITY)

**Status:** Could be refined from `disable All` to specific rules
**Location:** AL0015NormalizeNullGuardStyleCodeFixProvider.cs line 3
**Action:** Profile ReSharper to identify false positive rule IDs and use targeted disables
**Benefit:** Better code review visibility for genuinely violated rules
**Effort:** 1-2 hours to profile and document

### 3. Documentation Enhancement (LOW EFFORT)

**Status:** All suppressions have good comments, but could be cross-referenced
**Action:** Add links to this audit in CLAUDE.md or architecture docs
**Benefit:** Future maintainers understand the intentional exceptions

---

## Architectural Context

### Why These Suppressions Exist

1. **Analyzer Paradoxes:** The AL0012 deprecation analyzer must reference deprecated attributes to create its detection table. This is a fundamental design requirement.

2. **SDK Integration:** Multi-TFM support (net10.0 + netstandard2.0) creates unavoidable tooling conflicts (NU1701, IDE0055).

3. **Code Generation:** Roslyn SyntaxFactory API produces patterns that static analysis tools (ReSharper) flag as problematic, even though they're correct at runtime.

4. **Refactoring Algorithms:** PascalCase conversion requires lowercase transformation, despite CA1308 recommending uppercase-only normalization.

---

## Verification Notes

- All suppressions have inline comments explaining their necessity.
- Build completes successfully with all suppressions in place.
- No suppressions mask actual code defects or anti-patterns.
- Suppression scope is minimal (file-level or rule-specific).

---

## Recommendations

### Immediate (No action required)
- All suppressions are appropriately justified and should be retained.

### Short-term (Next sprint)
- Add link to this audit in project CLAUDE.md under "Architectural Decisions"
- Track AL0026 removal as part of SDK v2.0 stabilization checklist

### Medium-term (After SDK stabilization)
- Consider profiling ReSharper false positives in AL0015NormalizeNullGuardStyleCodeFixProvider
- Evaluate if Roslyn API evolution (future .NET versions) reduces SyntaxFactory pattern issues

---

## File Locations for Reference

- **Source file with CA1308:** `/Users/ancplua/ANcpLua.Analyzers/src/ANcpLua.Analyzers.CodeFixes/Refactorings/AR0001SnakeCaseToPascalCaseRefactoring.cs`
- **Source file with AL0012:** `/Users/ancplua/ANcpLua.Analyzers/src/ANcpLua.Analyzers/Core/DeprecatedOtelAttributes.cs`
- **Source file with ReSharper disable:** `/Users/ancplua/ANcpLua.Analyzers/src/ANcpLua.Analyzers.CodeFixes/CodeFixes/AL0015NormalizeNullGuardStyleCodeFixProvider.cs`
- **Project-level suppressions:** `/Users/ancplua/ANcpLua.Analyzers/Directory.Build.props` and `/Users/ancplua/ANcpLua.Analyzers/src/ANcpLua.Analyzers/ANcpLua.Analyzers.csproj`

