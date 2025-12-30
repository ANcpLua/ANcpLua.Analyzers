[![NuGet](https://img.shields.io/nuget/v/ANcpLua.Analyzers?label=NuGet&color=0891B2)](https://www.nuget.org/packages/ANcpLua.Analyzers/)
[![.NET Standard 2.0](https://img.shields.io/badge/.NET%20Standard-2.0-512BD4)](https://dotnet.microsoft.com/platform/dotnet-standard)
[![License](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

# ANcpLua.Analyzers

Roslyn analyzers for C# code quality, focusing on modern .NET patterns and best practices.

## Installation

```shell
dotnet add package ANcpLua.Analyzers
```

Or use [ANcpLua.NET.Sdk](https://github.com/ANcpLua/ANcpLua.NET.Sdk) which includes these analyzers automatically.

## Rules

|Id|Category|Description|Severity|
|--|--------|-----------|:------:|
|[AL0001](https://github.com/ANcpLua/ANcpLua.Analyzers/blob/main/docs/AL0001.md)|Design|Prohibit primary constructor parameter reassignment|Error|
|[AL0002](https://github.com/ANcpLua/ANcpLua.Analyzers/blob/main/docs/AL0002.md)|Design|Don't repeat negated patterns|Warning|
|[AL0003](https://github.com/ANcpLua/ANcpLua.Analyzers/blob/main/docs/AL0003.md)|Reliability|Don't divide by constant zero|Error|
|[AL0004](https://github.com/ANcpLua/ANcpLua.Analyzers/blob/main/docs/AL0004.md)|Usage|Use pattern matching for Span constant comparison|Warning|
|[AL0005](https://github.com/ANcpLua/ANcpLua.Analyzers/blob/main/docs/AL0005.md)|Usage|Use SequenceEqual for Span non-constant comparison|Warning|
|[AL0006](https://github.com/ANcpLua/ANcpLua.Analyzers/blob/main/docs/AL0006.md)|Design|Field name conflicts with primary constructor parameter|Warning|
|[AL0007](https://github.com/ANcpLua/ANcpLua.Analyzers/blob/main/docs/AL0007.md)|Usage|GetSchema should be explicitly implemented|Warning|
|[AL0008](https://github.com/ANcpLua/ANcpLua.Analyzers/blob/main/docs/AL0008.md)|Usage|GetSchema must return null and not be abstract|Warning|
|[AL0009](https://github.com/ANcpLua/ANcpLua.Analyzers/blob/main/docs/AL0009.md)|Usage|Don't call IXmlSerializable.GetSchema|Warning|
|[AL0010](https://github.com/ANcpLua/ANcpLua.Analyzers/blob/main/docs/AL0010.md)|Design|Type should be partial for source generator support|Info|
|[AL0011](https://github.com/ANcpLua/ANcpLua.Analyzers/blob/main/docs/AL0011.md)|Threading|Avoid lock keyword on non-Lock types|Warning|
|[AL0012](https://github.com/ANcpLua/ANcpLua.Analyzers/blob/main/docs/AL0012.md)|OpenTelemetry|Deprecated semantic convention attribute|Warning|
|[AL0013](https://github.com/ANcpLua/ANcpLua.Analyzers/blob/main/docs/AL0013.md)|OpenTelemetry|Missing telemetry schema URL|Info|
|[AL0014](https://github.com/ANcpLua/ANcpLua.Analyzers/blob/main/docs/AL0014.md)|Style|Prefer pattern matching for null and zero comparisons|Info|
|[AL0015](https://github.com/ANcpLua/ANcpLua.Analyzers/blob/main/docs/AL0015.md)|Style|Normalize null-guard style|Info|
|[AL0016](https://github.com/ANcpLua/ANcpLua.Analyzers/blob/main/docs/AL0016.md)|Style|Combine declaration with subsequent null-check|Info|

## Configuration

Configure rule severity in `.editorconfig`:

```ini
[*.cs]
dotnet_diagnostic.AL0001.severity = error
dotnet_diagnostic.AL0014.severity = suggestion
```

See [docs/README.md](docs/README.md) for complete configuration options.

## Related

- [ANcpLua.NET.Sdk](https://github.com/ANcpLua/ANcpLua.NET.Sdk) — MSBuild SDK that includes this analyzer
- [ANcpLua.Roslyn.Utilities](https://github.com/ANcpLua/ANcpLua.Roslyn.Utilities) — Roslyn utilities used by these analyzers
