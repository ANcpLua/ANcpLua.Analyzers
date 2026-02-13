## Release 1.13.0

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|------
AL0001 | Design | Error | Prohibit primary constructor parameter reassignment
AL0002 | Design | Warning | Don't repeat negated pattern
AL0003 | Reliability | Error | Don't divide by constant zero
AL0004 | Usage | Warning | Use pattern matching for Span constant comparison
AL0005 | Usage | Warning | Use SequenceEqual for Span non-constant comparison
AL0006 | Design | Warning | Field name conflicts with primary constructor parameter
AL0007 | Usage | Error | GetSchema should be explicitly implemented
AL0008 | Usage | Error | GetSchema must return null
AL0009 | Usage | Error | Don't call GetSchema
AL0010 | Design | Disabled | Type should be partial
AL0011 | Threading | Warning | Avoid lock keyword on non-Lock types
AL0012 | OpenTelemetry | Warning | Deprecated semantic convention attribute
AL0013 | OpenTelemetry | Info | Missing telemetry schema URL
AL0014 | Style | Warning | Prefer pattern matching for null and zero
AL0015 | Style | Info | Normalize null guard style
AL0016 | Style | Info | Combine declaration with null check
AL0017 | VersionManagement | Warning | Hardcoded package version
AL0018 | VersionManagement | Warning | Version.props not imported
AL0019 | VersionManagement | Warning | Undefined version variable
AL0020 | ASP.NET Core | Error | FormCollection requires explicit attribute
AL0021 | ASP.NET Core | Error | Multiple structured form sources
AL0022 | ASP.NET Core | Error | Mixed FormCollection and DTO
AL0023 | ASP.NET Core | Error | Unsupported form type
AL0024 | ASP.NET Core | Error | Form and body conflict
AL0025 | Usage | Warning | Prefer static lambda
AL0026 | Usage | Warning | Avoid DateTime.Now
AL0027 | Usage | Warning | Avoid Newtonsoft.Json
AL0028 | Roslyn Utilities | Warning | Use IsEqualTo
AL0029 | Roslyn Utilities | Warning | Use HasAttribute
AL0030 | Roslyn Utilities | Warning | Use type hierarchy extensions
AL0031 | Roslyn Utilities | Warning | Use operation extensions
AL0032 | Roslyn Utilities | Warning | Use OrEmpty
AL0033 | Roslyn Utilities | Warning | Use ToImmutableArrayOrEmpty
AL0034 | Roslyn Utilities | Warning | Use WhereNotNull
AL0035 | Roslyn Utilities | Warning | Use ToDisplayString extensions
AL0036 | Roslyn Utilities | Warning | Use Guard.NotNull
AL0037 | Roslyn Utilities | Warning | Use TryParse extensions
AL0038 | Roslyn Utilities | Warning | Use GetOrNull
AL0039 | Roslyn Utilities | Warning | Use string comparison extensions
AL0040 | Roslyn Utilities | Warning | Use attribute extensions
AL0041 | AOT Testing | Warning | AotTest must return int
AL0042 | AOT Testing | Warning | AotTest exit code 100
AL0043 | AOT Testing | Warning | Trim-safe violation
AL0044 | AOT Testing | Warning | AOT-safe violation
AL0045 | Roslyn Utilities | Warning | Use Guard.NotNullOrEmpty
AL0046 | Roslyn Utilities | Warning | Use Guard.NotNullOrWhiteSpace
AL0047 | Roslyn Utilities | Warning | Use Guard.NotZero
AL0048 | Roslyn Utilities | Warning | Use Guard.NotNegative
AL0049 | Roslyn Utilities | Warning | Use Guard.Positive
AL0050 | Roslyn Utilities | Warning | Use Guard.NotEmptyGuid
AL0051 | Roslyn Utilities | Warning | Use Guard.DefinedEnum
AL0052 | AOT Testing | Error | AotSafe calls AotUnsafe
AL0053 | AOT Testing | Warning | Unnecessary AotUnsafe
AL0054 | VersionManagement | Warning | Diagnostic missing from docs
AL0055 | VersionManagement | Warning | Diagnostic missing from release notes
AL0056 | VersionManagement | Warning | Diagnostic documentation mismatch
AL0057 | Threading | Warning | Avoid async void
AL0058 | Threading | Warning | Avoid lock on this
AL0059 | Threading | Warning | Avoid lock on Type
AL0060 | Threading | Warning | Avoid lock on string
AL0061 | OpenTelemetry | Warning | Activity missing semantic conventions
AL0062 | OpenTelemetry | Warning | Deprecated semantic convention
AL0063 | OpenTelemetry | Warning | Unregistered ActivitySource
AL0064 | GenAI | Warning | GenAI missing required attributes
AL0065 | GenAI | Warning | Use token usage histogram
AL0066 | GenAI | Warning | Invalid GenAI operation name
AL0067 | Metrics | Warning | Unregistered Meter
AL0068 | Metrics | Warning | Invalid metric name
AL0069 | Configuration | Warning | Incomplete service defaults
AL0070 | Configuration | Warning | Non-OTLP collector endpoint
AL0071 | Metrics | Error | Meter class must be partial static
AL0072 | Metrics | Error | Metric method must be partial
AL0073 | OpenTelemetry | Error | Traced ActivitySource name empty
AL0074 | GenAI | Warning | Deprecated GenAI attribute
AL0075 | Metrics | Warning | High cardinality metric tag
AL0076 | OpenTelemetry | Warning | Missing OTel configuration
AL0077 | OpenTelemetry | Warning | Duplicate instrumentation
AL0078 | OpenTelemetry | Error | Invalid ActivitySource name
AL0079 | OpenTelemetry | Info | Manual span recommended
AL0080 | ASP.NET Core | Warning | Missing resilience configuration
AL0081 | ASP.NET Core | Warning | Missing health checks
AL0082 | Configuration | Info | Consider connection string
AL0083 | Configuration | Warning | Insecure endpoint
AL0084 | ASP.NET Core | Warning | Missing service discovery
AL0085 | OpenTelemetry | Error | Invalid attribute value
AL0086 | OpenTelemetry | Warning | Incorrect attribute type
AL0087 | OpenTelemetry | Info | Prefer constant attribute
AL0088 | OpenTelemetry | Warning | Sensitive data in attribute
AL0089 | OpenTelemetry | Warning | Missing OTLP configuration
AL0090 | OpenTelemetry | Warning | Uncompressed export
AL0091 | OpenTelemetry | Warning | Batch export disabled
AL0092 | OpenTelemetry | Info | Consider sampling
AL0093 | OpenTelemetry | Warning | Missing resource attributes
