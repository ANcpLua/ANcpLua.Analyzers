# CLAUDE.md - ANcpLua.Analyzers

Roslyn analyzers for C# code quality (AL0001-AL0035).

## Claude Rules

- **Never ask confirmation for requested actions** - If user asks for X, do X. Don't ask "do you want X?"
- **Always commit and push with tags** - When releasing, create git tag and push

**SDK:** ANcpLua.NET.Sdk | **Target:** .NET 10 + netstandard2.0

## Ecosystem Position

```
LAYER 0: ANcpLua.Roslyn.Utilities  <- UPSTREAM (no SDK dependency!)
         | publishes .Sources
LAYER 1: ANcpLua.NET.Sdk           <- SOURCE OF TRUTH (Version.props)
         | auto-syncs Version.props
LAYER 2: ANcpLua.Analyzers         <- YOU ARE HERE (DOWNSTREAM)
         | consumed by
LAYER 3: qyl, other projects       <- END USERS
```

### This Repo: LAYER 2 (Downstream)

| Property | Value |
|----------|-------|
| **Upstream dependencies** | ANcpLua.NET.Sdk |
| **Downstream consumers** | qyl, all SDK consumers |
| **Version.props** | COPY from SDK |
| **Auto-sync** | YES (receives from SDK) |

---

## Rules (AL0001-AL0035)

| Rule | Severity | Description |
|------|----------|-------------|
| AL0001 | Error | Prohibit reassignment of primary constructor params |
| AL0002 | Warning | Don't repeat negated patterns |
| AL0003 | Error | Don't divide by constant zero |
| AL0004 | Warning | Use pattern matching for Span constant comparison |
| AL0005 | Warning | Use SequenceEqual for Span non-constant comparison |
| AL0006 | Warning | Field name conflicts with primary constructor parameter |
| AL0007 | Warning | GetSchema should be explicitly implemented |
| AL0008 | Warning | GetSchema must return null and not be abstract |
| AL0009 | Warning | Don't call IXmlSerializable.GetSchema |
| AL0010 | Info | Type should be partial for source generator support |
| AL0011 | Warning | Avoid lock keyword on non-Lock types (.NET 9+) |
| AL0012 | Warning | Deprecated OTel semantic convention attribute |
| AL0013 | Info | Missing telemetry schema URL |
| AL0014 | Warning | Prefer pattern matching for null/zero comparisons |
| AL0015 | Info | Normalize null-guard style |
| AL0016 | Info | Combine declaration with subsequent null-check |
| AL0017 | Warning | Hardcoded package version in Directory.Packages.props |
| AL0018 | Warning | Version.props not imported in Directory.Build.props |
| AL0019 | Warning | Undefined version variable in Directory.Packages.props |
| AL0020 | Error | IFormCollection requires explicit [FromForm] |
| AL0021 | Error | Multiple structured form sources |
| AL0022 | Error | Mixed form collection and DTO |
| AL0023 | Error | Unsupported form type |
| AL0024 | Error | Form and body conflict |
| AL0025 | Warning | Prefer static lambda |
| AL0026 | Warning | Avoid DateTime.Now/UtcNow, use TimeProvider |
| AL0027 | Warning | Avoid Newtonsoft.Json, use System.Text.Json |
| AL0028 | Info | Use IsEqualTo instead of SymbolEqualityComparer.Equals |
| AL0029 | Info | Use HasAttribute instead of GetAttributes() patterns |
| AL0030 | Info | Use Implements/InheritsFrom instead of type hierarchy loops |
| AL0031 | Info | Use IsMethodNamed/TryGetConstantValue instead of verbose patterns |
| AL0032 | Info | Use OrEmpty() instead of null-coalescing with empty collections |
| AL0033 | Info | Use ToImmutableArrayOrEmpty() instead of ?.ToImmutableArray() ?? Empty |
| AL0034 | Info | Use WhereNotNull() instead of Where(x => x != null) |
| AL0035 | Info | Use GetFullyQualifiedName/GetMetadataName() instead of ToDisplayString |

## Commands

```bash
# Build
dotnet build ANcpLua.Analyzers.slnx -c Release

# Test (MTP - no -- separator needed)
dotnet test --project tests/ANcpLua.Analyzers.Tests/ANcpLua.Analyzers.Tests.csproj

# Test with filter (xUnit v3 MTP syntax)
dotnet test --project tests/ANcpLua.Analyzers.Tests/ANcpLua.Analyzers.Tests.csproj --filter-method "*AL0001*"

# Pack
dotnet pack src/ANcpLua.Analyzers/ANcpLua.Analyzers.csproj -c Release -o artifacts -p:PackageId=ANcpLua.Analyzers
```

## Banned Patterns

| Pattern | Reason |
|---------|--------|
| `FluentAssertions` | Abandoned - use `AwesomeAssertions` |
| `Microsoft.NET.Test.Sdk` | VSTest legacy - use `xunit.v3.mtp-v2` |
| `--filter "FQN~..."` | VSTest syntax - use `--filter-method` |
| `dotnet-quality: preview` | .NET 10 is LTS |
| `LangVersion` in csproj | SDK-owned property |
| `Nullable` in csproj | SDK-owned property |

## Project Structure

```
src/
  ANcpLua.Analyzers/           # Analyzers (DiagnosticAnalyzer)
  ANcpLua.Analyzers.CodeFixes/ # Code fixes (CodeFixProvider)
tests/
  ANcpLua.Analyzers.Tests/     # Unit tests (xunit.v3.mtp-v2)
```

> **Docs**: Centralized at [ANcpLua.io](https://github.com/ANcpLua/ANcpLua.io)

## Key Facts

| Fact | Details |
|------|---------|
| SDK auto-injects this analyzer | Use `PackageId=Dummy` in csproj to prevent cycle |
| CI uses real PackageId | Workflow passes `-p:PackageId=ANcpLua.Analyzers` |
| Both DLLs required | Pack includes Analyzers.dll AND CodeFixes.dll |
| Target: netstandard2.0 | Only ns2.0 assemblies go in nupkg |
| **Info severity = IDE only** | `DiagnosticSeverity.Info` diagnostics (AL0010, AL0013, AL0015, AL0016, AL0028-AL0035) **only show in IDE**, not in `dotnet build` output. This is by design - MSBuild only shows Warning/Error by default. |

## Current Package Versions

| Package | Version |
|---------|---------|
| ANcpLua.Analyzers | 1.10.2 |
| ANcpLua.NET.Sdk | 1.6.21 |
| ANcpLua.Roslyn.Utilities | 1.16.0 |
| ANcpLua.Roslyn.Utilities.Testing | 1.16.0 |
| Roslyn | 5.0.0 |
| RoslynAnalyzers | 3.11.0 |
| xunit.v3 | 3.2.2 |
| AwesomeAssertions | 9.3.0 |

## GitHub Actions (Jan 2026)

```yaml
- uses: actions/checkout@v6
- uses: actions/setup-dotnet@v5
- uses: actions/upload-artifact@v6
```

## Analyzer Test Patterns

Use condensed single-line `InlineData` with interpolated boilerplate:

```csharp
// PREFERRED - Parameterized, condensed
[Theory]
[InlineData("int i", "[|i|] = 10")]
[InlineData("int i", "[|i|]++")]
public Task ShouldReport(string param, string stmt) =>
    VerifyAsync($"public class C({param}) {{ void M() {{ {stmt}; }} }}");

// AVOID - Verbose multi-line raw strings for simple cases
[InlineData("""
    public class C(int i) { void M() { [|i|] = 10; } }
    """)]
```

## Related Projects

| Project | Purpose |
|---------|---------|
| [ANcpLua.NET.Sdk](https://github.com/ANcpLua/ANcpLua.NET.Sdk) | MSBuild SDK that auto-injects this analyzer |
| [ANcpLua.Roslyn.Utilities](https://github.com/ANcpLua/ANcpLua.Roslyn.Utilities) | Shared Roslyn helpers |

## Common CI Errors

### SDK Version Not Found
```
error: Unable to find package ANcpLua.NET.Sdk with version (= X.X.X)
```

**Cause:** global.json references SDK version not yet published to NuGet.

**Fix:** Change global.json to latest published version:
```bash
# Check latest: https://www.nuget.org/packages/ANcpLua.NET.Sdk
sed -i '' 's/"ANcpLua.NET.Sdk": "X.X.X"/"ANcpLua.NET.Sdk": "LATEST"/' global.json
```

### Release Order (CRITICAL!)
```
1. Roslyn.Utilities -> publish to NuGet
2. SDK -> update Version.props -> publish to NuGet
3. THEN sync Version.props to Analyzers  <- YOU ARE HERE
4. Analyzers -> can now build
```
