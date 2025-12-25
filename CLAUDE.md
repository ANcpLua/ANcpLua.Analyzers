# CLAUDE.md

Guidance for Claude Code when working with ANcpLua.Analyzers.

## Project Overview

**ANcpLua.Analyzers** provides Roslyn analyzers and code fixes for C# best practices:
- AL0001-AL0016: Various code quality rules
- Shipped as a NuGet package consumed by ANcpLua.NET.Sdk

**Current Version:** See `Directory.Build.props`

## Build & Test

```bash
# Build
dotnet build

# Test
dotnet test

# Pack
dotnet pack src/ANcpLua.Analyzers.Package/
```

## Automation (ZERO MANUAL STEPS)

**Dependabot handles all dependency updates automatically.**

### What's Automated

| Automation | Trigger | What Happens |
|------------|---------|--------------|
| **Dependabot** | Weekly | Creates PRs for NuGet package updates |
| **Roslyn.Utilities updates** | Dependabot | Auto-PR when new version available |

### Dependencies

- `ANcpLua.Roslyn.Utilities` - Roslyn extension methods (NuGet reference)
- `Microsoft.CodeAnalysis.CSharp` - Roslyn APIs

**When Roslyn.Utilities updates:** Dependabot creates PR. Merge it. Done.

## Project Structure

```
src/
├── ANcpLua.Analyzers/           # Analyzer implementations
│   └── Analyzers/               # AL0001-AL0016
├── ANcpLua.Analyzers.CodeFixes/ # Code fix providers
└── ANcpLua.Analyzers.Package/   # NuGet package (ships both)

tests/
├── ANcpLua.Analyzers.Tests/     # Unit tests (MTP v2)
└── ANcpLua.Analyzers.Benchmarks/
```

## Adding New Rules

1. Create analyzer in `src/ANcpLua.Analyzers/Analyzers/AL00XXAnalyzer.cs`
2. Create code fix in `src/ANcpLua.Analyzers.CodeFixes/CodeFixes/AL00XXCodeFixProvider.cs`
3. Add tests in `tests/ANcpLua.Analyzers.Tests/`
4. Update `DiagnosticIds.cs` and `DiagnosticCategories.cs`

## Testing Analyzers

Uses Microsoft.CodeAnalysis.Testing infrastructure:

```csharp
await VerifyCS.VerifyAnalyzerAsync(testCode, expectedDiagnostic);
await VerifyCS.VerifyCodeFixAsync(testCode, expectedDiagnostic, fixedCode);
```

## Related Repos

| Repo | Relationship |
|------|--------------|
| `ANcpLua.Roslyn.Utilities` | Provides Roslyn extension methods |
| `ANcpLua.NET.Sdk` | Consumes this analyzer package |

## Critical Files

| File | Purpose |
|------|---------|
| `Directory.Build.props` | Version, common settings |
| `Directory.Packages.props` | Central Package Management |
| `src/ANcpLua.Analyzers.Package/*.csproj` | NuGet package definition |
