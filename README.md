# ANcpLua.Analyzers

Unified Roslyn analyzers for C# focusing on code quality, best practices, and OpenTelemetry conventions.

## Installation

```shell
dotnet add package ANcpLua.Analyzers
```

## Rules

### Design Rules

| ID     | Description                                             | Severity |
|--------|---------------------------------------------------------|----------|
| AL0001 | Prohibit reassignment of primary constructor parameters | Error    |
| AL0002 | Don't repeat negated patterns                           | Warning  |
| AL0006 | Field name conflicts with primary constructor parameter | Warning  |
| AL0010 | Type should be partial for source generator support     | Info     |

### Reliability Rules

| ID     | Description                   | Severity |
|--------|-------------------------------|----------|
| AL0003 | Don't divide by constant zero | Error    |

### Usage Rules

| ID     | Description                                              | Severity |
|--------|----------------------------------------------------------|----------|
| AL0004 | Use pattern matching when comparing Span with constants  | Warning  |
| AL0005 | Use SequenceEqual when comparing Span with non-constants | Warning  |
| AL0007 | GetSchema should be explicitly implemented               | Error    |
| AL0008 | GetSchema must return null and not be abstract           | Error    |
| AL0009 | Don't call IXmlSerializable.GetSchema                    | Error    |

### Threading Rules

| ID     | Description                                    | Severity |
|--------|------------------------------------------------|----------|
| AL0011 | Avoid lock keyword on non-Lock types (.NET 9+) | Warning  |

### OpenTelemetry Rules

| ID     | Description                              | Severity |
|--------|------------------------------------------|----------|
| AL0012 | Deprecated semantic convention attribute | Warning  |
| AL0013 | Missing telemetry schema URL             | Info     |

## Refactorings

| ID     | Description                                |
|--------|--------------------------------------------|
| AR0001 | Convert SCREAMING_SNAKE_CASE to PascalCase |

## Code Fixes

Most analyzers come with automatic code fixes:

- AL0002: Simplify pattern negation
- AL0004: Convert to pattern matching
- AL0005: Convert to SequenceEqual
- AL0008: Make GetSchema return null
- AL0010: Add partial modifier
- AL0012: Use modern attribute name

## Requirements

- .NET 10+
- C# 14

## License

MIT