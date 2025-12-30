---
name: dotnet-test-mtp
description: |
  Microsoft Testing Platform (MTP) CLI reference for .NET test projects. Use when:
  (1) Test errors show "Unknown option '--filter'" or "Unknown option '--logger'"
  (2) Exit code 5 from test runner (invalid arguments)
  (3) User asks about dotnet test filtering with xUnit v3, MSTest, NUnit, or TUnit
  (4) Zero tests ran with MTP test projects
  (5) Questions about --filter-query, --filter-class, --filter-method syntax
  (6) VSTest options not working with modern test frameworks
  (7) Migrating from VSTest to Microsoft.Testing.Platform
  (8) Configuring global.json for .NET 10+ native MTP support
  (9) CI/CD pipeline configuration for MTP tests (Azure DevOps, GitHub Actions)
---

# Microsoft Testing Platform (MTP) Reference

MTP is a lightweight, portable VSTest replacement embedded directly in test projects. No external runners needed.

## Quick Decision Tree

```
Is OutputType=Exe set? → No → Add <OutputType>Exe</OutputType>
Using .NET 10+?        → Yes → Use global.json { "test": { "runner": "Microsoft.Testing.Platform" } }
Using .NET 8/9?        → Yes → Add <TestingPlatformDotnetTestSupport>true</TestingPlatformDotnetTestSupport>
Using xUnit v3?        → Yes → Use xunit.v3.mtp-v2 package for .NET 10
```

## SDK-Specific Configuration

### .NET 10+ (Native Support)

```json
// global.json
{
  "sdk": { "version": "10.0.100", "rollForward": "latestMinor" },
  "test": { "runner": "Microsoft.Testing.Platform" }
}
```

No `--` separator needed. Run directly: `dotnet test --filter-method "*MyTest*"`

### .NET 8/9 (VSTest Compatibility Layer)

```xml
<!-- Directory.Build.props or csproj -->
<PropertyGroup>
  <OutputType>Exe</OutputType>
  <TestingPlatformDotnetTestSupport>true</TestingPlatformDotnetTestSupport>
</PropertyGroup>
```

Requires `--` separator: `dotnet test -- --filter-method "*MyTest*"`

## Framework Opt-In Properties

| Framework | Property | Package |
|-----------|----------|---------|
| MSTest | `<EnableMSTestRunner>true</EnableMSTestRunner>` | MSTest 3.2.0+ |
| NUnit | `<EnableNUnitRunner>true</EnableNUnitRunner>` | NUnit3TestAdapter 5.0.0+ |
| xUnit v3 | `<UseMicrosoftTestingPlatformRunner>true</UseMicrosoftTestingPlatformRunner>` | xunit.v3.mtp-v2 |
| TUnit | Built-in (always MTP) | TUnit |

## xUnit v3 Package Variants (.NET 10 Critical)

| Package | MTP Version | Use Case |
|---------|-------------|----------|
| `xunit.v3` | MTP v1 | Default, breaks on .NET 10 SDK |
| `xunit.v3.mtp-v2` | MTP v2 | **Required for .NET 10+** |
| `xunit.v3.mtp-v1` | MTP v1 | Explicit v1 lock |
| `xunit.v3.mtp-off` | None | VSTest only |

## CLI Argument Mapping (VSTest → MTP)

| VSTest | MTP | Notes |
|--------|-----|-------|
| `--filter "FQN~X"` | Framework-specific | See below |
| `--logger trx` | `--report-trx` | Requires `Microsoft.Testing.Extensions.TrxReport` |
| `--blame-crash` | `--crashdump` | Requires crash dump extension |
| `--blame-hang` | `--hangdump` | Requires hang dump extension |
| `--results-directory` | `--results-directory` | Same |
| `-t` / `--list-tests` | `--list-tests` | Same |
| `--settings file.runsettings` | Framework-specific | MSTest/NUnit still support |

## Filtering by Framework

### xUnit v3 (Unique Syntax)

```bash
--filter-class "Namespace.Class"       # Exact class
--filter-method "*Pattern*"            # Wildcard method
--filter-namespace "Namespace"         # By namespace
--filter-trait "Category=Unit"         # By trait
--filter-not-class "IntegrationTests"  # Exclusion
--filter-query "/**[Category=Fast]"    # Query language
```

Query syntax: `/<assembly>/<namespace>/<class>/<method>[traits]`

### MSTest / NUnit (VSTest-Compatible)

```bash
# Same syntax works with MTP
--filter "FullyQualifiedName~MyTest"
--filter "TestCategory=Unit"
```

## Running Tests

```bash
# Direct execution (fastest)
./MyTests.exe

# Via dotnet run
dotnet run --project MyTests

# Via dotnet test (.NET 10+)
dotnet test --filter-method "*Should*"

# Via dotnet test (.NET 8/9)
dotnet test -- --filter-method "*Should*"
```

## Common Exit Codes

| Code | Meaning |
|------|---------|
| 0 | Success |
| 1 | Unknown error |
| 2 | Tests failed |
| 3 | Tests cancelled |
| 5 | Invalid command line (wrong arguments!) |
| 6 | No tests found |
| 8 | Zero tests ran |

Ignore specific codes: `--ignore-exit-code 8`

## Troubleshooting

### "Unknown option '--filter'"
→ Using VSTest syntax with MTP. Use `--filter-method` / `--filter-class` for xUnit v3.

### Exit Code 5 (Invalid Arguments)
→ VSTest arguments passed to MTP runner. Check CLI mapping table above.

### "Zero tests ran" / Exit Code 8
1. Verify filter syntax matches your framework
2. Check `--` separator for .NET 8/9
3. Try `--list-tests` to see discovered tests
4. Run `--filter-query "/**"` to test without filter

### .NET 10 + xunit.v3 TypeLoadException
```
Could not load type 'Microsoft.Testing.Platform.Extensions.TestHost.IDataConsumer'
```
→ xunit.v3 default is MTP v1, incompatible with .NET 10. Switch to `xunit.v3.mtp-v2`.

### Force VSTest Mode (Escape Hatch)
```bash
dotnet test -p:TestingPlatformDotnetTestSupport=false --filter "..."
```

## CI/CD Configuration

### Azure DevOps (.NET 10+)

```yaml
- task: DotNetCoreCLI@2
  displayName: Run Tests
  inputs:
    command: test
    arguments: '--report-trx --results-directory $(Agent.TempDirectory)'
```

### GitHub Actions

```yaml
- name: Test
  run: dotnet test --report-trx --results-directory ./TestResults
```

## MSBuild Properties Reference

```xml
<PropertyGroup>
  <!-- Core MTP -->
  <OutputType>Exe</OutputType>
  <TestingPlatformDotnetTestSupport>true</TestingPlatformDotnetTestSupport>
  <UseMicrosoftTestingPlatform>true</UseMicrosoftTestingPlatform>
  
  <!-- Framework-specific -->
  <EnableMSTestRunner>true</EnableMSTestRunner>       <!-- MSTest -->
  <EnableNUnitRunner>true</EnableNUnitRunner>         <!-- NUnit -->
  <UseMicrosoftTestingPlatformRunner>true</UseMicrosoftTestingPlatformRunner> <!-- xUnit -->
  
  <!-- Entry point control -->
  <GenerateTestingPlatformEntryPoint>false</GenerateTestingPlatformEntryPoint>
  <IsTestingPlatformApplication>false</IsTestingPlatformApplication>
  
  <!-- Ignore exit codes -->
  <TestingPlatformCommandLineArguments>--ignore-exit-code 8</TestingPlatformCommandLineArguments>
</PropertyGroup>
```

See `references/migration-guide.md` for complete VSTest → MTP migration steps.
See `references/extensions.md` for MTP extension packages (TRX, coverage, crash dump).