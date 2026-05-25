# Analyzer ID Renumber Plan (2.0.0 Break)

Source of truth for the renumber: `src/ANcpLua.Analyzers/AnalyzerReleases.Unshipped.md` (89 entries, all active) and the `"AL\d{4}"` literals declared in `src/ANcpLua.Analyzers/Analyzers/AL*.cs` (89 distinct IDs across 77 analyzer files). `AnalyzerReleases.Shipped.md` is **empty** in source — every previously published `AL####` ID has been released only through CI's pack step, not through the shipped release-tracking file.

This document is a planning artifact only. No source files are modified.

The companion repo `Qyl.Opentelemetry.SemanticConventions` performed an analogous `QYL####` renumber on the 3.0.0 break; this plan mirrors its format and depth (see `eng/analyzer-renumber-plan.md` in that repo).

---

## 0. Critical pre-flight finding: sibling packages own slots inside `AL0xxx`

The `AL` prefix is **not** exclusive to `ANcpLua.Analyzers`. Three sibling packages in `~/RiderProjects/ANcpLua.Roslyn.Utilities/` ship analyzers under the same prefix:

| Sibling package                | Owned IDs                          | Source                                                                                                          |
|--------------------------------|------------------------------------|-----------------------------------------------------------------------------------------------------------------|
| `ANcpLua.AotReflection`        | `AL0097`, `AL0098`, `AL0099`, `AL0100` | `ANcpLua.Roslyn.Utilities/src/ANcpLua.AotReflection/DiagnosticDescriptors.cs:6-30`                              |
| `ANcpLua.ExtensibleEnumMirror` | `AL0200`, `AL0201`, `AL0202`       | `ANcpLua.Roslyn.Utilities/src/ANcpLua.ExtensibleEnumMirror/DiagnosticDescriptors.cs:6-22`                       |
| `ANcpLua.DiscriminatedUnion`   | `AL0300`, `AL0301`, `AL0302`, `AL0303` | `ANcpLua.Roslyn.Utilities/src/ANcpLua.DiscriminatedUnion/DiagnosticDescriptors.cs:6-30`                         |

A naïve 100-wide rebanding starting at `AL0100..AL0199`, `AL0200..AL0299`, `AL0300..AL0399` (the obvious Qyl-style mirror) **would collide with all three sibling packages**. To avoid the collision while keeping 100-wide bands, the new ID space starts at `AL1000` and uses bands `AL1000..AL1099`, `AL1100..AL1199`, …, `AL1800..AL1899`. The entire `AL0xxx` range is reserved for: (a) sibling-package use, (b) the legacy orphan resx keys that survive as deletion candidates in section 3 below.

The repo's existing `AnalyzerConventionTests.AllDiagnosticIdsMatchExpectedFormat` enforces `^AL\d{4}$`; the new IDs stay within 4 digits and the convention test continues to pass without modification.

---

## 1. Domain blocks (proposed)

Nine 100-wide bands. The widest active band carries 21 rules, leaving ~79 free slots per band for growth — no future re-renumber required.

| Range            | Domain                                                                                                                            | Rules in band |
|------------------|-----------------------------------------------------------------------------------------------------------------------------------|---------------|
| `AL1000..1099`   | **Correctness / language pitfalls** (primary-ctor reassignment, division-by-zero, span equality, IXmlSerializable, lock keyword, pattern-matching/null-guard normalization) | 13 |
| `AL1100..1199`   | **ASP.NET Core / Aspire / web hosting** (`[FromForm]` shape, HTTP resilience, health checks, connection-string hygiene, service discovery, `Task.Run` in handlers)           | 10 |
| `AL1200..1299`   | **Roslyn Utilities helper API surface** (prefer `IsEqualTo`/`HasAttribute`/`OrEmpty`/extension helpers; `Guard.*` over throw helpers)                                       | 21 |
| `AL1300..1399`   | **Async / threading / reliability** (async-void, lock-target hygiene, await-using, blocking-in-async, SQL interpolation in `CommandText`, fire-and-forget, parse exceptions, response info-leak, LINQ materialization, transaction safety, cancellation propagation, midpoint rounding) | 15 |
| `AL1400..1499`   | **AOT / trim safety** (`[AotTest]/[TrimTest]` shape, `[TrimSafe]/[AotSafe]` call-graph, unnecessary `[AotUnsafe]`, `dynamic`/`Expression.Compile`/`Activator.CreateInstance`/`Type.GetType` in AOT) | 10 |
| `AL1500..1599`   | **Roslyn-author hygiene** (closed-hierarchy switch exhaustiveness, no-`ISymbol`-in-models, `IIncrementalGenerator`, no `NormalizeWhitespace` in source generators, `[DuckDbTable]` partial-ness + ordinal collisions) | 6 |
| `AL1600..1699`   | **Package / version management / doc alignment** (hardcoded versions, `Version.props` import, undefined version variables, AL-vs-docs alignment, outdated MAF package version) | 7 |
| `AL1700..1799`   | **Style** (static lambdas, `TimeProvider` over `DateTime`, `System.Text.Json` over Newtonsoft, implicit-when-apparent `var`)                                                  | 4 |
| `AL1800..1899`   | **Agent / tool governance (Loom)** (`[LoomTool]` destructive-action approval, side-effect annotation, capability annotation)                                                  | 3 |

Reservations not used: `AL1900..AL9999` left empty for future bands (e.g., security/PII, observability if any OTel-shape rule lands here in future, MAF-specific, etc.). The legacy `AL0xxx` range is reserved for sibling-package use and is **not** to be reissued by `ANcpLua.Analyzers`.

Within each band, new IDs are assigned by ascending old ID — deterministic and easy to verify mechanically.

---

## 2. Old to New ID mapping

89 active rules. Each row's title text is sourced from `Resources.resx`'s `<data name="AL####AnalyzerTitle">` value, with the 4 inline-title analyzers (AL0014, AL0015, AL0016, AL0103) sourced from each analyzer's `new DiagnosticDescriptor(...)` literal in source.

| Old ID  | New ID  | Title                                                                              | Domain band              | Has analyzer file? | Has resx key? |
|---------|---------|------------------------------------------------------------------------------------|--------------------------|--------------------|---------------|
| AL0001  | AL1000  | Prohibit reassignment of primary constructor parameters                            | Correctness              | yes (`AL0001ProhibitPrimaryConstructorParameterReassignmentAnalyzer.cs`)         | yes |
| AL0002  | AL1001  | Don't repeat negated patterns                                                      | Correctness              | yes (`AL0002DontRepeatNegatedPatternAnalyzer.cs`)                                 | yes |
| AL0003  | AL1002  | Don't divide by constant zero                                                      | Correctness              | yes (`AL0003DontDivideByConstantZeroAnalyzer.cs`)                                 | yes |
| AL0004  | AL1003  | Use pattern matching when comparing Span with constants                            | Correctness              | yes (`AL0004ToAL0005SpanComparisonAnalyzer.cs`)                                   | yes |
| AL0005  | AL1004  | Use SequenceEqual when comparing Span with non-constants                           | Correctness              | yes (`AL0004ToAL0005SpanComparisonAnalyzer.cs`)                                   | yes |
| AL0006  | AL1005  | Field name conflicts with primary constructor parameter                            | Correctness              | yes (`AL0006FieldNameConflictWithPrimaryConstructorAnalyzer.cs`)                  | yes |
| AL0007  | AL1006  | GetSchema should be explicitly implemented                                         | Correctness              | yes (`AL0007ToAL0009IXmlSerializableAnalyzer.cs`)                                 | yes |
| AL0008  | AL1007  | GetSchema must return null and not be abstract                                     | Correctness              | yes (`AL0007ToAL0009IXmlSerializableAnalyzer.cs`)                                 | yes |
| AL0009  | AL1008  | Don't call IXmlSerializable.GetSchema                                              | Correctness              | yes (`AL0007ToAL0009IXmlSerializableAnalyzer.cs`)                                 | yes |
| AL0011  | AL1009  | Avoid lock keyword on non-Lock types                                               | Correctness              | yes (`AL0011LockKeywordAnalyzer.cs`)                                              | yes |
| AL0014  | AL1010  | Prefer pattern matching for null and zero comparisons                              | Correctness              | yes (`AL0014PreferPatternMatchingAnalyzer.cs`)                                    | **no — inline title** |
| AL0015  | AL1011  | Normalize null-guard style                                                         | Correctness              | yes (`AL0015NormalizeNullGuardStyleAnalyzer.cs`)                                  | **no — inline title** |
| AL0016  | AL1012  | Combine declaration with subsequent null-check                                     | Correctness              | yes (`AL0016CombineDeclarationWithNullCheckAnalyzer.cs`)                          | **no — inline title** |
| AL0020  | AL1100  | IFormCollection requires explicit attribute                                        | ASP.NET Core             | yes (`AL0020ToAL0024FormBindingAnalyzer.cs`)                                      | yes |
| AL0021  | AL1101  | Multiple structured form sources                                                   | ASP.NET Core             | yes (`AL0020ToAL0024FormBindingAnalyzer.cs`)                                      | yes |
| AL0022  | AL1102  | Mixed form collection and DTO                                                      | ASP.NET Core             | yes (`AL0020ToAL0024FormBindingAnalyzer.cs`)                                      | yes |
| AL0023  | AL1103  | Unsupported form type                                                              | ASP.NET Core             | yes (`AL0020ToAL0024FormBindingAnalyzer.cs`)                                      | yes |
| AL0024  | AL1104  | Form and body conflict                                                             | ASP.NET Core             | yes (`AL0020ToAL0024FormBindingAnalyzer.cs`)                                      | yes |
| AL0080  | AL1105  | Missing resilience configuration                                                   | ASP.NET Core             | yes (`AL0080MissingResilienceConfigurationAnalyzer.cs`)                           | yes |
| AL0081  | AL1106  | Missing health checks                                                              | ASP.NET Core             | yes (`AL0081MissingHealthChecksAnalyzer.cs`)                                      | yes |
| AL0082  | AL1107  | Consider using configuration for connection string                                 | ASP.NET Core             | yes (`AL0082ConsiderConnectionStringAnalyzer.cs`)                                 | yes |
| AL0084  | AL1108  | Missing service discovery                                                          | ASP.NET Core             | yes (`AL0084MissingServiceDiscoveryAnalyzer.cs`)                                  | yes |
| AL0106  | AL1109  | Avoid Task.Run in ASP.NET Core request handlers                                    | ASP.NET Core             | yes (`AL0106AvoidTaskRunInAspNetCoreAnalyzer.cs`)                                 | yes |
| AL0028  | AL1200  | Use IsEqualTo extension                                                            | Roslyn Utilities         | yes (`AL0028UseIsEqualToAnalyzer.cs`)                                             | yes |
| AL0029  | AL1201  | Use HasAttribute extension                                                         | Roslyn Utilities         | yes (`AL0029UseHasAttributeAnalyzer.cs`)                                          | yes |
| AL0030  | AL1202  | Use type hierarchy extension                                                       | Roslyn Utilities         | yes (`AL0030UseTypeHierarchyAnalyzer.cs`)                                         | yes |
| AL0031  | AL1203  | Use operation extension                                                            | Roslyn Utilities         | yes (`AL0031UseOperationExtensionsAnalyzer.cs`)                                   | yes |
| AL0032  | AL1204  | Use OrEmpty extension                                                              | Roslyn Utilities         | yes (`AL0032UseOrEmptyAnalyzer.cs`)                                               | yes |
| AL0033  | AL1205  | Use ToImmutableArrayOrEmpty extension                                              | Roslyn Utilities         | yes (`AL0033UseToImmutableArrayOrEmptyAnalyzer.cs`)                               | yes |
| AL0034  | AL1206  | Use WhereNotNull extension                                                         | Roslyn Utilities         | yes (`AL0034UseWhereNotNullAnalyzer.cs`)                                          | yes |
| AL0035  | AL1207  | Use symbol display string extension                                                | Roslyn Utilities         | yes (`AL0035UseToDisplayStringExtensionsAnalyzer.cs`)                             | yes |
| AL0036  | AL1208  | Use null-guard helper                                                              | Roslyn Utilities         | yes (`AL0036UseGuardNotNullAnalyzer.cs`)                                          | yes |
| AL0037  | AL1209  | Use TryParse extension                                                             | Roslyn Utilities         | yes (`AL0037UseTryParseExtensionsAnalyzer.cs`)                                    | yes |
| AL0039  | AL1210  | Use StringComparison extension                                                     | Roslyn Utilities         | yes (`AL0039UseStringComparisonExtensionsAnalyzer.cs`)                            | yes |
| AL0040  | AL1211  | Use attribute argument extraction extension                                        | Roslyn Utilities         | yes (`AL0040UseAttributeExtensionsAnalyzer.cs`)                                   | yes |
| AL0045  | AL1212  | Use null-or-empty guard helper                                                     | Roslyn Utilities         | yes (`AL0045UseGuardNotNullOrEmptyAnalyzer.cs`)                                   | yes |
| AL0046  | AL1213  | Use null-or-whitespace guard helper                                                | Roslyn Utilities         | yes (`AL0046UseGuardNotNullOrWhiteSpaceAnalyzer.cs`)                              | yes |
| AL0047  | AL1214  | Use zero-guard helper                                                              | Roslyn Utilities         | yes (`AL0047UseGuardNotZeroAnalyzer.cs`)                                          | yes |
| AL0048  | AL1215  | Use non-negative guard helper                                                      | Roslyn Utilities         | yes (`AL0048UseGuardNotNegativeAnalyzer.cs`)                                      | yes |
| AL0049  | AL1216  | Use positive-guard helper                                                          | Roslyn Utilities         | yes (`AL0049UseGuardPositiveAnalyzer.cs`)                                         | yes |
| AL0050  | AL1217  | Use empty-guid guard helper                                                        | Roslyn Utilities         | yes (`AL0050UseGuardNotEmptyGuidAnalyzer.cs`)                                     | yes |
| AL0051  | AL1218  | Use defined-enum guard helper                                                      | Roslyn Utilities         | yes (`AL0051UseGuardDefinedEnumAnalyzer.cs`)                                      | yes |
| AL0125  | AL1219  | Use *Any* string comparison extension                                              | Roslyn Utilities         | yes (`AL0125UseStringComparisonAnyExtensionsAnalyzer.cs`)                         | yes |
| AL0137  | AL1220  | Use Guard.* helpers instead of throw helpers                                       | Roslyn Utilities         | yes (`AL0137UseGuardForThrowIfAnalyzer.cs`)                                       | yes |
| AL0057  | AL1300  | Avoid async void methods                                                           | Async / reliability      | yes (`AL0057ToAL0060ThreadingAnalyzer.cs`)                                        | yes |
| AL0058  | AL1301  | Avoid lock on 'this'                                                               | Async / reliability      | yes (`AL0057ToAL0060ThreadingAnalyzer.cs`)                                        | yes |
| AL0059  | AL1302  | Avoid lock on typeof(T)                                                            | Async / reliability      | yes (`AL0057ToAL0060ThreadingAnalyzer.cs`)                                        | yes |
| AL0060  | AL1303  | Avoid lock on string                                                               | Async / reliability      | yes (`AL0057ToAL0060ThreadingAnalyzer.cs`)                                        | yes |
| AL0104  | AL1304  | Prefer 'await using' for IAsyncDisposable                                          | Async / reliability      | yes (`AL0104PreferAwaitUsingAnalyzer.cs`)                                         | yes |
| AL0105  | AL1305  | Avoid blocking calls in async methods                                              | Async / reliability      | yes (`AL0105AvoidBlockingCallsInAsyncAnalyzer.cs`)                                | yes |
| AL0111  | AL1306  | Avoid SQL string interpolation in CommandText                                      | Async / reliability      | yes (`AL0111SqlInterpolationInCommandTextAnalyzer.cs`)                            | yes |
| AL0112  | AL1307  | Avoid fire-and-forget task discard                                                 | Async / reliability      | yes (`AL0112FireAndForgetTaskAnalyzer.cs`)                                        | yes |
| AL0114  | AL1308  | Prefer TryParse over Parse                                                         | Async / reliability      | yes (`AL0114PreferTryParseAnalyzer.cs`)                                           | yes |
| AL0115  | AL1309  | Empty catch block swallows exceptions                                              | Async / reliability      | yes (`AL0115EmptyCatchBlockAnalyzer.cs`)                                          | yes |
| AL0116  | AL1310  | Exception details leaked in HTTP response                                          | Async / reliability      | yes (`AL0116ExceptionLeakedInResponseAnalyzer.cs`)                                | yes |
| AL0117  | AL1311  | Unnecessary LINQ materialization                                                   | Async / reliability      | yes (`AL0117UnnecessaryLinqMaterializationAnalyzer.cs`)                           | yes |
| AL0118  | AL1312  | Read-modify-write without transaction                                              | Async / reliability      | yes (`AL0118ReadModifyWriteWithoutTransactionAnalyzer.cs`)                        | yes |
| AL0126  | AL1313  | Forward CancellationToken to invocations that support it                           | Async / reliability      | yes (`AL0126CancellationTokenPropagationAnalyzer.cs`)                             | yes |
| AL0138  | AL1314  | Use Math.Round/MathF.Round overload with explicit MidpointRounding                 | Async / reliability      | yes (`AL0138UseExplicitMidpointRoundingAnalyzer.cs`)                              | yes |
| AL0041  | AL1400  | Method with [AotTest] or [TrimTest] must return int                                | AOT / trim               | yes (`AL0041AotTestMustReturnIntAnalyzer.cs`)                                     | yes |
| AL0042  | AL1401  | [AotTest]/[TrimTest] method should return 100 on success                           | AOT / trim               | yes (`AL0042AotTestExitCode100Analyzer.cs`)                                       | yes |
| AL0043  | AL1402  | [TrimSafe] code must not call methods with [RequiresUnreferencedCode]              | AOT / trim               | yes (`AL0043TrimSafeViolationAnalyzer.cs`)                                        | yes |
| AL0044  | AL1403  | [AotSafe] code must not call methods with [RequiresDynamicCode]                    | AOT / trim               | yes (`AL0044AotSafeViolationAnalyzer.cs`)                                         | yes |
| AL0052  | AL1404  | [AotSafe] code must not call [AotUnsafe] code                                      | AOT / trim               | yes (`AL0052AotSafeCallsAotUnsafeAnalyzer.cs`)                                    | yes |
| AL0053  | AL1405  | Unnecessary [AotUnsafe] attribute                                                  | AOT / trim               | yes (`AL0053UnnecessaryAotUnsafeAnalyzer.cs`)                                     | yes |
| AL0094  | AL1406  | Avoid 'dynamic' keyword in AOT-published code                                      | AOT / trim               | yes (`AL0094AvoidDynamicKeywordAnalyzer.cs`)                                      | yes |
| AL0095  | AL1407  | Avoid Expression.Compile() in AOT context                                          | AOT / trim               | yes (`AL0095AvoidExpressionCompileAnalyzer.cs`)                                   | yes |
| AL0101  | AL1408  | Activator.CreateInstance is not AOT-safe                                           | AOT / trim               | yes (`AL0101AvoidActivatorCreateInstanceAnalyzer.cs`)                             | yes |
| AL0102  | AL1409  | Type.GetType with dynamic name is not AOT-safe                                     | AOT / trim               | yes (`AL0102AvoidTypeGetTypeAnalyzer.cs`)                                         | yes |
| AL0103  | AL1500  | Closed hierarchy match is not exhaustive                                           | Roslyn-author hygiene    | yes (`AL0103ClosedTypeHierarchySwitchAnalyzer.cs`)                                | **no — inline title** |
| AL0119  | AL1501  | Avoid storing ISymbol in source generator models                                   | Roslyn-author hygiene    | yes (`AL0119SymbolStoredInModelAnalyzer.cs`)                                      | yes |
| AL0120  | AL1502  | Use IIncrementalGenerator instead of ISourceGenerator                              | Roslyn-author hygiene    | yes (`AL0120UseIncrementalGeneratorAnalyzer.cs`)                                  | yes |
| AL0121  | AL1503  | Avoid NormalizeWhitespace in source generators                                     | Roslyn-author hygiene    | yes (`AL0121NormalizeWhitespaceAnalyzer.cs`)                                      | yes |
| AL0122  | AL1504  | [DuckDbTable] type must be partial                                                 | Roslyn-author hygiene    | yes (`AL0122DuckDbTableMustBePartialAnalyzer.cs`)                                 | yes |
| AL0123  | AL1505  | Conflicting [DuckDbColumn] ordinal values                                          | Roslyn-author hygiene    | yes (`AL0123DuckDbColumnConflictingOrdinalAnalyzer.cs`)                           | yes |
| AL0017  | AL1600  | Hardcoded package version detected                                                 | Package / version        | yes (`AL0017HardcodedPackageVersionAnalyzer.cs`)                                  | yes |
| AL0018  | AL1601  | Version.props not imported                                                         | Package / version        | yes (`AL0018VersionPropsNotImportedAnalyzer.cs`)                                  | yes |
| AL0019  | AL1602  | Undefined version variable                                                         | Package / version        | yes (`AL0019UndefinedVersionVariableAnalyzer.cs`)                                 | yes |
| AL0054  | AL1603  | Diagnostic missing from documentation                                              | Package / version        | yes (`AL0054ToAL0056DiagnosticsAlignmentAnalyzer.cs`)                             | yes |
| AL0055  | AL1604  | Diagnostic missing from release notes                                              | Package / version        | yes (`AL0054ToAL0056DiagnosticsAlignmentAnalyzer.cs`)                             | yes |
| AL0056  | AL1605  | Diagnostic documentation mismatch                                                  | Package / version        | yes (`AL0054ToAL0056DiagnosticsAlignmentAnalyzer.cs`)                             | yes |
| AL0127  | AL1606  | Outdated MAF ecosystem package version                                             | Package / version        | yes (`AL0127OutdatedMafPackageVersionAnalyzer.cs`)                                | yes |
| AL0025  | AL1700  | Anonymous function can be made static                                              | Style                    | yes (`AL0025PreferStaticLambdaAnalyzer.cs`)                                       | yes |
| AL0026  | AL1701  | Avoid DateTime/DateTimeOffset time accessors                                       | Style                    | yes (`AL0026AvoidDateTimeNowAnalyzer.cs`)                                         | yes |
| AL0027  | AL1702  | Avoid legacy JSON library                                                          | Style                    | yes (`AL0027AvoidNewtonsoftJsonAnalyzer.cs`)                                      | yes |
| AL0139  | AL1703  | Use implicit type when type is apparent                                            | Style                    | yes (`AL0139UseImplicitTypeWhenApparentAnalyzer.cs`)                              | yes |
| AL0128  | AL1800  | Destructive Loom tool must require approval                                        | Agent governance         | yes (`AL0128DestructiveToolMustRequireApprovalAnalyzer.cs`)                       | yes |
| AL0129  | AL1801  | Loom tool should declare its side effect                                           | Agent governance         | yes (`AL0129ToolMustDeclareSideEffectAnalyzer.cs`)                                | yes |
| AL0130  | AL1802  | Loom tool should declare required capabilities                                     | Agent governance         | yes (`AL0130ToolMustDeclareCapabilityAnalyzer.cs`)                                | yes |

---

## 3. Dead-ID list, orphan resx keys, and pre-existing bugs

### 3.1 Dead analyzer IDs

**None.** Every `AL####` literal in `src/ANcpLua.Analyzers/Analyzers/AL*.cs` is also declared in `AnalyzerReleases.Unshipped.md`, and vice versa. `AnalyzerReleases.Shipped.md` is empty, so there are no orphaned shipped entries to delete. The set of active IDs in source is exactly the 89 rules listed in section 2.

### 3.2 Orphan resx keys (delete on renumber)

`src/ANcpLua.Analyzers/Resources.resx` contains 390 `<data name="AL####...">` entries spanning numeric IDs `AL0001..AL0136`. Cross-referencing against the active analyzer literals reveals **44 numeric IDs that have no live analyzer in this repo**:

```
AL0010 AL0012 AL0013
AL0061 AL0062 AL0063 AL0064 AL0065 AL0066 AL0067 AL0068 AL0069 AL0070
AL0071 AL0072 AL0073 AL0074 AL0075 AL0076 AL0077 AL0078 AL0079
AL0083 AL0085 AL0086 AL0087 AL0088 AL0089 AL0090 AL0091 AL0092 AL0093
AL0096 AL0107 AL0108 AL0109 AL0110 AL0113 AL0124
AL0131 AL0132 AL0133 AL0134 AL0135 AL0136
```

Every one of these matches an OpenTelemetry semantic-conventions rule that **lives in the `Qyl.Opentelemetry.SemanticConventions` repo today** (its `QYL####` analyzers were forked from this `AL####` namespace; see Qyl's `eng/analyzer-renumber-plan.md` section 4). The resx entries in `ANcpLua.Analyzers/Resources.resx` are the original upstream keys that were left behind when the OTel analyzers moved out. They have no associated analyzer code in this repo, no consumer-visible behavior, and the dogfood `.editorconfig` references for `AL0010/AL0012/AL0013` (lines 1045-1055) point at rules that **do not exist** in this assembly.

**Recommendation for 2.0.0 break:** delete the 44 orphan resx prefix-groups (≈132 keys: title + message + description per ID) and regenerate `Resources.Designer.cs`. Same for `CodeFixResources.resx` orphan entries enumerated in 3.3 below.

### 3.3 Orphan CodeFixResources keys (delete on renumber)

`src/ANcpLua.Analyzers.CodeFixes/CodeFixResources.resx` has 50 keys. Cross-referenced against the 37 active code-fix files plus their analyzer IDs, the following keys are orphaned (their numeric ID has no live analyzer here — same Qyl-fork story as 3.2):

```
AL0010CodeFixTitle
AL0012CodeFixTitle
AL0071CodeFixTitle
AL0072CodeFixTitle
AL0073CodeFixTitle
AL0074CodeFixTitle
AL0107CodeFixTitle
AL0108CodeFixTitle
AL0109CodeFixTitle
AL0110CodeFixTitle
AL0124CodeFixTitle
AL0134CodeFixTitle
AL0135CodeFixTitle
```

13 orphan code-fix titles to delete. The remaining 37 keys all map to active code-fix providers and need the `AL{old}` → `AL{new}` prefix rename (section 4 below).

### 3.4 Stale `.editorconfig` references in this repo

`.editorconfig` lines 1045-1064 set severities for `AL0010`, `AL0012`, `AL0013` — none of which exist as active diagnostics in this repo. These are dead config that should be deleted as part of the same PR.

### 3.5 Stale build/.props comment references

`src/ANcpLua.Analyzers/build/ANcpLua.Analyzers.props:6` and `src/ANcpLua.Analyzers/AotContext.cs:7` mention `AL0096` in comments as an "AOT-only rule". `AL0096` exists only as orphaned resx text and has no analyzer. Update both comments to remove `AL0096` once the orphan keys are deleted.

### 3.6 Pre-existing base-class bug? — **NO**

Unlike the Qyl repo (which had a resx-prefix bug because Qyl's `Al0061…` resx keys did not match its live `QYL####` diagnostic IDs), this repo's `AlAnalyzer.CreateRule` interpolation works correctly:

```csharp
// AlAnalyzer.cs:36-42
new LocalizableResourceString($"{id}AnalyzerTitle",        Resources.ResourceManager, ...)
new LocalizableResourceString($"{id}AnalyzerMessageFormat", Resources.ResourceManager, ...)
new LocalizableResourceString($"{id}AnalyzerDescription",  Resources.ResourceManager, ...)
```

For every active rule whose DiagnosticId is `"AL####"`, the resx contains `AL####AnalyzerTitle/MessageFormat/Description`. The base class and resx are aligned today. After the renumber, both must move together (section 4) — but the interpolation pattern itself is sound. Nothing to fix in the base class.

---

## 4. Resource-key rename rule

The 85 active rules that go through `AlAnalyzer.CreateRule` (i.e., all 89 minus the 4 inline-title rules `AL0014`, `AL0015`, `AL0016`, `AL0103`) need their resx keys renamed in lockstep with the diagnostic-ID change.

### Per-suffix rename pattern

```
AL{old}AnalyzerTitle           -> AL{new}AnalyzerTitle
AL{old}AnalyzerMessageFormat   -> AL{new}AnalyzerMessageFormat
AL{old}AnalyzerDescription     -> AL{new}AnalyzerDescription
AL{old}CodeFixTitle            -> AL{new}CodeFixTitle
AL{old}{Variant}CodeFixTitle   -> AL{new}{Variant}CodeFixTitle   (e.g. AL0030ImplementsCodeFixTitle, AL0031CodeFixTitleTryGetConstantValue)
```

### Concrete resx renames (Resources.resx + CodeFixResources.resx)

Driven directly by the section-2 mapping. 85 prefix groups in `Resources.resx`, 37 prefix groups in `CodeFixResources.resx` (the 37 IDs whose analyzer has a paired code-fix provider).

| Old resx prefix | New resx prefix | Notes                                                                                       |
|-----------------|-----------------|---------------------------------------------------------------------------------------------|
| AL0001          | AL1000          |                                                                                             |
| AL0002          | AL1001          | also `AL0002CodeFixTitle` -> `AL1001CodeFixTitle`                                           |
| AL0003          | AL1002          |                                                                                             |
| AL0004          | AL1003          | also `AL0004CodeFixTitle` -> `AL1003CodeFixTitle`                                           |
| AL0005          | AL1004          | also `AL0005CodeFixTitle` -> `AL1004CodeFixTitle`                                           |
| AL0006          | AL1005          |                                                                                             |
| AL0007          | AL1006          |                                                                                             |
| AL0008          | AL1007          | also `AL0008CodeFixTitle` -> `AL1007CodeFixTitle`                                           |
| AL0009          | AL1008          |                                                                                             |
| AL0011          | AL1009          | also `AL0011CodeFixTitle` -> `AL1009CodeFixTitle`                                           |
| AL0017          | AL1600          |                                                                                             |
| AL0018          | AL1601          |                                                                                             |
| AL0019          | AL1602          |                                                                                             |
| AL0020          | AL1100          |                                                                                             |
| AL0021          | AL1101          |                                                                                             |
| AL0022          | AL1102          |                                                                                             |
| AL0023          | AL1103          |                                                                                             |
| AL0024          | AL1104          |                                                                                             |
| AL0025          | AL1700          | also `AL0025CodeFixTitle` -> `AL1700CodeFixTitle`                                           |
| AL0026          | AL1701          | also `AL0026CodeFixTitle` -> `AL1701CodeFixTitle`                                           |
| AL0027          | AL1702          | also `AL0027CodeFixTitle` -> `AL1702CodeFixTitle`                                           |
| AL0028          | AL1200          | also `AL0028CodeFixTitle` -> `AL1200CodeFixTitle`                                           |
| AL0029          | AL1201          | also `AL0029CodeFixTitle` -> `AL1201CodeFixTitle`                                           |
| AL0030          | AL1202          | also `AL0030ImplementsCodeFixTitle` -> `AL1202ImplementsCodeFixTitle`, `AL0030InheritsFromCodeFixTitle` -> `AL1202InheritsFromCodeFixTitle` |
| AL0031          | AL1203          | also `AL0031CodeFixTitle` -> `AL1203CodeFixTitle`, `AL0031CodeFixTitleTryGetConstantValue` -> `AL1203CodeFixTitleTryGetConstantValue` |
| AL0032          | AL1204          | also `AL0032CodeFixTitle` -> `AL1204CodeFixTitle`                                           |
| AL0033          | AL1205          | also `AL0033CodeFixTitle` -> `AL1205CodeFixTitle`                                           |
| AL0034          | AL1206          | also `AL0034CodeFixTitle` -> `AL1206CodeFixTitle`                                           |
| AL0035          | AL1207          | also `AL0035CodeFixTitle` -> `AL1207CodeFixTitle`                                           |
| AL0036          | AL1208          | also `AL0036CodeFixTitle` -> `AL1208CodeFixTitle`                                           |
| AL0037          | AL1209          | also `AL0037CodeFixTitle` -> `AL1209CodeFixTitle`                                           |
| AL0039          | AL1210          | also `AL0039CodeFixTitle` -> `AL1210CodeFixTitle`                                           |
| AL0040          | AL1211          | also `AL0040CodeFixTitle` -> `AL1211CodeFixTitle`                                           |
| AL0041          | AL1400          |                                                                                             |
| AL0042          | AL1401          |                                                                                             |
| AL0043          | AL1402          |                                                                                             |
| AL0044          | AL1403          |                                                                                             |
| AL0045          | AL1212          | also `AL0045CodeFixTitle` -> `AL1212CodeFixTitle`                                           |
| AL0046          | AL1213          | also `AL0046CodeFixTitle` -> `AL1213CodeFixTitle`                                           |
| AL0047          | AL1214          | also `AL0047CodeFixTitle` -> `AL1214CodeFixTitle`                                           |
| AL0048          | AL1215          | also `AL0048CodeFixTitle` -> `AL1215CodeFixTitle`                                           |
| AL0049          | AL1216          | also `AL0049CodeFixTitle` -> `AL1216CodeFixTitle`                                           |
| AL0050          | AL1217          | also `AL0050CodeFixTitle` -> `AL1217CodeFixTitle`                                           |
| AL0051          | AL1218          | also `AL0051CodeFixTitle` -> `AL1218CodeFixTitle`                                           |
| AL0052          | AL1404          |                                                                                             |
| AL0053          | AL1405          |                                                                                             |
| AL0054          | AL1603          |                                                                                             |
| AL0055          | AL1604          |                                                                                             |
| AL0056          | AL1605          |                                                                                             |
| AL0057          | AL1300          |                                                                                             |
| AL0058          | AL1301          |                                                                                             |
| AL0059          | AL1302          |                                                                                             |
| AL0060          | AL1303          |                                                                                             |
| AL0080          | AL1105          |                                                                                             |
| AL0081          | AL1106          |                                                                                             |
| AL0082          | AL1107          |                                                                                             |
| AL0084          | AL1108          |                                                                                             |
| AL0094          | AL1406          |                                                                                             |
| AL0095          | AL1407          |                                                                                             |
| AL0101          | AL1408          |                                                                                             |
| AL0102          | AL1409          |                                                                                             |
| AL0104          | AL1304          |                                                                                             |
| AL0105          | AL1305          |                                                                                             |
| AL0106          | AL1109          |                                                                                             |
| AL0111          | AL1306          |                                                                                             |
| AL0112          | AL1307          |                                                                                             |
| AL0114          | AL1308          |                                                                                             |
| AL0115          | AL1309          |                                                                                             |
| AL0116          | AL1310          |                                                                                             |
| AL0117          | AL1311          |                                                                                             |
| AL0118          | AL1312          |                                                                                             |
| AL0119          | AL1501          |                                                                                             |
| AL0120          | AL1502          |                                                                                             |
| AL0121          | AL1503          | also `AL0121CodeFixTitle` -> `AL1503CodeFixTitle`                                           |
| AL0122          | AL1504          | also `AL0122CodeFixTitle` -> `AL1504CodeFixTitle`                                           |
| AL0123          | AL1505          |                                                                                             |
| AL0125          | AL1219          |                                                                                             |
| AL0126          | AL1313          | also `AL0126CodeFixTitle` -> `AL1313CodeFixTitle`                                           |
| AL0127          | AL1606          |                                                                                             |
| AL0128          | AL1800          |                                                                                             |
| AL0129          | AL1801          |                                                                                             |
| AL0130          | AL1802          |                                                                                             |
| AL0137          | AL1220          | also `AL0137CodeFixTitle` -> `AL1220CodeFixTitle`                                           |
| AL0138          | AL1314          | also `AL0138CodeFixTitle` -> `AL1314CodeFixTitle`                                           |
| AL0139          | AL1703          | (codefix has no resx key; uses inline title)                                                |

85 unique prefix groups in `Resources.resx`. 37 unique prefix groups in `CodeFixResources.resx` (the rows marked "also AL####CodeFixTitle" plus AL0103 — note `AL0103CodeFixTitle` exists despite the analyzer using inline-title; the codefix provider does have a resx-driven title).

### Special-case keys

These keys carry suffix variants beyond the simple `Title`/`MessageFormat`/`Description`/`CodeFixTitle` pattern. The renumber must preserve the variant suffix:

| Old key                                       | New key                                       |
|-----------------------------------------------|-----------------------------------------------|
| `AL0030ImplementsCodeFixTitle`                | `AL1202ImplementsCodeFixTitle`                |
| `AL0030InheritsFromCodeFixTitle`              | `AL1202InheritsFromCodeFixTitle`              |
| `AL0031CodeFixTitleTryGetConstantValue`       | `AL1203CodeFixTitleTryGetConstantValue`       |

Both Designer files (`Resources.Designer.cs`, `CodeFixResources.Designer.cs`) must be regenerated by `dotnet msbuild /t:CoreResGen` after the resx edits — do not hand-edit.

---

## 5. Sanity checks

- **Old distinct active IDs:** 89  (verified by `grep -rEoh '"AL[0-9]{4}"' src/ANcpLua.Analyzers/Analyzers/ | sort -u | wc -l`)
- **Active IDs in `AnalyzerReleases.Unshipped.md`:** 89  (matches; `AnalyzerReleases.Shipped.md` is empty)
- **New distinct IDs assigned:** 89  (matches active count)
- **New ID range used:** `AL1000..AL1012`, `AL1100..AL1109`, `AL1200..AL1220`, `AL1300..AL1314`, `AL1400..AL1409`, `AL1500..AL1505`, `AL1600..AL1606`, `AL1700..AL1703`, `AL1800..AL1802`  (9 contiguous segments across 9 bands, max density 21/100 in the Roslyn-Utilities band)
- **Resx prefix groups renamed in `Resources.resx`:** 85  (4 inline-title rules — AL0014, AL0015, AL0016, AL0103 — own zero resx entries today; they continue to use inline strings)
- **Resx prefix groups renamed in `CodeFixResources.resx`:** 37  (the active code-fix providers, including AL0103)
- **Orphan resx prefix groups deleted from `Resources.resx`:** 44  (≈132 keys total, listed in §3.2)
- **Orphan keys deleted from `CodeFixResources.resx`:** 13  (listed in §3.3)
- **Stale `.editorconfig` lines deleted:** lines 1045-1064 (`AL0010`, `AL0012`, `AL0013` blocks)
- **Stale comment cleanups:** 4 spots mentioning the non-existent `AL0096` (`AotContext.cs:7`, `build/ANcpLua.Analyzers.props:6`, `tests/ANcpLua.Analyzers.Tests/AL0094…Tests.cs:12`, `tests/ANcpLua.Analyzers.Tests/AL0095…Tests.cs:13` — the tests just mention "same pattern as AL0096" in `<remarks>`; safe to delete or replace with "same pattern as AL1406").

### Files to touch (mechanical)

1. **`src/ANcpLua.Analyzers/Analyzers/AL*.cs`** (77 files) — rewrite every `"AL\d{4}"` literal and every `class Al####...Analyzer` class-name prefix. The class name prefix `Al####` is enforced by `AnalyzerConventionTests.AllAnalyzersFollowNamingConvention` at line 20 (regex `^Al\d{4}.*Analyzer$`). The analyzer base class `AlAnalyzer.cs` does **not** need a code change — its `CreateRule(id, ...)` interpolation pattern works for any AL prefix length up to 4 digits.

   **File renames recommended for searchability:** rename each analyzer file from `AL{old}…Analyzer.cs` to `AL{new}…Analyzer.cs`. This keeps `grep AL{new}` finding the right file. 77 analyzer renames in the analyzers folder.

2. **`src/ANcpLua.Analyzers/AnalyzerReleases.Unshipped.md`** — replace 89 entries with their AL{new} IDs. Keep the same category/severity/notes columns. Sort by ID ascending after rewrite.

3. **`src/ANcpLua.Analyzers/AnalyzerReleases.Shipped.md`** — no change needed (file is empty). The 2.0.0 ship will move all 89 Unshipped entries into a new `## Release 2.0.0` header in Shipped.md as part of the release process — that's not part of the renumber PR itself.

4. **`src/ANcpLua.Analyzers/Resources.resx`** — apply the 85 prefix renames in §4 + delete the 44 orphan prefix groups in §3.2. Then regenerate `Resources.Designer.cs`.

5. **`src/ANcpLua.Analyzers.CodeFixes/CodeFixResources.resx`** — apply the 37 prefix renames + delete the 13 orphan keys in §3.3. Then regenerate `CodeFixResources.Designer.cs`.

6. **`src/ANcpLua.Analyzers.CodeFixes/CodeFixes/AL*.cs`** (37 files) — rewrite every `"AL\d{4}"` literal in the `FixableDiagnosticIds` array of each code-fix provider, and rewrite the resx key references (`CodeFixResources.AL{old}CodeFixTitle` -> `CodeFixResources.AL{new}CodeFixTitle`). File renames recommended for searchability (37 renames).

7. **`tests/ANcpLua.Analyzers.Tests/AL*.cs`** (68 test files) — rewrite every `"AL\d{4}"` string literal asserting diagnostic IDs, every test class name (`class Al####…Tests`), and file names (recommended). The convention test `AnalyzerConventionTests.AllDiagnosticIdsMatchExpectedFormat` continues to pass because new IDs still match `^AL\d{4}$`.

8. **`tests/ANcpLua.Analyzers.Tests/ANcpLua.Analyzers.Tests.csproj` line 9** — `<NoWarn>$(NoWarn);RS0030;IDE0028;IDE0055;IDE1006;CA1707;CA1859;AL0039</NoWarn>` — change `AL0039` to `AL1210`.

9. **`src/ANcpLua.Analyzers.AnalyzerDocs/ANcpLua.Analyzers.AnalyzerDocs.csproj` line 8** — `…RS1041;AL0028</NoWarn>` — change `AL0028` to `AL1200`.

10. **`src/ANcpLua.Analyzers.AnalyzerDocsGenerator/ANcpLua.Analyzers.AnalyzerDocsGenerator.csproj` lines 5+10+23** — `AL0101` (twice in body) → `AL1408`; the line-23 comment `AL0039` → `AL1210`.

11. **`Directory.Packages.props` line 2** — comment `…dogfooded AL0019 inspects…` → `…dogfooded AL1602 inspects…`.

12. **`src/ANcpLua.Analyzers/build/ANcpLua.Analyzers.props` lines 4+6** — comment `AL0018 to detect …` → `AL1601 to detect …` and `AL0094/AL0095/AL0096` comment must drop AL0096 and become `AL1406/AL1407` (AL0096 is dead — see §3.5).

13. **`src/ANcpLua.Analyzers/AotContext.cs:7`** — XML-doc comment `(AL0094, AL0095, AL0096)` → `(AL1406, AL1407)`; drop AL0096.

14. **`.editorconfig`** — apply per-rule rewrites for all 89 IDs in the dogfood section (lines ≈1011-1086) and delete the dead `AL0010/AL0012/AL0013` blocks (lines 1045-1064).

15. **`docs/Al0028UseIsEqualTo.md`** — regenerate via `pwsh scripts/generate-docs.ps1`; the markdown contains `AL0028:` reference text on line 16 that the docs-generator will rewrite once the underlying scenario class file (`Al0028UseIsEqualToDocs.cs`) and analyzer references are updated.

16. **`docs/analyzer-confidence-audit-2026-05-23.md`** — historical audit referencing old IDs (AL0011, AL0014-16, AL0026-27, AL0028-35, AL0036-40, AL0045-51, AL0053, AL0080-82, AL0084, AL0106, AL0117-18, AL0125-26, AL0137, AL0139, plus deleted AL0140). Either leave as a frozen historical doc with a top-of-file note pointing to this renumber plan, or apply the same rewrite. The author's call.

17. **`README.md` lines 43-130** — full rule-catalog table needs all 89 ID + URL rewrites. Also: line 130 "**`AL0126`** — `CancellationToken` propagation" → `**`AL1313`**`. Code-fixes block at lines 132-134 lists 37 IDs by comma — full rewrite.

18. **`scripts/generate-docs.ps1`** — no AL-literal references; only invokes `dotnet run -- generate` against the docs-generator project, which is data-driven by the `[Scenario]`-tagged classes in `src/ANcpLua.Analyzers.AnalyzerDocs/`. After file 19 below is updated, re-running this script regenerates `docs/*.md`.

19. **`src/ANcpLua.Analyzers.AnalyzerDocs/Al0028UseIsEqualToDocs.cs`** — class name `Al0028UseIsEqualToDocs` → `Al1200UseIsEqualToDocs`, file rename, and the `AL0028` comment on line 30 → `AL1200`. (This is the only scenarios file today; the docs-generator scaffold is set up to handle one-class-per-rule.)

---

## 6. Version decision

NuGet's current latest published version of `ANcpLua.Analyzers` is **`1.29.4`** (verified via `https://api.nuget.org/v3-flatcontainer/ancplua.analyzers/index.json` 2026-05-25). All 85 published versions live in the `1.x` channel.

**Proposed next-major version: `2.0.0`.**

This is the canonical breaking change pattern for a Roslyn analyzer assembly: every public diagnostic ID changes simultaneously, which invalidates downstream `.editorconfig` / `.globalconfig` / `#pragma warning disable` / `<NoWarn>` references and breaks build-time enforcement until consumers re-pin to the new IDs. SemVer requires the major bump. The renumber + the resx orphan-key cleanup ship together in 2.0.0.

`Version.props` and any `<Version>` literal in `src/ANcpLua.Analyzers/ANcpLua.Analyzers.csproj` should be bumped to `2.0.0`. The CHANGELOG entry should explicitly enumerate (or link to this plan for) the full old→new mapping so consumers can scripted-rewrite.

---

## 7. Cross-package migration candidates (OTel semantic-conventions-shaped)

Scope: identify ANcpLua.Analyzers rules whose **semantic purpose** is OpenTelemetry semantic-conventions-shaped (Activity / ActivitySource / Meter / span / metric / event / OTLP / SchemaUrl / GenAI / `gen_ai.*` / resource attributes). Such rules are misplaced in this assembly and semantically belong in `Qyl.Opentelemetry.SemanticConventions.Analyzers`.

The **renumber mapping in section 2 is NOT changed by this analysis** — these rules keep their AL → AL1xxx renumber for 2.0.0. The candidate list is purely informational for a follow-on cross-package migration the team lead will surface to the user.

**Result: zero strong migration candidates.** The full 89-rule audit produced no rule whose primary purpose is OTel semantic-conventions enforcement. Three rules use the literal category label `"GenAI"` (AL0128, AL0129, AL0130) but they enforce a `[LoomTool]` attribute contract — they are about agent governance (destructive-action gating, side-effect annotation, capability declaration) and do not touch `Activity`, `ActivitySource`, `Meter`, OTLP, schemas, or any `gen_ai.*` semconv. They stay in ANcpLua.

Four ASP.NET Core rules sit in the observability-adjacent space (`AL0080` resilience, `AL0081` health checks, `AL0082` connection strings, `AL0084` service discovery). None are OTel-semantic-conventions-shaped — they enforce `Microsoft.Extensions.Http.Resilience`, `Microsoft.Extensions.Diagnostics.HealthChecks`, connection-string hygiene, and `Microsoft.Extensions.ServiceDiscovery` registration. They stay in ANcpLua.

For completeness, the table below documents the scan's conclusion per category that could plausibly look OTel-shaped at first glance:

| AL ID  | Title                                                  | OTel-shape check                                                                                  | Risk class |
|--------|--------------------------------------------------------|---------------------------------------------------------------------------------------------------|------------|
| AL0128 | Destructive Loom tool must require approval            | Enforces `[LoomTool]` + `[RequiresApproval]` attribute contract; nothing to do with OTel spans/metrics. | keep       |
| AL0129 | Loom tool should declare its side effect               | Enforces `[ToolSideEffect]` attribute; agent-governance, not OTel.                                | keep       |
| AL0130 | Loom tool should declare required capabilities         | Enforces `[RequiresCapability]` attribute; agent-governance, not OTel.                            | keep       |
| AL0080 | Missing resilience configuration                       | About `AddStandardResilienceHandler`/Polly composition; not OTel.                                  | keep       |
| AL0081 | Missing health checks                                  | About `AddHealthChecks()`; container-orchestration concern, not OTel.                              | keep       |
| AL0082 | Consider using configuration for connection string     | Hardcoded-secret hygiene; not OTel.                                                                | keep       |
| AL0084 | Missing service discovery                              | About .NET Aspire `http+https://name` URL pattern; service-mesh, not OTel semconv.                 | keep       |
| AL0026 | Avoid `DateTime`/`DateTimeOffset` time accessors       | About `TimeProvider` for testability; not OTel timestamps.                                         | keep       |

**Candidates flagged: 0.**

The orphan resx keys enumerated in §3.2 (`AL0061..AL0079`, `AL0085..AL0093`, `AL0096`, `AL0107..AL0110`, `AL0113`, `AL0124`, `AL0131..AL0136`) are the **historical evidence** of the cross-package migration that already happened — those exact numeric IDs are now live `QYL####` rules in the Qyl analyzer assembly. The Qyl repo's `eng/analyzer-renumber-plan.md` section 4 maps these to their new `QYL####` IDs.

---

## 8. Release strategy & consumer scan

### 8.1 Chosen release path: (B) Coordinated simultaneous release

**Decision:** Publish ANcpLua.Analyzers 2.0.0 and ANcpLua.NET.Sdk simultaneously with lockstep version pins to avoid the transient broken state where consumers have the new analyzer package but old SDK .editorconfig mappings.

**Steps:**
1. Merge all AL→AL1### renumbering commits to main in ANcpLua.Analyzers
2. Build and stage ANcpLua.Analyzers 2.0.0 nupkg (do NOT publish yet)
3. Update ANcpLua.NET.Sdk:
   - Update `src/Config/Analyzer.ANcpLua.Analyzers.editorconfig` with all new AL1### IDs
   - Bump `ANcpLuaAnalyzersVersion` to `2.0.0` in SDK's Version.props
   - Update any other references to old AL00## IDs
4. Build and stage ANcpLua.NET.Sdk nupkg (do NOT publish yet)
5. Publish both packages within the same maintenance window (< 5 minutes apart)
6. Monitor for any consumer issues in the first 24 hours

**Rationale:** Option A (sequential publish) would create a window where consumers with auto-restore would get:
- New analyzer reporting AL1### diagnostics
- Old SDK .editorconfig still referencing AL00## (causing all per-rule severity customizations to stop working)
- Result: transient flood of unexpected diagnostics until SDK update propagates

Option B eliminates this gap by ensuring the matching .editorconfig ships simultaneously.

### 8.2 Consumer impact scan

Repos in `~/RiderProjects/` that reference `AL####` literally and will need rewiring after the 2.0.0 break:

| Repo                                  | Files referencing AL#### | Surface area                                                                                       |
|---------------------------------------|--------------------------|----------------------------------------------------------------------------------------------------|
| `ANcpLua.NET.Sdk`                     | 3                        | `tools/.editorconfig` (1 line: AL0025), `src/Config/Analyzer.ANcpLua.Analyzers.editorconfig` (every AL#### with a per-rule severity block — high-traffic config that consumers inherit transitively via the SDK reference). |
| `ANcpLua.Roslyn.Utilities`            | 6                        | **Sibling-package owner of AL0097/0098/0099/0100/0200/0201/0202/0300/0301/0302/0303.** Not a consumer of ANcpLua.Analyzers IDs; it just uses the AL prefix for its own analyzers. No rewiring needed. One stale XML-doc comment in `AnalyzerTest.cs:201` references "AL0018" as an example — needs an AL→AL1602 update or a rewrite to a generic placeholder. |
| `ErrorOrX`                            | 1                        | `.editorconfig` (4 lines: AL0025, AL0026, AL0027, AL0029) — all `style/usage` rules.                |
| `qyl`                                 | 2                        | `.globalconfig` (25+ lines covering AL0001-0025) and `.editorconfig` (similar set). High-impact — this is the largest downstream consumer. |
| `TourPlanner`                         | 1                        | `.editorconfig` line 18: `dotnet_diagnostic.AL0038.severity = none` — **stale** (AL0038 has never existed in this repo; safe to delete). |

5 repos will need rewiring. The largest by surface area: `qyl/.globalconfig` and `ANcpLua.NET.Sdk/src/Config/Analyzer.ANcpLua.Analyzers.editorconfig`. The `qyl` repo's existing OTel-shaped configs (e.g., `qyl/.globalconfig:101 # AL0013 # Missing schema URL`) reference IDs that **no longer exist in ANcpLua.Analyzers** — those are leftover dead config from the original AL-prefixed OTel analyzer; the al-qyl-rewire agent should delete them outright rather than try to map them.

`ANcpLua.Roslyn.Utilities` is **not** a consumer in the dependency sense — it's a sibling package using the same prefix. The al-qyl-rewire agent should skip the `DiagnosticDescriptors.cs` files in that repo (`ANcpLua.AotReflection`, `ANcpLua.ExtensibleEnumMirror`, `ANcpLua.DiscriminatedUnion`) because their AL IDs are owned by those packages, not by ANcpLua.Analyzers.

### Scan command (for reproducibility)

```bash
for dir in /Users/ancplua/RiderProjects/*/; do
  hits=$(grep -rE '"AL[0-9]{4}"|#pragma warning (disable|restore) AL[0-9]{4}|dotnet_diagnostic\.AL[0-9]{4}' "$dir" \
    --include="*.cs" --include="*.csproj" --include="*.props" --include="*.targets" \
    --include="*.editorconfig" --include="*.globalconfig" --include="*.md" \
    -l 2>/dev/null | wc -l | tr -d ' ')
  [ "$hits" -gt 0 ] && printf "%-65s %3d files\n" "$(basename "$dir")" "$hits"
done
```

---

## 9. Docs-generator notes (for the follow-on al-doc-gen agent)

This repo has a working analyzer-docs generator pipeline. The al-doc-gen agent will need to know:

- **Generator project:** `src/ANcpLua.Analyzers.AnalyzerDocsGenerator/` — abstract base classes (`DocsGenerator.cs`, `DocsVerifier.cs`, `ScenarioAttribute.cs`). 
- **Scenarios project:** `src/ANcpLua.Analyzers.AnalyzerDocs/` — concrete `Al####Docs` classes per analyzer, each implementing one `[Scenario]`-tagged method per scenario, plus a `{Name}_Failure` twin method invoking the analyzer to capture its diagnostic message.
- **Output:** `docs/{ClassName-without-Docs-suffix}.md` at repo root (currently only `docs/Al0028UseIsEqualTo.md` exists; the scaffold supports one MD per scenarios class).
- **Driver script:** `scripts/generate-docs.ps1` — pushes into `src/ANcpLua.Analyzers.AnalyzerDocs/`, runs `dotnet run -c Release -- generate`, writes the MD file. CI invocation: `scripts/generate-docs.ps1 -ValidateNoChanges` (fails on stale-docs PRs).
- **Source-of-truth question:** the generator walks the **scenario classes** in `src/ANcpLua.Analyzers.AnalyzerDocs/Al####...Docs.cs`, NOT `AnalyzerReleases.Shipped.md` and NOT `DiagnosticDescriptors.cs`. After the renumber, every existing scenarios source file needs class rename, file rename, and any `AL####` comment text rewrite — see `DocsGenerator.cs:65` (it parses the source file via `SyntaxFactory.ParseCompilationUnit`, then reflects on the matching class by namespace+name into the compiled scenarios assembly at line 60). The scenarios assembly is `ANcpLua.Analyzers.AnalyzerDocs.dll`; `Al####...Docs` class names are reflected by full name `ANcpLua.Analyzers.AnalyzerDocs.Al####...Docs`.
- **One-scenario-only scaffold today:** `Al0028UseIsEqualToDocs.cs` is the only existing scenarios file. The al-doc-gen agent's main job is to (a) rename it to `Al1200UseIsEqualToDocs.cs` with class `Al1200UseIsEqualToDocs`, update the `AL0028:` comment to `AL1200:`, and (b) generate scaffolds for the remaining 88 analyzers if scope expands. The generator project's `Program.cs` is just `await new AlAnalyzerDocsGenerator().ExecuteAsync();` — extending to 89 classes is a matter of one-line `Program.cs` updates plus 88 new Docs files.
- **No source-of-truth conflict with this renumber plan:** because the docs-generator reads scenarios source files (not the analyzer release-tracking md and not the resx), the renumber and the docs regeneration are decoupled. Run the renumber PR first, then re-run `scripts/generate-docs.ps1` to refresh `docs/Al0028UseIsEqualTo.md` → `docs/Al1200UseIsEqualTo.md`.

The al-doc-gen agent may also want to rewrite the docs-generator to *additionally* walk `DiagnosticDescriptors`/Shipped.md and emit a stub markdown per AL ID that has no scenarios class — that's a green-field enhancement beyond the renumber scope.

---

## 10. Notes / non-goals

- **No public DLL signature changes other than the IDs themselves.** The analyzer class names (`Al####...Analyzer`) and code-fix class names (`Al####...CodeFixProvider`) change in lockstep, but they are internal Roslyn-discovery types — consumers reference them only by AL ID, not by class name.
- **`AlAnalyzer.HelpLink(id)` returns `https://ancplua.mintlify.app/analyzers/rules/{id}`.** Help-link URLs change from `…/AL0017` to `…/AL1600` etc. The Mintlify docs site will need a parallel update (out of scope for the analyzer renumber PR itself — that's a docs-site change). Until those pages exist, the help links will 404. Consider adding HTTP redirects on the docs site from `/analyzers/rules/AL{old}` → `/analyzers/rules/AL{new}` for the 89 mapped rules, so old IDE links continue to resolve.
- **CodeFix dispatcher (`ALCodeFixProvider.cs`)** — verify this file (no AL literals found in current grep, so likely nothing to change), but a 30-second review during execution is cheap insurance.
- **`AnalyzerConventionTests.AllAnalyzersFollowNamingConvention`** expects class names to match `^Al\d{4}.*Analyzer$` — the renumbered class names (`Al1000…Analyzer`, etc.) continue to satisfy this regex. No test change needed.
- **`AnalyzerConventionTests.AllDiagnosticIdsMatchExpectedFormat`** expects IDs to match `^AL\d{4}$` — all new IDs satisfy this. No test change needed.
- **No `AL9####` suppressor pattern** exists in this repo (unlike Qyl's `QYL9####` runtime-derived suppressor IDs). `AL9000+` is free space for future use.
