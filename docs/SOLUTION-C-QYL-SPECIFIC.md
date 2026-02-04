# Solution C: QYL-Specific Analyzers Design

## Executive Summary

**Key Insight**: Aspire and qyl are fundamentally different domains:
- **Aspire** = App orchestrator (resources, connections, deployments)
- **qyl** = Telemetry/observability platform (traces, metrics, spans)

Aspire's 8 analyzers (ASPIRE006-ASPIRE013) focus on orchestration patterns that have no equivalent in qyl. Instead of mapping Aspire diagnostics, we should design **qyl-native analyzers** that validate qyl's actual APIs.

---

## Part 1: Aspire Diagnostic Applicability Assessment

### Aspire Diagnostics Summary

| ID | Description | Applicable to qyl? |
|----|-------------|-------------------|
| ASPIRE006 | Model names must be valid | NO - orchestration-specific |
| ASPIRE007 | [AspireExport] method must be static | NO - qyl has no exports |
| ASPIRE008 | Invalid AspireExport ID format | NO - qyl has no exports |
| ASPIRE009 | Return type must be ATS-compatible | NO - qyl has no ATS |
| ASPIRE010 | Parameter type must be ATS-compatible | NO - qyl has no ATS |
| ASPIRE011 | AspireUnion requires 2+ types | NO - qyl has no unions |
| ASPIRE012 | AspireUnion type must be ATS-compatible | NO - qyl has no unions |
| ASPIRE013 | Duplicate AspireExport ID | NO - qyl has no exports |

### Conclusion

**Zero Aspire diagnostics apply to qyl.** The domains don't overlap:
- Aspire validates app model topology and export contracts
- qyl validates telemetry instrumentation correctness

---

## Part 2: QYL-Native Diagnostic Design

### Category: QYL Instrumentation (AL0060-AL0079)

These analyzers validate correct usage of qyl's source-generator-driven instrumentation attributes.

---

### AL0060: [Meter] Class Must Be Partial and Static

**Rationale**: The source generator cannot emit implementations for non-partial or instance classes.

```csharp
// ERROR: Not partial
[Meter("MyApp")]
public static class AppMetrics { }  // AL0060

// ERROR: Not static
[Meter("MyApp")]
public partial class AppMetrics { }  // AL0060

// CORRECT
[Meter("MyApp")]
public static partial class AppMetrics { }
```

**Implementation**:
```csharp
// Existing check in MeterAnalyzer.cs line 52-53:
if (!classSyntax.Modifiers.Any(SyntaxKind.PartialKeyword) ||
    !classSyntax.Modifiers.Any(SyntaxKind.StaticKeyword))
    return null;  // Silently ignored - should report diagnostic instead
```

| Property | Value |
|----------|-------|
| Severity | Error |
| Category | QYL Instrumentation |
| Code Fix | Add missing modifiers |

---

### AL0061: [Counter]/[Histogram] Method Must Be Partial

**Rationale**: Metric methods must be partial for generator to implement.

```csharp
[Meter("MyApp")]
public static partial class AppMetrics
{
    // ERROR: Not partial
    [Counter("requests")]
    public static void RecordRequest() { }  // AL0061

    // CORRECT
    [Counter("requests")]
    public static partial void RecordRequest();
}
```

| Property | Value |
|----------|-------|
| Severity | Error |
| Category | QYL Instrumentation |
| Code Fix | Make method partial |

---

### AL0062: [Counter]/[Histogram] Method Must Have No Body

**Rationale**: Partial methods with bodies prevent generator implementation.

```csharp
[Counter("requests")]
public static partial void RecordRequest() { }  // AL0062 - has empty body

[Counter("requests")]
public static partial void RecordRequest();  // CORRECT
```

| Property | Value |
|----------|-------|
| Severity | Error |
| Category | QYL Instrumentation |
| Code Fix | Remove method body |

---

### AL0063: [Meter] Name Must Be Non-Empty

**Rationale**: Empty meter names cause runtime failures.

```csharp
[Meter("")]  // AL0063
public static partial class AppMetrics { }

[Meter("MyApp")]  // CORRECT
public static partial class AppMetrics { }
```

| Property | Value |
|----------|-------|
| Severity | Error |
| Category | QYL Instrumentation |
| Code Fix | None (requires user input) |

---

### AL0064: [Counter]/[Histogram] Name Must Follow OTel Conventions

**Rationale**: OTel metric names should be lowercase with dots/underscores.

```csharp
[Counter("RequestCount")]  // AL0064 - PascalCase
[Counter("request_count")]  // CORRECT (snake_case)
[Counter("request.count")]  // CORRECT (dot notation)
```

Reference: https://opentelemetry.io/docs/specs/semconv/general/naming/

| Property | Value |
|----------|-------|
| Severity | Warning |
| Category | QYL Instrumentation |
| Code Fix | Convert to snake_case |

---

### AL0065: [Traced] ActivitySourceName Must Be Non-Empty

**Rationale**: Empty ActivitySource names cause runtime issues.

```csharp
[Traced("")]  // AL0065
public void DoWork() { }

[Traced("MyApp.Orders")]  // CORRECT
public void DoWork() { }
```

| Property | Value |
|----------|-------|
| Severity | Error |
| Category | QYL Instrumentation |
| Code Fix | None (requires user input) |

---

### AL0066: [Traced] on Class Requires Public Methods

**Rationale**: Class-level [Traced] only instruments public methods. A class with no public methods will have no effect.

```csharp
[Traced("MyApp")]
internal class Service  // AL0066 - warning, no public methods
{
    private void Helper() { }
}
```

| Property | Value |
|----------|-------|
| Severity | Warning |
| Category | QYL Instrumentation |
| Code Fix | None |

---

### AL0067: [Tag] Parameter Requires Non-Empty Name

**Rationale**: Empty tag names are invalid in OTel.

```csharp
[Counter("requests")]
public static partial void Record([Tag("")] string status);  // AL0067

[Counter("requests")]
public static partial void Record([Tag("status")] string status);  // CORRECT
```

| Property | Value |
|----------|-------|
| Severity | Error |
| Category | QYL Instrumentation |
| Code Fix | Use parameter name as default |

---

### AL0068: [OTel] Attribute Name Should Follow Semantic Conventions

**Rationale**: OTel attribute names should follow semconv (e.g., `gen_ai.request.model`).

```csharp
[OTel("RequestModel")]  // AL0068 - not semconv format
public string Model { get; set; }

[OTel("gen_ai.request.model")]  // CORRECT
public string Model { get; set; }
```

| Property | Value |
|----------|-------|
| Severity | Info (hidden by default) |
| Category | QYL Instrumentation |
| Code Fix | Suggest known semconv alternatives |

---

### AL0069: [Histogram] Must Have Value Parameter

**Rationale**: Histograms require a measurement value.

```csharp
[Histogram("request.duration")]
public static partial void RecordDuration();  // AL0069 - no value parameter

[Histogram("request.duration")]
public static partial void RecordDuration(double duration);  // CORRECT
```

| Property | Value |
|----------|-------|
| Severity | Error |
| Category | QYL Instrumentation |
| Code Fix | Add value parameter |

---

### AL0070: Duplicate [Traced] Attribute

**Rationale**: Method-level [Traced] overrides class-level, but having both on method is redundant.

```csharp
[Traced("MyApp")]
public class Service
{
    [Traced("MyApp")]  // AL0070 - redundant with class-level
    public void DoWork() { }
}
```

| Property | Value |
|----------|-------|
| Severity | Info |
| Category | QYL Instrumentation |
| Code Fix | Remove redundant attribute |

---

### AL0071: [NoTrace] Without Class-Level [Traced]

**Rationale**: [NoTrace] only makes sense when class has [Traced].

```csharp
public class Service  // No [Traced]
{
    [NoTrace]  // AL0071 - has no effect
    public void Helper() { }
}
```

| Property | Value |
|----------|-------|
| Severity | Warning |
| Category | QYL Instrumentation |
| Code Fix | Remove [NoTrace] |

---

### AL0072: ActivitySource Registration Not Found

**Rationale**: [Traced] references an ActivitySource that isn't registered.

```csharp
[Traced("UnregisteredSource")]  // AL0072 - no matching ActivitySource.Create()
public void DoWork() { }
```

**Note**: This requires cross-file analysis. Implementation complexity: HIGH.

| Property | Value |
|----------|-------|
| Severity | Warning |
| Category | QYL Instrumentation |
| Code Fix | Generate ActivitySource registration |

---

### AL0073: [Meter] Class Has No Metric Methods

**Rationale**: A [Meter] class without [Counter] or [Histogram] methods is useless.

```csharp
[Meter("MyApp")]
public static partial class AppMetrics  // AL0073 - no metric methods
{
    public static void Helper() { }  // Not a metric method
}
```

| Property | Value |
|----------|-------|
| Severity | Warning |
| Category | QYL Instrumentation |
| Code Fix | None |

---

### AL0074: [TracedTag] on Non-[Traced] Method

**Rationale**: [TracedTag] only works on methods that are traced.

```csharp
public void DoWork([TracedTag("id")] string id) { }  // AL0074 - method not traced
```

| Property | Value |
|----------|-------|
| Severity | Warning |
| Category | QYL Instrumentation |
| Code Fix | Add [Traced] to method/class |

---

### AL0075: Metric Unit Should Use UCUM Format

**Rationale**: OTel recommends UCUM units (e.g., `{request}`, `ms`, `By`).

```csharp
[Counter("requests", Unit = "count")]  // AL0075 - not UCUM
[Counter("requests", Unit = "{request}")]  // CORRECT
```

Reference: https://opentelemetry.io/docs/specs/semconv/general/metrics/

| Property | Value |
|----------|-------|
| Severity | Info |
| Category | QYL Instrumentation |
| Code Fix | Suggest UCUM equivalent |

---

## Part 3: Implementation Priority

### Priority 1 (Must Have) - Build Failures

These prevent the source generator from working:

| ID | Diagnostic | Complexity |
|----|-----------|------------|
| AL0060 | [Meter] must be partial static | Low |
| AL0061 | Metric method must be partial | Low |
| AL0062 | Metric method must have no body | Low |
| AL0063 | [Meter] name non-empty | Low |
| AL0065 | [Traced] ActivitySourceName non-empty | Low |
| AL0067 | [Tag] name non-empty | Low |
| AL0069 | [Histogram] needs value parameter | Medium |

### Priority 2 (Should Have) - Correctness

These prevent runtime issues:

| ID | Diagnostic | Complexity |
|----|-----------|------------|
| AL0066 | [Traced] class needs public methods | Low |
| AL0071 | [NoTrace] without [Traced] | Low |
| AL0073 | [Meter] class has no metrics | Low |
| AL0074 | [TracedTag] on non-traced method | Medium |

### Priority 3 (Nice to Have) - Best Practices

Style and convention enforcement:

| ID | Diagnostic | Complexity |
|----|-----------|------------|
| AL0064 | Metric name conventions | Medium |
| AL0068 | OTel attribute semconv | Medium |
| AL0070 | Duplicate [Traced] | Low |
| AL0075 | UCUM unit format | Medium |

### Priority 4 (Future) - Advanced Analysis

Requires cross-file or compilation-wide analysis:

| ID | Diagnostic | Complexity |
|----|-----------|------------|
| AL0072 | ActivitySource registration | High |

---

## Part 4: Comparison with Aspire Approach

| Aspect | Aspire | qyl |
|--------|--------|-----|
| **Domain** | App orchestration | Telemetry/observability |
| **Primary Concern** | Resource topology | Instrumentation correctness |
| **Attribute Focus** | [AspireExport], [AspireUnion] | [Traced], [Meter], [Counter], [Histogram] |
| **Validation Type** | Export contracts, type compatibility | Generator prerequisites, OTel conventions |
| **Generator Output** | ATS-compatible APIs | Interceptors, meter implementations |

---

## Part 5: File Structure

```
src/ANcpLua.Analyzers/
  Analyzers/
    QYL/
      Al0060MeterClassRequirementsAnalyzer.cs
      Al0061MetricMethodPartialAnalyzer.cs
      Al0063MeterNameAnalyzer.cs
      Al0064MetricNamingConventionAnalyzer.cs
      Al0065TracedActivitySourceAnalyzer.cs
      Al0066TracedClassPublicMethodsAnalyzer.cs
      Al0067TagNameAnalyzer.cs
      Al0068OTelAttributeConventionAnalyzer.cs
      Al0069HistogramValueParameterAnalyzer.cs
      Al0070DuplicateTracedAnalyzer.cs
      Al0071NoTraceWithoutTracedAnalyzer.cs
      Al0073MeterWithoutMetricsAnalyzer.cs
      Al0074TracedTagWithoutTracedAnalyzer.cs
      Al0075MetricUnitFormatAnalyzer.cs
  Core/
    QylWellKnownTypes.cs  # qyl attribute type references
```

---

## Part 6: WellKnownTypes for qyl

Add to `WellKnownTypes.cs`:

```csharp
// qyl.servicedefaults.Instrumentation attributes
QylTracedAttribute,
QylNoTraceAttribute,
QylMeterAttribute,
QylCounterAttribute,
QylHistogramAttribute,
QylTagAttribute,
QylOTelAttribute,
QylTracedTagAttribute,
```

Type metadata:

```csharp
{ WellKnownType.QylTracedAttribute, "Qyl.ServiceDefaults.Instrumentation.TracedAttribute" },
{ WellKnownType.QylNoTraceAttribute, "Qyl.ServiceDefaults.Instrumentation.NoTraceAttribute" },
{ WellKnownType.QylMeterAttribute, "Qyl.ServiceDefaults.Instrumentation.MeterAttribute" },
{ WellKnownType.QylCounterAttribute, "Qyl.ServiceDefaults.Instrumentation.CounterAttribute" },
{ WellKnownType.QylHistogramAttribute, "Qyl.ServiceDefaults.Instrumentation.HistogramAttribute" },
{ WellKnownType.QylTagAttribute, "Qyl.ServiceDefaults.Instrumentation.TagAttribute" },
{ WellKnownType.QylOTelAttribute, "Qyl.ServiceDefaults.Instrumentation.OTelAttribute" },
{ WellKnownType.QylTracedTagAttribute, "Qyl.ServiceDefaults.Instrumentation.TracedTagAttribute" },
```

---

## Conclusion

The qyl-specific approach provides:

1. **Domain-appropriate validation** - Analyzers that match qyl's actual APIs
2. **Generator support** - Errors that prevent silent generator failures
3. **OTel compliance** - Convention enforcement for semantic correctness
4. **Clear error messages** - Users understand what to fix and why

This is **orthogonal to Aspire** - the two analyzer sets could coexist without conflict in a project that uses both Aspire (for orchestration) and qyl (for telemetry).
