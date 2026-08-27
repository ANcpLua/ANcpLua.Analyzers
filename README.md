[![NuGet ANcpLua.Analyzers](https://img.shields.io/nuget/v/ANcpLua.Analyzers?label=ANcpLua.Analyzers&color=0891B2)](https://www.nuget.org/packages/ANcpLua.Analyzers/)
[![NuGet ANcpLua.NET.Sdk](https://img.shields.io/nuget/v/ANcpLua.NET.Sdk?label=.NET.Sdk&color=0891B2)](https://www.nuget.org/packages/ANcpLua.NET.Sdk/)
[![NuGet ANcpLua.Roslyn.Utilities](https://img.shields.io/nuget/v/ANcpLua.Roslyn.Utilities?label=.Roslyn.Utilities&color=0891B2)](https://www.nuget.org/packages/ANcpLua.Roslyn.Utilities/)
[![NuGet ANcpLua.Agents](https://img.shields.io/nuget/v/ANcpLua.Agents?label=.Agents&color=0891B2)](https://www.nuget.org/packages/ANcpLua.Agents/)
[![.NET](https://img.shields.io/badge/.NET-netstandard2.0-512BD4)](https://dotnet.microsoft.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

# ANcpLua.Analyzers

Roslyn diagnostic analyzers and code fixes for modern C# correctness, async and threading reliability, AOT and trim safety, ASP.NET Core / Aspire hosting, Roslyn-author hygiene, package/version management, agent-tool governance, and the ANcpLua Roslyn-utilities helper API — 90 rules across 9 domain bands.

Targets: `netstandard2.0` (Roslyn host requirement)

Host floor: built against `Microsoft.CodeAnalysis` 5.9.0, so a Roslyn 5.9+ compiler host is required — the compiler bundled with .NET SDK 10.0.400 is Roslyn 5.9. An older host reports [CS9057](https://github.com/dotnet/roslyn/issues/64754) and skips every rule in this package rather than running a subset.

## Family

| Package | Contents |
|---|---|
| [`ANcpLua.Analyzers`](https://www.nuget.org/packages/ANcpLua.Analyzers/) | Roslyn diagnostic analyzers + code fixes (this package) |
| [`ANcpLua.NET.Sdk`](https://www.nuget.org/packages/ANcpLua.NET.Sdk/) | MSBuild SDK that auto-injects this analyzer package and shared `.editorconfig` defaults |
| [`ANcpLua.Roslyn.Utilities`](https://www.nuget.org/packages/ANcpLua.Roslyn.Utilities/) | Shared Roslyn helpers, extensions, and `Guard.*` API the `AL12xx` band promotes |
| [`ANcpLua.Agents`](https://www.nuget.org/packages/ANcpLua.Agents/) | Microsoft Agent Framework consumer toolkit; the `AL18xx` agent-governance band targets its `[LoomTool]` attributes |

## Domain bands

| Range | Domain | Rules |
|---|---|---|
| `AL1000..1099` | Correctness / language pitfalls | 13 |
| `AL1100..1199` | ASP.NET Core / Aspire / web hosting | 10 |
| `AL1200..1299` | Roslyn Utilities helper API surface | 21 |
| `AL1300..1399` | Async / threading / reliability | 15 |
| `AL1400..1499` | AOT / trim safety | 10 |
| `AL1500..1599` | Roslyn-author hygiene | 7 |
| `AL1600..1699` | Package / version management / doc alignment | 7 |
| `AL1700..1799` | Style | 4 |
| `AL1800..1899` | Agent / tool governance (Loom) | 3 |

`AL0xxx` is reserved for sibling packages in the ANcpLua family that ship their own AL-prefixed analyzers. `AL1900..AL9999` is reserved for future bands.

## Rules

### Correctness / language pitfalls (`AL1000..1012`)

| ID | Severity | Title |
|---|---|---|
| AL1000 | Error | Prohibit reassignment of primary constructor parameters |
| AL1001 | Warning | Don't repeat negated patterns |
| AL1002 | Error | Don't divide by constant zero |
| AL1003 | Warning | Use pattern matching when comparing Span with constants |
| AL1004 | Warning | Use SequenceEqual when comparing Span with non-constants |
| AL1005 | Warning | Field name conflicts with primary constructor parameter |
| AL1006 | Error | GetSchema should be explicitly implemented |
| AL1007 | Error | GetSchema must return null and not be abstract |
| AL1008 | Error | Don't call IXmlSerializable.GetSchema |
| AL1009 | Warning | Avoid lock keyword on non-Lock types |
| AL1010 | Warning | Prefer pattern matching for null and zero comparisons |
| AL1011 | Info | Normalize null-guard style |
| AL1012 | Info | Combine declaration with subsequent null-check |

### ASP.NET Core / Aspire (`AL1100..1109`)

| ID | Severity | Title |
|---|---|---|
| AL1100 | Error | IFormCollection requires explicit attribute |
| AL1101 | Error | Multiple structured form sources |
| AL1102 | Error | Mixed form collection and DTO |
| AL1103 | Error | Unsupported form type |
| AL1104 | Error | Form and body conflict |
| AL1105 | Warning | Missing resilience configuration |
| AL1106 | Warning | Missing health checks |
| AL1107 | Info | Consider using configuration for connection string |
| AL1108 | Warning | Missing service discovery |
| AL1109 | Warning | Avoid Task.Run in ASP.NET Core request handlers |

### Roslyn Utilities helper API (`AL1200..1220`)

| ID | Severity | Title |
|---|---|---|
| AL1200 | Info | Use IsEqualTo extension |
| AL1201 | Info | Use HasAttribute extension |
| AL1202 | Info | Use type hierarchy extension |
| AL1203 | Info | Use operation extension |
| AL1204 | Info | Use OrEmpty extension |
| AL1205 | Info | Use ToImmutableArrayOrEmpty extension |
| AL1206 | Info | Use WhereNotNull extension |
| AL1207 | Info | Use symbol display string extension |
| AL1208 | Warning | Use null-guard helper |
| AL1209 | Warning | Use TryParse extension |
| AL1210 | Warning | Use StringComparison extension |
| AL1211 | Warning | Use attribute argument extraction extension |
| AL1212 | Warning | Use null-or-empty guard helper |
| AL1213 | Warning | Use null-or-whitespace guard helper |
| AL1214 | Warning | Use zero-guard helper |
| AL1215 | Warning | Use non-negative guard helper |
| AL1216 | Warning | Use positive-guard helper |
| AL1217 | Warning | Use empty-guid guard helper |
| AL1218 | Warning | Use defined-enum guard helper |
| AL1219 | Info | Use *Any* string comparison extension |
| AL1220 | Warning | Use Guard.* helpers instead of throw helpers |

### Async / threading / reliability (`AL1300..1314`)

| ID | Severity | Title |
|---|---|---|
| AL1300 | Warning | Avoid async void methods |
| AL1301 | Warning | Avoid lock on 'this' |
| AL1302 | Warning | Avoid lock on typeof(T) |
| AL1303 | Warning | Avoid lock on string |
| AL1304 | Warning | Prefer 'await using' for IAsyncDisposable |
| AL1305 | Warning | Avoid blocking calls in async methods |
| AL1306 | Warning | Avoid SQL string interpolation in CommandText |
| AL1307 | Warning | Avoid fire-and-forget task discard |
| AL1308 | Warning | Prefer TryParse over Parse |
| AL1309 | Warning | Empty catch block swallows exceptions |
| AL1310 | Warning | Exception details leaked in HTTP response |
| AL1311 | Warning | Unnecessary LINQ materialization |
| AL1312 | Warning | Read-modify-write without transaction |
| AL1313 | Info | Forward CancellationToken to invocations that support it |
| AL1314 | Warning | Use Math.Round/MathF.Round overload with explicit MidpointRounding |

### AOT / trim safety (`AL1400..1409`)

| ID | Severity | Title |
|---|---|---|
| AL1400 | Error | Method with [AotTest] or [TrimTest] must return int |
| AL1401 | Warning | [AotTest]/[TrimTest] method should return 100 on success |
| AL1402 | Warning | [TrimSafe] code must not call methods with [RequiresUnreferencedCode] |
| AL1403 | Warning | [AotSafe] code must not call methods with [RequiresDynamicCode] |
| AL1404 | Error | [AotSafe] code must not call [AotUnsafe] code |
| AL1405 | Warning | Unnecessary [AotUnsafe] attribute |
| AL1406 | Warning | Avoid 'dynamic' keyword in AOT-published code |
| AL1407 | Warning | Avoid Expression.Compile() in AOT context |
| AL1408 | Warning | Activator.CreateInstance is not AOT-safe |
| AL1409 | Warning | Type.GetType with dynamic name is not AOT-safe |

### Roslyn-author hygiene (`AL1500..1506`)

| ID | Severity | Title |
|---|---|---|
| AL1500 | Warning | Closed hierarchy match is not exhaustive |
| AL1501 | Warning | Avoid storing ISymbol in source generator models |
| AL1502 | Warning | Use IIncrementalGenerator instead of ISourceGenerator |
| AL1503 | Warning | Avoid NormalizeWhitespace in source generators |
| AL1504 | Error | [DuckDbTable] type must be partial |
| AL1505 | Warning | Conflicting [DuckDbColumn] ordinal values |
| AL1506 | Warning | Excluded code hides untested branches |

### Package / version management (`AL1600..1606`)

| ID | Severity | Title |
|---|---|---|
| AL1600 | Warning | Hardcoded package version detected |
| AL1601 | Warning | Version.props not imported |
| AL1602 | Warning | Undefined version variable |
| AL1603 | Warning | Diagnostic missing from documentation |
| AL1604 | Warning | Diagnostic missing from release notes |
| AL1605 | Warning | Diagnostic documentation mismatch |
| AL1606 | Warning | Outdated MAF ecosystem package version |

### Style (`AL1700..1703`)

| ID | Severity | Title |
|---|---|---|
| AL1700 | Warning | Anonymous function can be made static |
| AL1701 | Warning | Avoid DateTime/DateTimeOffset time accessors |
| AL1702 | Warning | Avoid legacy JSON library |
| AL1703 | Warning | Use implicit type when type is apparent |

### Agent / tool governance (`AL1800..1802`)

| ID | Severity | Title |
|---|---|---|
| AL1800 | Warning | Destructive Loom tool must require approval |
| AL1801 | Info | Loom tool should declare its side effect |
| AL1802 | Info | Loom tool should declare required capabilities |

## Usage

```xml
<PackageReference Include="ANcpLua.Analyzers"
                  Version="2.1.1"
                  PrivateAssets="all"
                  IncludeAssets="analyzers; buildtransitive" />
```

`IncludeAssets="analyzers; buildtransitive"` is what makes `<AlAnalysisMode>` and the bundled editorconfig profiles flow through. Consumers of `ANcpLua.NET.Sdk` get the analyzer auto-injected and don't need this `<PackageReference>`.

## Consumer-side severity profile (`<AlAnalysisMode>`)

Switch the whole `AL10xx`–`AL18xx` band in one csproj line instead of dropping editorconfig files:

```xml
<PropertyGroup>
  <AlAnalysisMode>AllAsErrors</AlAnalysisMode>
</PropertyGroup>
```

| Value | Behavior |
|---|---|
| `Default` | Every rule at its descriptor-declared default severity. Useful to override an ambient stricter config (incl. `ANcpLua.NET.Sdk`'s bundled profile). |
| `AllAsErrors` | Every AL rule promoted to error. Use for strict CI. |
| `Disabled` | Every AL rule silenced. |
| _(unset)_ | No editorconfig injection. Inside an `ANcpLua.NET.Sdk` consumer the SDK's bundled editorconfig still applies; outside it, descriptor severities apply. |

The knob is exposed via `buildTransitive/ANcpLua.Analyzers.props` in the NuGet, which appends the matching profile from `buildTransitive/editorconfig/` to `$(EditorConfigFiles)` on restore. The name is intentionally not bare `<AnalysisMode>` — that's owned by `Microsoft.CodeAnalysis.NetAnalyzers`.

## Documentation

- **[`docs/ANcpLua.Analyzers.md`](docs/ANcpLua.Analyzers.md)** — slim index with the full catalog, configuration knob, and cross-cutting policy.
- **[`docs/rules/`](docs/rules/)** — one markdown file per rule (`ALXXXX_<SymbolicName>.md`) with severity, category, code-fix status, description, and source link. Every descriptor's `HelpLinkUri` resolves to a per-rule page, so IDE Quick-Fix "Show error help" lands on the focused rule, not on a multi-thousand-line aggregate.
- **[`docs/ANcpLua.Analyzers.sarif`](docs/ANcpLua.Analyzers.sarif)** — SARIF v2.1.0 rule manifest for tool interop (Sonar bridges, GitHub Advanced Security uploads, IDE rule-catalog importers).
- **[`docs/editorconfig/`](docs/editorconfig/)** — three drop-in severity profiles: `Default`, `AllRulesAsErrors`, `AllRulesDisabled`. Same content ships inside the NuGet under `buildTransitive/editorconfig/` so `<AlAnalysisMode>` can pick them up.

Siblings: [ANcpLua.Roslyn.Utilities](https://github.com/ANcpLua/ANcpLua.Roslyn.Utilities) · [ANcpLua.NET.Sdk](https://github.com/ANcpLua/ANcpLua.NET.Sdk) · [ANcpLua.Agents](https://github.com/ANcpLua/ANcpLua.Agents) · [Qyl.Opentelemetry.SemanticConventions](https://github.com/ANcpLua/Qyl.Opentelemetry.SemanticConventions)
