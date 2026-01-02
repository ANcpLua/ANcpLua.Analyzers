# ANcpLua.Analyzers

Roslyn analyzers for C# code quality with 17 diagnostic rules (AL0001-AL0017).

## Installation

```bash
dotnet add package ANcpLua.Analyzers
```

Or via PackageReference:

```xml
<PackageReference Include="ANcpLua.Analyzers" Version="1.3.6" PrivateAssets="all" />
```

## Features

- **17 diagnostic rules** covering code quality, patterns, and best practices
- **Code fixes** for most diagnostics with batch fix support
- **Zero configuration** - works out of the box
- **.NET 10 + netstandard2.0** compatible

## Quick Start

After installation, the analyzer automatically runs during build and in your IDE.

```csharp
// AL0001: Primary constructor parameter reassignment
public class Example(int x)
{
    void Bad() => x = 10;  // ⚠️ Diagnostic reported
}

// AL0014: Prefer pattern matching
if (obj == null)  // ⚠️ Use 'obj is null' instead
```

## Documentation

- [Rules Reference](rules/index.md) - All 17 diagnostic rules
- [API Reference](api/index.md) - Source code documentation

## Source Code

[GitHub Repository](https://github.com/ANcpLua/ANcpLua.Analyzers)
