# CLAUDE.md

Guidance for Claude Code when working with ANcpLua.Analyzers.

## Project Overview

**ANcpLua.Analyzers** provides Roslyn analyzers and code fixes for C# best practices:
- AL0001-AL0016: Various code quality rules
- Shipped as a NuGet package consumed by ANcpLua.NET.Sdk
- Self-contained (no external dependencies beyond Roslyn)

**Current Version:** See `Directory.Build.props`

## Build & Test

```bash
# Build
dotnet build

# Test
dotnet run --project tests/ANcpLua.Analyzers.Tests/

# Pack
dotnet pack src/ANcpLua.Analyzers.Package/
```

## Automation

**Dependabot handles dependency updates automatically.**

| Automation | Trigger | What Happens |
|------------|---------|--------------|
| **Dependabot** | Weekly | Creates PRs for NuGet/GitHub Actions updates |
| **NuGet Publish** | Tag `v*` | Builds, packs, publishes to NuGet.org |

## Project Structure

```
src/
├── ANcpLua.Analyzers/           # Analyzer implementations
│   ├── Analyzers/               # AL0001-AL0016
│   ├── Core/                    # ALAnalyzer base, DiagnosticIds, etc.
│   └── Internal/                # RoslynExtensions (inlined helpers)
├── ANcpLua.Analyzers.CodeFixes/ # Code fix providers
└── ANcpLua.Analyzers.Package/   # NuGet package (ships both)

tests/
└── ANcpLua.Analyzers.Tests/     # Unit tests (xUnit v3 + MTP v2)
```

## Adding New Rules

1. Create analyzer in `src/ANcpLua.Analyzers/Analyzers/AL00XXAnalyzer.cs`
2. Create code fix in `src/ANcpLua.Analyzers.CodeFixes/CodeFixes/AL00XXCodeFixProvider.cs`
3. Add diagnostic ID to `DiagnosticIds` in `Core/ALAnalyzer.cs`
4. Add tests in `tests/ANcpLua.Analyzers.Tests/`

## Testing Analyzers

Uses Microsoft.CodeAnalysis.Testing infrastructure:

```csharp
await VerifyCS.VerifyAnalyzerAsync(testCode, expectedDiagnostic);
await VerifyCS.VerifyCodeFixAsync(testCode, expectedDiagnostic, fixedCode);
```

## Related

| Repo | Relationship |
|------|--------------|
| `ANcpLua.NET.Sdk` | MSBuild SDK that consumes this analyzer |

## Critical Files

| File | Purpose |
|------|---------|
| `Directory.Build.props` | Version, common settings |
| `Directory.Packages.props` | Central Package Management |
| `src/ANcpLua.Analyzers/Core/ALAnalyzer.cs` | Base class, DiagnosticIds, DiagnosticCategories |
| `src/ANcpLua.Analyzers.Package/*.csproj` | NuGet package definition |
