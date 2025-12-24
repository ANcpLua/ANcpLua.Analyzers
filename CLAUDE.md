# CLAUDE.md

Guidance for Claude Code when working with this repository.

## Project Overview

**ANcpLua.Analyzers** provides Roslyn analyzers and code fixes (AL0001-AL0013) that enforce coding standards. Distributed as a NuGet package and also bundled with ANcpLua.NET.Sdk.

**Current Analyzer Rules:**
- AL0001-AL0013: Various code quality and style rules

## Architecture Relationship

```
┌─────────────────────────────────────────────────────────────────────────┐
│                   ANcpLua.Roslyn.Utilities                              │
│                 (Single source of truth)                                 │
└─────────────────────────────────────────────────────────────────────────┘
                              │
                    NuGet reference (.Testing)
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                   ANcpLua.Analyzers (THIS REPO)                         │
│                 (Binary NuGet reference)                                 │
└─────────────────────────────────────────────────────────────────────────┘
```

**Key points:**
- References `ANcpLua.Roslyn.Utilities.Testing` via NuGet for generator/analyzer tests
- Does **NOT** embed source (uses binary reference unlike SDK)
- Independent release cycle from SDK
- Has its own Core utilities (`DiagnosticReportingExtensions`) that are analyzer-specific

**Why binary reference works here:**
- Analyzers run at compile-time with full .NET runtime
- Unlike source generators, analyzers CAN reference NuGet packages
- No need for source embedding transformation

## Build & Test

```bash
# Build
dotnet build ANcpLua.Analyzers.sln

# Run all tests
dotnet test ANcpLua.Analyzers.sln

# Pack NuGet
dotnet pack ANcpLua.Analyzers.sln
```

## Directory Structure

```
src/
├── ANcpLua.Analyzers/           # Main analyzer project
│   ├── Analyzers/               # Analyzer implementations (AL0001-AL0013)
│   ├── CodeFixes/               # Code fix providers
│   └── Core/                    # Analyzer-specific utilities
│       └── DiagnosticReportingExtensions.cs
└── ANcpLua.Analyzers.CodeFixes/ # Code fixes (if separate)

tests/
└── ANcpLua.Analyzers.Tests/     # Test project
    └── Uses ANcpLua.Roslyn.Utilities.Testing
```

## Testing with Roslyn.Utilities.Testing

The test project uses `ANcpLua.Roslyn.Utilities.Testing` for fluent analyzer testing:

```csharp
// Example test pattern
await source.ShouldHaveDiagnostics<MyAnalyzer>(
    expected: [("AL0001", DiagnosticSeverity.Warning, "Expected message")]
);
```

**Available test APIs:**
- `ShouldHaveDiagnostics<T>()` - Verify expected diagnostics
- `ShouldNotHaveDiagnostics<T>()` - Verify no diagnostics
- Fluent assertion extensions via AwesomeAssertions

## Core Utilities (Analyzer-Specific)

Unlike Roslyn.Utilities (shared), these utilities are specific to this analyzer project:

| File | Purpose |
|------|---------|
| `Core/DiagnosticReportingExtensions.cs` | Analyzer-specific diagnostic helpers |

These are NOT shared with Roslyn.Utilities because they're specific to analyzer implementation patterns rather than general Roslyn development.

## Relationship to SDK

- SDK bundles this analyzer package for automatic enforcement
- This repo has independent versioning
- Changes here require SDK version bump to propagate
