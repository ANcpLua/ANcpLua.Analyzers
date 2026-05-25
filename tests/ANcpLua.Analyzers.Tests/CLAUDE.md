# ANcpLua.Analyzers.Tests - Test Project

Unit tests for all analyzers and code fixes.

## Target

- **Framework:** net10.0
- **Test Framework:** xunit.v3.mtp-v2 ($(XunitV3Version) = 3.2.2)
- **Assertions:** AwesomeAssertions ($(AwesomeAssertionsVersion) = 9.4.0)
- **Test Infrastructure:** ANcpLua.Roslyn.Utilities.Testing ($(ANcpLuaRoslynUtilitiesTestingVersion))

## Running Tests

```bash
# All tests
dotnet test --project tests/ANcpLua.Analyzers.Tests/ANcpLua.Analyzers.Tests.csproj

# Filter by diagnostic ID (MTP syntax)
dotnet test --project tests/ANcpLua.Analyzers.Tests/ANcpLua.Analyzers.Tests.csproj --filter-method "*AL1000*"

# Filter by test class
dotnet test --project tests/ANcpLua.Analyzers.Tests/ANcpLua.Analyzers.Tests.csproj --filter-class "*PartialType*"

# List tests without running
dotnet test --project tests/ANcpLua.Analyzers.Tests/ANcpLua.Analyzers.Tests.csproj --list-tests
```

**Important:** Use `--filter-method` (MTP syntax), NOT `--filter "FQN~..."` (VSTest syntax).

## Test Base Classes

From `ANcpLua.Roslyn.Utilities.Testing`:

| Class                          | Purpose                          |
|--------------------------------|----------------------------------|
| `AnalyzerTest<TAnalyzer>`      | Analyzer-only tests              |
| `CodeFixTest<TAnalyzer, TFix>` | Code fix tests                   |

## Analyzer Test Pattern

```csharp
public sealed class Al1000Tests : AnalyzerTest<Al1000ProhibitPrimaryConstructorParameterReassignmentAnalyzer> {
    // Parameterized tests with condensed InlineData
    [Theory]
    [InlineData("int i", "[|i|] = 10")]
    [InlineData("int i", "[|i|]++")]
    [InlineData("int i", "[|i|] += 1")]
    public Task ShouldReport(string param, string stmt) =>
        VerifyAsync($"public class C({param}) {{ void M() {{ {stmt}; }} }}");

    // No-diagnostic tests
    [Fact]
    public Task ShouldNotReport_ReadOnly() =>
        VerifyAsync("public class C(int i) { void M() { var x = i; } }");
}
```

## Code Fix Test Pattern

```csharp
public sealed class Al1700CodeFixTests : CodeFixTest<Al1700PreferStaticLambdaAnalyzer, Al1700StaticLambdaCodeFixProvider> {
    [Fact]
    public Task ShouldFix() =>
        VerifyCodeFixAsync(
            "class C { System.Action a = [|() => { }|]; }",  // Before - with diagnostic marker
            "class C { System.Action a = static () => { }; }"); // After - expected fix result
}
```

## Diagnostic Markers

| Marker              | Meaning                                |
|---------------------|----------------------------------------|
| `[|code|]`          | Expected diagnostic at this location   |
| `{|AL1000:code|}`   | Expected specific diagnostic ID        |

## Style Guidelines

Prefer condensed `[InlineData]` over verbose raw strings:

```csharp
// Preferred - condensed
[InlineData("int i", "[|i|] = 10")]
public Task Test(string p, string s) => VerifyAsync($"class C({p}) {{ void M() {{ {s}; }} }}");

// Avoid - verbose raw strings for simple cases
[InlineData("""
    public class C(int i) {
        void M() { [|i|] = 10; }
    }
    """)]
```

## Key Dependencies

- `ANcpLua.Roslyn.Utilities.Testing` - Test base classes (AnalyzerTest, CodeFixTest)
- `xunit.v3.mtp-v2` - Test framework with MTP protocol
- `AwesomeAssertions` - Fluent assertions (replaces FluentAssertions)
- `Basic.Reference.Assemblies.Net100` - Reference assemblies for compilation
