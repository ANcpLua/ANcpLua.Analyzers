# ANcpLua.Analyzers.Tests - Test Project

Unit tests for all analyzers and code fixes using xUnit v3 and ANcpLua.Roslyn.Utilities.Testing.

## Project Info

| Property              | Value                                     |
|-----------------------|-------------------------------------------|
| **Target**            | net10.0                                   |
| **SDK**               | ANcpLua.NET.Sdk                           |
| **Test Framework**    | xunit.v3.mtp-v2 (3.2.2)                   |
| **Assertions**        | AwesomeAssertions (9.3.0)                 |
| **Testing Utilities** | ANcpLua.Roslyn.Utilities.Testing (1.16.0) |

## Running Tests

```bash
# All tests
dotnet test --project tests/ANcpLua.Analyzers.Tests/ANcpLua.Analyzers.Tests.csproj

# Filter by analyzer rule
dotnet test --project tests/ANcpLua.Analyzers.Tests/ANcpLua.Analyzers.Tests.csproj --filter-method "*AL0001*"

# Filter by class
dotnet test --project tests/ANcpLua.Analyzers.Tests/ANcpLua.Analyzers.Tests.csproj --filter-class "*PartialType*"

# List tests without running
dotnet test --project tests/ANcpLua.Analyzers.Tests/ANcpLua.Analyzers.Tests.csproj --list-tests
```

## Test Pattern

Tests inherit from `AnalyzerTest<TAnalyzer>` or `CodeFixTest<TAnalyzer, TCodeFix>`:

```csharp
public sealed class Al0001Tests : AnalyzerTest<Al0001ProhibitPrimaryConstructorParameterReassignmentAnalyzer> {
    [Theory]
    [InlineData("int i", "[|i|] = 10")]
    [InlineData("int i", "[|i|]++")]
    public Task ShouldReport(string param, string stmt) =>
        VerifyAsync($"public class C({param}) {{ void M() {{ {stmt}; }} }}");

    [Fact]
    public Task ShouldNotReport_ReadOnly() =>
        VerifyAsync("public class C(int i) { void M() { var x = i; } }");
}
```

## Test Markers

| Marker | Description |
|--------|-------------|
| `[|code|]` | Expected diagnostic location |
| `{|DiagId:code|}` | Expected specific diagnostic at location |

## Code Fix Test Pattern

```csharp
public sealed class Al0010CodeFixTests : CodeFixTest<Al0010PartialTypeAnalyzer, Al0010PartialTypeCodeFixProvider> {
    [Fact]
    public Task ShouldFix() =>
        VerifyCodeFixAsync(
            "[|class|] C { }",        // Before (with diagnostic marker)
            "partial class C { }");   // After (expected fix)
}
```

## Key Test Classes from ANcpLua.Roslyn.Utilities.Testing

| Class                          | Purpose                          |
|--------------------------------|----------------------------------|
| `AnalyzerTest<T>`              | Base for analyzer-only tests     |
| `CodeFixTest<TAnalyzer, TFix>` | Base for code fix tests          |
| `CodeFixTestWithEditorConfig`  | Tests with .editorconfig support |
| `Test<TGenerator>`             | Fluent API for generator tests   |

## Important Notes

- Use **MTP syntax** for filtering (`--filter-method`), NOT VSTest (`--filter "FQN~..."`)
- Tests use `[|markers|]` for expected diagnostic locations
- Prefer condensed `[InlineData]` over verbose raw strings
- `AwesomeAssertions` replaces abandoned `FluentAssertions`
- Info-severity diagnostics won't show in build output (IDE only)