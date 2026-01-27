[![NuGet](https://img.shields.io/nuget/v/ANcpLua.Analyzers?label=NuGet&color=0891B2)](https://www.nuget.org/packages/ANcpLua.Analyzers/)
[![.NET Standard 2.0](https://img.shields.io/badge/.NET%20Standard-2.0-512BD4)](https://dotnet.microsoft.com/platform/dotnet-standard)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

# ANcpLua.Analyzers

Roslyn analyzers for modern C# patterns. Catches common mistakes at compile time with automatic code fixes.

## Installation

```bash
dotnet add package ANcpLua.Analyzers
```

> **Using ANcpLua.NET.Sdk?** This package is auto-injected - no installation needed.

## Rules

| Rule                                                          | Severity | Description                                    |
|:--------------------------------------------------------------|:--------:|:-----------------------------------------------|
| [AL0001](https://ancplua.mintlify.app/analyzers/rules/AL0001) |  Error   | Don't reassign primary constructor parameters  |
| [AL0002](https://ancplua.mintlify.app/analyzers/rules/AL0002) | Warning  | Don't repeat negated patterns                  |
| [AL0003](https://ancplua.mintlify.app/analyzers/rules/AL0003) |  Error   | Don't divide by constant zero                  |
| [AL0004](https://ancplua.mintlify.app/analyzers/rules/AL0004) | Warning  | Use `is` for Span constant comparison          |
| [AL0005](https://ancplua.mintlify.app/analyzers/rules/AL0005) | Warning  | Use `SequenceEqual` for Span comparison        |
| [AL0006](https://ancplua.mintlify.app/analyzers/rules/AL0006) | Warning  | Field conflicts with primary constructor param |
| [AL0007](https://ancplua.mintlify.app/analyzers/rules/AL0007) | Warning  | `GetSchema` should be explicit interface       |
| [AL0008](https://ancplua.mintlify.app/analyzers/rules/AL0008) | Warning  | `GetSchema` must return null                   |
| [AL0009](https://ancplua.mintlify.app/analyzers/rules/AL0009) | Warning  | Don't call `IXmlSerializable.GetSchema`        |
| [AL0010](https://ancplua.mintlify.app/analyzers/rules/AL0010) |   Info   | Type should be partial for generators          |
| [AL0011](https://ancplua.mintlify.app/analyzers/rules/AL0011) | Warning  | Use `Lock` type instead of `lock` keyword      |
| [AL0012](https://ancplua.mintlify.app/analyzers/rules/AL0012) | Warning  | Deprecated OTel semantic convention            |
| [AL0013](https://ancplua.mintlify.app/analyzers/rules/AL0013) |   Info   | Missing telemetry schema URL                   |
| [AL0014](https://ancplua.mintlify.app/analyzers/rules/AL0014) | Warning  | Use `is null` instead of `== null`             |
| [AL0015](https://ancplua.mintlify.app/analyzers/rules/AL0015) |   Info   | Normalize null-guard style                     |
| [AL0016](https://ancplua.mintlify.app/analyzers/rules/AL0016) |   Info   | Combine declaration with null-check            |
| [AL0017](https://ancplua.mintlify.app/analyzers/rules/AL0017) | Warning  | Hardcoded version in Directory.Packages.props  |
| [AL0018](https://ancplua.mintlify.app/analyzers/rules/AL0018) | Warning  | Version.props not imported                     |
| [AL0019](https://ancplua.mintlify.app/analyzers/rules/AL0019) | Warning  | Undefined version variable                     |
| AL0020-24                                                     |  Error   | ASP.NET Core form binding issues               |
| [AL0025](https://ancplua.mintlify.app/analyzers/rules/AL0025) | Warning  | Prefer static lambda                           |
| [AL0026](https://ancplua.mintlify.app/analyzers/rules/AL0026) | Warning  | Use `TimeProvider` instead of `DateTime.Now`   |
| [AL0027](https://ancplua.mintlify.app/analyzers/rules/AL0027) | Warning  | Use `System.Text.Json` instead of Newtonsoft   |
| [AL0028](https://ancplua.mintlify.app/analyzers/rules/AL0028) |   Info   | Use `IsEqualTo` for symbol comparison          |
| [AL0029](https://ancplua.mintlify.app/analyzers/rules/AL0029) |   Info   | Use `HasAttribute` extension                   |
| [AL0030](https://ancplua.mintlify.app/analyzers/rules/AL0030) |   Info   | Use `Implements`/`InheritsFrom` extensions     |
| [AL0031](https://ancplua.mintlify.app/analyzers/rules/AL0031) |   Info   | Use `IsMethodNamed`/`TryGetConstantValue`      |
| [AL0032](https://ancplua.mintlify.app/analyzers/rules/AL0032) |   Info   | Use `OrEmpty()` extension                      |
| [AL0033](https://ancplua.mintlify.app/analyzers/rules/AL0033) |   Info   | Use `ToImmutableArrayOrEmpty()` extension      |
| [AL0034](https://ancplua.mintlify.app/analyzers/rules/AL0034) |   Info   | Use `WhereNotNull()` extension                 |
| [AL0035](https://ancplua.mintlify.app/analyzers/rules/AL0035) |   Info   | Use `GetFullyQualifiedName`/`GetMetadataName`  |
| [AL0036](https://ancplua.mintlify.app/analyzers/rules/AL0036) | Warning  | Use `Guard.NotNull()` instead of throw pattern |
| [AL0037](https://ancplua.mintlify.app/analyzers/rules/AL0037) | Warning  | Use `TryParseInt32()` etc. extensions          |
| [AL0038](https://ancplua.mintlify.app/analyzers/rules/AL0038) | Warning  | Use `GetOrNull()`/`GetOrDefault()` extensions  |
| [AL0039](https://ancplua.mintlify.app/analyzers/rules/AL0039) | Warning  | Use `EqualsIgnoreCase()` etc. extensions       |
| [AL0040](https://ancplua.mintlify.app/analyzers/rules/AL0040) | Warning  | Use `GetConstructorArgument<T>()` extensions   |
| [AL0041](https://ancplua.mintlify.app/analyzers/rules/AL0041) |  Error   | `[AotTest]`/`[TrimTest]` must return `int`     |
| [AL0042](https://ancplua.mintlify.app/analyzers/rules/AL0042) | Warning  | `[AotTest]`/`[TrimTest]` should return 100     |
| [AL0043](https://ancplua.mintlify.app/analyzers/rules/AL0043) | Warning  | `[TrimSafe]` violates trim safety              |
| [AL0044](https://ancplua.mintlify.app/analyzers/rules/AL0044) | Warning  | `[AotSafe]` violates AOT safety                |

**Legend:** Error = build error, Warning = build warning, Info = IDE only

## Code Fixes

Automatic fixes available for: AL0002, AL0004, AL0005, AL0008, AL0010, AL0011, AL0012, AL0014, AL0015, AL0016, AL0025,
AL0026, AL0027, AL0028, AL0029, AL0030, AL0031

## Configuration

```editorconfig
[*.cs]
dotnet_diagnostic.AL0001.severity = error
dotnet_diagnostic.AL0014.severity = none
```

## Documentation

**[ancplua.mintlify.app/analyzers](https://ancplua.mintlify.app/analyzers/overview)**

## Related

- [ANcpLua.NET.Sdk](https://github.com/ANcpLua/ANcpLua.NET.Sdk) - MSBuild SDK (auto-injects this analyzer)
- [ANcpLua.Roslyn.Utilities](https://github.com/ANcpLua/ANcpLua.Roslyn.Utilities) - Roslyn helpers

## License

[MIT](LICENSE)