[![NuGet](https://img.shields.io/nuget/v/ANcpLua.Analyzers?label=NuGet&color=0891B2)](https://www.nuget.org/packages/ANcpLua.Analyzers/)
[![.NET Standard 2.0](https://img.shields.io/badge/.NET%20Standard-2.0-512BD4)](https://dotnet.microsoft.com/platform/dotnet-standard)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

# ANcpLua.Analyzers

Roslyn analyzers for modern C# patterns. Catches common mistakes at compile time with automatic code fixes.

## Installation

```bash
dotnet add package ANcpLua.Analyzers
```

> **Using ANcpLua.NET.Sdk?** This package is auto-injected—no installation needed.

## Rules

| Rule | Severity | Description |
|:-----|:--------:|:------------|
| [AL0001](https://ancplua.mintlify.app/analyzers/rules/AL0001) | ❌ | Don't reassign primary constructor parameters |
| [AL0002](https://ancplua.mintlify.app/analyzers/rules/AL0002) | ⚠️ | Don't repeat negated patterns |
| [AL0003](https://ancplua.mintlify.app/analyzers/rules/AL0003) | ❌ | Don't divide by constant zero |
| [AL0004](https://ancplua.mintlify.app/analyzers/rules/AL0004) | ⚠️ | Use `is` for Span constant comparison |
| [AL0005](https://ancplua.mintlify.app/analyzers/rules/AL0005) | ⚠️ | Use `SequenceEqual` for Span comparison |
| [AL0006](https://ancplua.mintlify.app/analyzers/rules/AL0006) | ⚠️ | Field conflicts with primary constructor param |
| [AL0007](https://ancplua.mintlify.app/analyzers/rules/AL0007) | ⚠️ | `GetSchema` should be explicit interface |
| [AL0008](https://ancplua.mintlify.app/analyzers/rules/AL0008) | ⚠️ | `GetSchema` must return null |
| [AL0009](https://ancplua.mintlify.app/analyzers/rules/AL0009) | ⚠️ | Don't call `IXmlSerializable.GetSchema` |
| [AL0010](https://ancplua.mintlify.app/analyzers/rules/AL0010) | ℹ️ | Type should be partial for generators |
| [AL0011](https://ancplua.mintlify.app/analyzers/rules/AL0011) | ⚠️ | Use `Lock` type instead of `lock` keyword |
| [AL0012](https://ancplua.mintlify.app/analyzers/rules/AL0012) | ⚠️ | Deprecated OTel semantic convention |
| [AL0013](https://ancplua.mintlify.app/analyzers/rules/AL0013) | ℹ️ | Missing telemetry schema URL |
| [AL0014](https://ancplua.mintlify.app/analyzers/rules/AL0014) | ℹ️ | Use `is null` instead of `== null` |
| [AL0015](https://ancplua.mintlify.app/analyzers/rules/AL0015) | ℹ️ | Normalize null-guard style |
| [AL0016](https://ancplua.mintlify.app/analyzers/rules/AL0016) | ℹ️ | Combine declaration with null-check |
| [AL0017](https://ancplua.mintlify.app/analyzers/rules/AL0017) | ⚠️ | Hardcoded version in Directory.Packages.props |
| [AL0018](https://ancplua.mintlify.app/analyzers/rules/AL0018) | ⚠️ | Version.props not imported |
| AL0020-24 | ❌ | ASP.NET Core form binding issues |
| [AL0025](https://ancplua.mintlify.app/analyzers/rules/AL0025) | ⚠️ | Prefer static lambda |
| [AL0026](https://ancplua.mintlify.app/analyzers/rules/AL0026) | ⚠️ | Use `TimeProvider` instead of `DateTime.Now` |
| [AL0027](https://ancplua.mintlify.app/analyzers/rules/AL0027) | ⚠️ | Use `System.Text.Json` instead of Newtonsoft |

**Legend:** ❌ Error · ⚠️ Warning · ℹ️ Info

## Configuration

```editorconfig
[*.cs]
dotnet_diagnostic.AL0001.severity = error
dotnet_diagnostic.AL0014.severity = none
```

## Documentation

**[ancplua.mintlify.app/analyzers](https://ancplua.mintlify.app/analyzers/overview)**

## Related

- [ANcpLua.NET.Sdk](https://github.com/ANcpLua/ANcpLua.NET.Sdk) — MSBuild SDK (auto-injects this analyzer)
- [ANcpLua.Roslyn.Utilities](https://github.com/ANcpLua/ANcpLua.Roslyn.Utilities) — Roslyn helpers

## License

[MIT](LICENSE)
