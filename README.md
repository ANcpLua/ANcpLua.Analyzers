[![NuGet](https://img.shields.io/nuget/v/ANcpLua.Analyzers?label=NuGet&color=0891B2)](https://www.nuget.org/packages/ANcpLua.Analyzers/)
[![.NET Standard 2.0](https://img.shields.io/badge/.NET%20Standard-2.0-512BD4)](https://dotnet.microsoft.com/platform/dotnet-standard)
[![License](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

# ANcpLua.Analyzers

Roslyn analyzers for C# code quality, focusing on modern .NET patterns and best practices.

## Installation

```xml
<PackageReference Include="ANcpLua.Analyzers" Version="1.0.9">
  <PrivateAssets>all</PrivateAssets>
  <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
</PackageReference>
```

Or use [ANcpLua.NET.Sdk](https://github.com/ANcpLua/ANcpLua.NET.Sdk) which includes these analyzers automatically.

## Rules

| ID | Description |
|----|-------------|
| AL0001 | Prohibit primary constructor parameter reassignment |
| AL0002 | Don't repeat negated pattern |
| AL0003 | Don't divide by constant zero |
| AL0004 | Use pattern matching for span constant comparison |
| AL0005 | Use SequenceEqual for span non-constant comparison |
| AL0006 | Field name conflicts with primary constructor parameter |
| AL0007 | IXmlSerializable requires parameterless constructor |
| AL0008 | IXmlSerializable requires XmlSchemaProvider attribute |
| AL0009 | IXmlSerializable requires GetSchema to return null |
| AL0010 | Type should be partial for source generation |
| AL0011 | Use lock keyword instead of object monitor |
| AL0012 | Use recommended attribute for deprecation |
| AL0013 | Missing schema URL in OpenTelemetry instrumentation |
| AL0014 | Prefer pattern matching for null and zero checks |
| AL0015 | Normalize null guard style |
| AL0016 | Combine declaration with null check |

## Configuration

Configure rule severity in `.editorconfig`:

```ini
[*.cs]
dotnet_diagnostic.AL0001.severity = warning
dotnet_diagnostic.AL0014.severity = suggestion
```

## Related

- [ANcpLua.NET.Sdk](https://github.com/ANcpLua/ANcpLua.NET.Sdk) — MSBuild SDK that includes this analyzer
