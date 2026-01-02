# Analyzer Rules

ANcpLua.Analyzers provides 17 diagnostic rules for C# code quality.

## Rule Categories

| Rule | Severity | Description |
|------|----------|-------------|
| [AL0001](AL0001.md) | Error | Prohibit reassignment of primary constructor params |
| [AL0002](AL0002.md) | Warning | Don't repeat negated patterns |
| [AL0003](AL0003.md) | Error | Don't divide by constant zero |
| [AL0004](AL0004.md) | Warning | Use pattern matching for Span constant comparison |
| [AL0005](AL0005.md) | Warning | Use SequenceEqual for Span non-constant comparison |
| [AL0006](AL0006.md) | Warning | Field name conflicts with primary constructor parameter |
| [AL0007](AL0007.md) | Warning | GetSchema should be explicitly implemented |
| [AL0008](AL0008.md) | Warning | GetSchema must return null and not be abstract |
| [AL0009](AL0009.md) | Warning | Don't call IXmlSerializable.GetSchema |
| [AL0010](AL0010.md) | Info | Type should be partial for source generator support |
| [AL0011](AL0011.md) | Warning | Avoid lock keyword on non-Lock types (.NET 9+) |
| [AL0012](AL0012.md) | Warning | Deprecated OTel semantic convention attribute |
| [AL0013](AL0013.md) | Info | Missing telemetry schema URL |
| [AL0014](AL0014.md) | Info | Prefer pattern matching for null/zero comparisons |
| [AL0015](AL0015.md) | Info | Normalize null-guard style |
| [AL0016](AL0016.md) | Info | Combine declaration with subsequent null-check |
| [AL0017](AL0017.md) | Warning | Hardcoded package version in Directory.Packages.props |

## Configuration

All rules can be configured via `.editorconfig`:

```ini
[*.cs]
# Disable a rule
dotnet_diagnostic.AL0001.severity = none

# Change severity
dotnet_diagnostic.AL0014.severity = warning
```
