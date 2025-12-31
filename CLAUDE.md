# CLAUDE.md — ANcpLua.Analyzers

Roslyn analyzers for C# code quality (AL0001-AL0016).

**Current Version:** 1.3.2 | **SDK:** ANcpLua.NET.Sdk 1.3.7

## Rules (AL0001-AL0016)

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
| AL0014 | Info | Prefer pattern matching for null/zero comparisons |
| AL0015 | Info | Normalize null-guard style |
| AL0016 | Info | Combine declaration with subsequent null-check |

## Commands

```bash
dotnet build ANcpLua.Analyzers.slnx
dotnet test --project tests/ANcpLua.Analyzers.Tests/ANcpLua.Analyzers.Tests.csproj
dotnet pack src/ANcpLua.Analyzers/ANcpLua.Analyzers.csproj -c Release -o artifacts -p:PackageId=ANcpLua.Analyzers
```

## Project Structure

```
src/
├── ANcpLua.Analyzers/           # Analyzers (DiagnosticAnalyzer)
└── ANcpLua.Analyzers.CodeFixes/ # Code fixes (CodeFixProvider)

tests/ANcpLua.Analyzers.Tests/   # Unit tests
docs/                            # Per-rule documentation (AL0001.md, etc.)
```

## Key Facts

| Fact | Details |
|------|---------|
| SDK auto-injects this analyzer | Use `PackageId=Dummy` in csproj to prevent cycle |
| CI uses real PackageId | Workflow passes `-p:PackageId=ANcpLua.Analyzers` |
| Both DLLs required | Pack includes Analyzers.dll AND CodeFixes.dll |
| Target: netstandard2.0 | Only ns2.0 assemblies go in nupkg |

## GitHub Actions (Dec 2025)

```yaml
- uses: actions/checkout@v6
- uses: actions/setup-dotnet@v5
- uses: actions/upload-artifact@v6
```
