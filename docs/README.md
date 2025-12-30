# ANcpLua.Analyzers Rules

|Id|Category|Description|Severity|Is enabled|Code fix|
|--|--------|-----------|:------:|:--------:|:------:|
|[AL0001](AL0001.md)|Design|Prohibit reassignment of primary constructor parameters|<span title='Error'>❌</span>|✔️|❌|
|[AL0002](AL0002.md)|Design|Don't repeat negated patterns|<span title='Warning'>⚠️</span>|✔️|❌|
|[AL0003](AL0003.md)|Reliability|Don't divide by constant zero|<span title='Error'>❌</span>|✔️|❌|
|[AL0004](AL0004.md)|Usage|Use pattern matching for Span constant comparison|<span title='Warning'>⚠️</span>|✔️|✔️|
|[AL0005](AL0005.md)|Usage|Use SequenceEqual for Span non-constant comparison|<span title='Warning'>⚠️</span>|✔️|✔️|
|[AL0006](AL0006.md)|Design|Field name conflicts with primary constructor parameter|<span title='Warning'>⚠️</span>|✔️|❌|
|[AL0007](AL0007.md)|Usage|GetSchema should be explicitly implemented|<span title='Warning'>⚠️</span>|✔️|❌|
|[AL0008](AL0008.md)|Usage|GetSchema must return null and not be abstract|<span title='Warning'>⚠️</span>|✔️|❌|
|[AL0009](AL0009.md)|Usage|Don't call IXmlSerializable.GetSchema|<span title='Warning'>⚠️</span>|✔️|❌|
|[AL0010](AL0010.md)|Design|Type should be partial for source generator support|<span title='Info'>ℹ️</span>|❌|✔️|
|[AL0011](AL0011.md)|Threading|Avoid lock keyword on non-Lock types|<span title='Warning'>⚠️</span>|✔️|❌|
|[AL0012](AL0012.md)|OpenTelemetry|Deprecated semantic convention attribute|<span title='Warning'>⚠️</span>|✔️|❌|
|[AL0013](AL0013.md)|OpenTelemetry|Missing telemetry schema URL|<span title='Info'>ℹ️</span>|✔️|❌|
|[AL0014](AL0014.md)|Style|Prefer pattern matching for null and zero comparisons|<span title='Info'>ℹ️</span>|✔️|✔️|
|[AL0015](AL0015.md)|Style|Normalize null-guard style|<span title='Info'>ℹ️</span>|✔️|✔️|
|[AL0016](AL0016.md)|Style|Combine declaration with subsequent null-check|<span title='Info'>ℹ️</span>|✔️|✔️|

# .editorconfig - default values

```editorconfig
# AL0001: Prohibit reassignment of primary constructor parameters
dotnet_diagnostic.AL0001.severity = error

# AL0002: Don't repeat negated patterns
dotnet_diagnostic.AL0002.severity = warning

# AL0003: Don't divide by constant zero
dotnet_diagnostic.AL0003.severity = error

# AL0004: Use pattern matching for Span constant comparison
dotnet_diagnostic.AL0004.severity = warning

# AL0005: Use SequenceEqual for Span non-constant comparison
dotnet_diagnostic.AL0005.severity = warning

# AL0006: Field name conflicts with primary constructor parameter
dotnet_diagnostic.AL0006.severity = warning

# AL0007: GetSchema should be explicitly implemented
dotnet_diagnostic.AL0007.severity = warning

# AL0008: GetSchema must return null and not be abstract
dotnet_diagnostic.AL0008.severity = warning

# AL0009: Don't call IXmlSerializable.GetSchema
dotnet_diagnostic.AL0009.severity = warning

# AL0010: Type should be partial for source generator support
dotnet_diagnostic.AL0010.severity = none

# AL0011: Avoid lock keyword on non-Lock types
dotnet_diagnostic.AL0011.severity = warning

# AL0012: Deprecated semantic convention attribute
dotnet_diagnostic.AL0012.severity = warning

# AL0013: Missing telemetry schema URL
dotnet_diagnostic.AL0013.severity = suggestion

# AL0014: Prefer pattern matching for null and zero comparisons
dotnet_diagnostic.AL0014.severity = suggestion

# AL0015: Normalize null-guard style
dotnet_diagnostic.AL0015.severity = suggestion

# AL0016: Combine declaration with subsequent null-check
dotnet_diagnostic.AL0016.severity = suggestion
```
