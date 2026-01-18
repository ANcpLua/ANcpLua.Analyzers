[![NuGet](https://img.shields.io/nuget/v/ANcpLua.Analyzers?label=NuGet&color=0891B2)](https://www.nuget.org/packages/ANcpLua.Analyzers/)
[![Target: .NET Standard 2.0](https://img.shields.io/badge/Target-.NET%20Standard%202.0-512BD4)](https://dotnet.microsoft.com/platform/dotnet-standard)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

# ANcpLua.Analyzers

Roslyn analyzers for C# code quality, focusing on modern .NET patterns and best practices. Catches common mistakes at
compile time with actionable diagnostics and automatic code fixes.

> **Layer 2 Package**: This is a downstream package that
> uses [ANcpLua.NET.Sdk](https://github.com/ANcpLua/ANcpLua.NET.Sdk) for its build infrastructure. If you use
> ANcpLua.NET.Sdk, this analyzer package is **auto-injected** to all your projects.

## Installation

```bash
dotnet add package ANcpLua.Analyzers
```

Or add to your project file:

```xml
<PackageReference Include="ANcpLua.Analyzers" PrivateAssets="all" />
```

## Rules

| Rule   | Category          | Description                                             | Severity | Enabled | Code Fix |
|:-------|:------------------|:--------------------------------------------------------|:--------:|:-------:|:--------:|
| AL0001 | Design            | Prohibit reassignment of primary constructor parameters |    ❌     |   ✔️    |          |
| AL0002 | Design            | Don't repeat negated patterns                           |    ⚠️    |   ✔️    |    ✔️    |
| AL0003 | Reliability       | Don't divide by constant zero                           |    ❌     |   ✔️    |          |
| AL0004 | Usage             | Use pattern matching for Span constant comparison       |    ⚠️    |   ✔️    |    ✔️    |
| AL0005 | Usage             | Use SequenceEqual for Span non-constant comparison      |    ⚠️    |   ✔️    |    ✔️    |
| AL0006 | Design            | Field name conflicts with primary constructor parameter |    ⚠️    |   ✔️    |          |
| AL0007 | Usage             | GetSchema should be explicitly implemented              |    ⚠️    |   ✔️    |          |
| AL0008 | Usage             | GetSchema must return null and not be abstract          |    ⚠️    |   ✔️    |    ✔️    |
| AL0009 | Usage             | Don't call IXmlSerializable.GetSchema                   |    ⚠️    |   ✔️    |          |
| AL0010 | Design            | Type should be partial for source generator support     |    ℹ️    |         |    ✔️    |
| AL0011 | Threading         | Avoid lock keyword on non-Lock types                    |    ⚠️    |   ✔️    |          |
| AL0012 | OpenTelemetry     | Deprecated semantic convention attribute                |    ⚠️    |   ✔️    |    ✔️    |
| AL0013 | OpenTelemetry     | Missing telemetry schema URL                            |    ℹ️    |   ✔️    |          |
| AL0014 | Style             | Prefer pattern matching for null and zero comparisons   |    ℹ️    |   ✔️    |    ✔️    |
| AL0015 | Style             | Normalize null-guard style                              |    ℹ️    |   ✔️    |    ✔️    |
| AL0016 | Style             | Combine declaration with subsequent null-check          |    ℹ️    |   ✔️    |    ✔️    |
| AL0017 | VersionManagement | Hardcoded package version in Directory.Packages.props   |    ⚠️    |   ✔️    |          |
| AL0020 | AspNetCore        | IFormCollection requires explicit [FromForm]            |    ❌     |   ✔️    |          |
| AL0021 | AspNetCore        | Multiple structured form sources                        |    ❌     |   ✔️    |          |
| AL0022 | AspNetCore        | Mixed form collection and DTO                           |    ❌     |   ✔️    |          |
| AL0023 | AspNetCore        | Unsupported form type                                   |    ❌     |   ✔️    |          |
| AL0024 | AspNetCore        | Form and body conflict                                  |    ❌     |   ✔️    |          |

**Legend:** ❌ Error · ⚠️ Warning · ℹ️ Info

## Configuration

Configure rule severity in your `.editorconfig`:

```editorconfig
[*.cs]
# AL0001: Prohibit reassignment of primary constructor parameters
dotnet_diagnostic.AL0001.severity = error

# AL0002: Don't repeat negated patterns
dotnet_diagnostic.AL0002.severity = warning

# AL0003: Don't divide by constant zero
dotnet_diagnostic.AL0003.severity = error

# AL0004: Use pattern matching for Span constant comparison
dotnet_diagnostic.AL0004.severity = warning

# AL0005: Use SequenceEqual for Span non-constant comparison
dotnet_diagnostic.AL0005.severity = warning

# AL0006: Field name conflicts with primary constructor parameter
dotnet_diagnostic.AL0006.severity = warning

# AL0007: GetSchema should be explicitly implemented
dotnet_diagnostic.AL0007.severity = warning

# AL0008: GetSchema must return null and not be abstract
dotnet_diagnostic.AL0008.severity = warning

# AL0009: Don't call IXmlSerializable.GetSchema
dotnet_diagnostic.AL0009.severity = warning

# AL0010: Type should be partial for source generator support
dotnet_diagnostic.AL0010.severity = none

# AL0011: Avoid lock keyword on non-Lock types
dotnet_diagnostic.AL0011.severity = warning

# AL0012: Deprecated semantic convention attribute
dotnet_diagnostic.AL0012.severity = warning

# AL0013: Missing telemetry schema URL
dotnet_diagnostic.AL0013.severity = suggestion

# AL0014: Prefer pattern matching for null and zero comparisons
dotnet_diagnostic.AL0014.severity = suggestion

# AL0015: Normalize null-guard style
dotnet_diagnostic.AL0015.severity = suggestion

# AL0016: Combine declaration with subsequent null-check
dotnet_diagnostic.AL0016.severity = suggestion

# AL0017: Hardcoded package version
dotnet_diagnostic.AL0017.severity = warning

# AL0020-AL0024: ASP.NET Core form binding
dotnet_diagnostic.AL0020.severity = error
dotnet_diagnostic.AL0021.severity = error
dotnet_diagnostic.AL0022.severity = error
dotnet_diagnostic.AL0023.severity = error
dotnet_diagnostic.AL0024.severity = error
```

## Examples

### AL0001: Primary Constructor Parameter Reassignment

```csharp
// Error: Primary constructor parameter 'x' should not be reassigned
public class Example(int x)
{
    public void SetX(int value) => x = value;  // AL0001
}

// Fix: Use a separate field
public class Example(int x)
{
    private int _x = x;
    public void SetX(int value) => _x = value;
}
```

### AL0014: Pattern Matching for Null and Zero Comparisons

```csharp
// Before: AL0014 triggered
if (x == null) { }
if (x != null) { }
if (count == 0) { }
if (count != 0) { }

// After: Use pattern matching
if (x is null) { }
if (x is not null) { }
if (count is 0) { }
if (count is not 0) { }
```

> **Note:** This analyzer skips code inside expression trees (`Expression<T>`) where pattern matching is not supported.

## ANcpLua.NET.Sdk Integration

This analyzer is **auto-injected** when using [ANcpLua.NET.Sdk](https://github.com/ANcpLua/ANcpLua.NET.Sdk):

```xml
<!-- Just use the SDK - analyzer is automatically included -->
<Project Sdk="ANcpLua.NET.Sdk" />
```

### SDK Features You Get "For Free"

| Feature                 | Benefit                                                         |
|-------------------------|-----------------------------------------------------------------|
| Auto-injected analyzers | ANcpLua.Analyzers + BannedApiAnalyzers                          |
| Guard clauses           | `Throw.IfNull()` replaces `ArgumentNullException.ThrowIfNull()` |
| Banned API enforcement  | 50+ banned APIs enforced at compile time                        |
| Test framework          | xUnit v3 + AwesomeAssertions auto-configured                    |
| Code quality            | Comprehensive .editorconfig with 100s of rules                  |

### Guard Clauses (Auto-Injected)

```csharp
// Available in all SDK projects - replaces ArgumentNullException.ThrowIfNull
public void Process(string name, List<int> items)
{
    Throw.IfNull(name);                // Auto-captures param name
    Throw.IfNullOrEmpty(items);        // Works on collections
    Throw.IfNullOrWhitespace(name);    // String validation
    Throw.IfOutOfRange(myEnum);        // Enum validation
    Throw.IfLessThan(count, 0);        // Range checks
}
```

### Test Base Classes (SDK Test Projects)

```csharp
// Analyzer testing - pre-configured with .NET 10 reference assemblies
public class MyAnalyzerTests : AnalyzerTest<MyDiagnosticAnalyzer>
{
    [Fact]
    public Task DetectsIssue() => VerifyAsync("class C { int x = 1 / 0; }");
}

// Integration testing - WebApplicationFactory + FakeLogCollector
public class ApiTests : IntegrationTestBase<Program>
{
    [Fact]
    public async Task GetUsers_ReturnsOk()
    {
        var response = await Client.GetAsync("/api/users");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
```

## Documentation

Full reference: https://ancplua.mintlify.app/analyzers/overview

## Related Projects

- [ANcpLua.NET.Sdk](https://github.com/ANcpLua/ANcpLua.NET.Sdk) - MSBuild SDK that auto-injects this analyzer
- [ANcpLua.Roslyn.Utilities](https://github.com/ANcpLua/ANcpLua.Roslyn.Utilities) - Roslyn utilities for source
  generators

## License

[MIT](LICENSE)