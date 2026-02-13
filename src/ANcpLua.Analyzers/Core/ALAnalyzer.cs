using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ANcpLua.Analyzers.Core;

/// <summary>
///     Base class for all ANcpLua analyzers.
/// </summary>
public abstract partial class AlAnalyzer : DiagnosticAnalyzer {
    /// <summary>Base URL for diagnostic help links.</summary>
    public const string HelpLinkBase = "https://github.com/ANcpLua/ANcpLua.Analyzers#rules";

    /// <summary>Initializes the analyzer and configures execution options.</summary>
    public sealed override void Initialize(AnalysisContext context) {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        RegisterActions(context);
    }

    /// <summary>Registers analysis actions to be performed during compilation.</summary>
    /// <param name="context">The analysis context to register actions with.</param>
    protected abstract void RegisterActions(AnalysisContext context);

    /// <summary>
    ///     Creates a <see cref="DiagnosticDescriptor"/> using resource-based localization.
    /// </summary>
    /// <remarks>
    ///     Resources must follow the naming convention:
    ///     <list type="bullet">
    ///         <item><c>{id}AnalyzerTitle</c> - The diagnostic title</item>
    ///         <item><c>{id}AnalyzerMessageFormat</c> - The message format with placeholders</item>
    ///         <item><c>{id}AnalyzerDescription</c> - The detailed description</item>
    ///     </list>
    /// </remarks>
    /// <param name="id">The diagnostic ID (e.g., "AL0036").</param>
    /// <param name="category">The diagnostic category from <see cref="DiagnosticCategories"/>.</param>
    /// <param name="severity">The diagnostic severity.</param>
    /// <param name="isEnabledByDefault">Whether the diagnostic is enabled by default.</param>
    /// <returns>A configured <see cref="DiagnosticDescriptor"/>.</returns>
    protected static DiagnosticDescriptor CreateRule(
        string id,
        string category,
        DiagnosticSeverity severity,
        bool isEnabledByDefault = true) {
        return new DiagnosticDescriptor(
            id,
            new LocalizableResourceString($"{id}AnalyzerTitle", Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString($"{id}AnalyzerMessageFormat", Resources.ResourceManager, typeof(Resources)),
            category,
            severity,
            isEnabledByDefault,
            new LocalizableResourceString($"{id}AnalyzerDescription", Resources.ResourceManager, typeof(Resources)),
            HelpLinkBase);
    }
}

/// <summary>
///     Standard severity levels for analyzers with documentation on when to use each.
/// </summary>
/// <remarks>
///     <para>
///         <b>IMPORTANT:</b> Avoid using <see cref="DiagnosticSeverity.Info"/> for analyzers
///         that should appear in normal build output. Info-level diagnostics are filtered
///         out by MSBuild by default and won't appear in build output or IDE error lists
///         unless explicitly configured.
///     </para>
///     <para>
///         Use <see cref="DiagnosticSeverity.Warning"/> for suggestions and code improvements.
///         Use <see cref="DiagnosticSeverity.Error"/> only for definite bugs or violations.
///     </para>
/// </remarks>
public static partial class DiagnosticSeverities {
    /// <summary>
    ///     Use for suggestions and code style improvements.
    ///     This appears in normal build output and IDE error lists.
    /// </summary>
    public const DiagnosticSeverity Suggestion = DiagnosticSeverity.Warning;

    /// <summary>
    ///     Use for definite bugs, security issues, or violations that must be fixed.
    /// </summary>
    public const DiagnosticSeverity RequiredFix = DiagnosticSeverity.Error;

    /// <summary>
    ///     Use only for diagnostics that should be hidden by default.
    ///     <b>WARNING:</b> Info-level diagnostics are NOT shown in normal build output!
    ///     Users must explicitly enable them via .editorconfig or MSBuild properties.
    /// </summary>
    public const DiagnosticSeverity HiddenByDefault = DiagnosticSeverity.Info;
}

/// <summary>
///     Diagnostic categories for grouping related analyzers.
/// </summary>
public static partial class DiagnosticCategories {
    /// <summary>Category for design-related diagnostics.</summary>
    public const string Design = "Design";
    /// <summary>Category for API usage diagnostics.</summary>
    public const string Usage = "Usage";
    /// <summary>Category for reliability diagnostics.</summary>
    public const string Reliability = "Reliability";
    /// <summary>Category for threading and synchronization diagnostics.</summary>
    public const string Threading = "Threading";
    /// <summary>Category for OpenTelemetry diagnostics.</summary>
    public const string OpenTelemetry = "OpenTelemetry";
    /// <summary>Category for Generative AI / LLM observability diagnostics.</summary>
    public const string GenAI = "GenAI";
    /// <summary>Category for metrics and measurement diagnostics.</summary>
    public const string Metrics = "Metrics";
    /// <summary>Category for configuration and setup diagnostics.</summary>
    public const string Configuration = "Configuration";
    /// <summary>Category for code style diagnostics.</summary>
    public const string Style = "Style";
    /// <summary>Category for version management diagnostics.</summary>
    public const string VersionManagement = "VersionManagement";
    /// <summary>Category for ASP.NET Core diagnostics.</summary>
    public const string AspNetCore = "ASP.NET Core";
    /// <summary>Category for Roslyn Utilities extension diagnostics.</summary>
    public const string RoslynUtilities = "Roslyn Utilities";
    /// <summary>Category for AOT and Trim testing diagnostics.</summary>
    public const string AotTesting = "AOT Testing";
}

/// <summary>
///     Central registry of all diagnostic IDs following Roslyn naming conventions.
/// </summary>
public static partial class DiagnosticIds {
    /// <summary>AL0001: Prohibit reassignment of primary constructor parameters.</summary>
    public const string ProhibitPrimaryConstructorParameterReassignment = "AL0001";

    /// <summary>AL0002: Don't repeat negated patterns.</summary>
    public const string DontRepeatNegatedPattern = "AL0002";

    /// <summary>AL0003: Don't divide by constant zero.</summary>
    public const string DontDivideByConstantZero = "AL0003";

    /// <summary>AL0004: Use pattern matching for Span constant comparison.</summary>
    public const string UsePatternMatchingForSpanConstantComparison = "AL0004";

    /// <summary>AL0005: Use SequenceEqual for Span non-constant comparison.</summary>
    public const string UseSequenceEqualForSpanNonConstantComparison = "AL0005";

    /// <summary>AL0006: Field name conflicts with primary constructor parameter.</summary>
    public const string FieldNameConflictsWithPrimaryConstructorParameter = "AL0006";

    /// <summary>AL0007: GetSchema should be explicitly implemented.</summary>
    public const string GetSchemaShouldBeExplicitlyImplemented = "AL0007";

    /// <summary>AL0008: GetSchema must return null and not be abstract.</summary>
    public const string GetSchemaMustReturnNull = "AL0008";

    /// <summary>AL0009: Don't call IXmlSerializable.GetSchema.</summary>
    public const string DontCallGetSchema = "AL0009";

    /// <summary>AL0010: Type should be partial for source generator support.</summary>
    public const string TypeShouldBePartial = "AL0010";

    /// <summary>AL0011: Avoid lock keyword on non-Lock types.</summary>
    public const string AvoidLockKeywordOnNonLockTypes = "AL0011";

    /// <summary>AL0012: Deprecated semantic convention attribute.</summary>
    public const string DeprecatedSemanticConventionAttribute = "AL0012";

    /// <summary>AL0013: Missing telemetry schema URL.</summary>
    public const string MissingTelemetrySchemaUrl = "AL0013";

    /// <summary>AR0001: Convert SCREAMING_SNAKE_CASE to PascalCase.</summary>
    public const string SnakeCaseToPascalCase = "AR0001";

    /// <summary>AL0014: Prefer pattern matching for null and zero comparisons.</summary>
    public const string PreferPatternMatchingForNullAndZero = "AL0014";

    /// <summary>AL0015: Normalize null-guard style.</summary>
    public const string NormalizeNullGuardStyle = "AL0015";

    /// <summary>AL0016: Combine declaration with subsequent null-check.</summary>
    public const string CombineDeclarationWithNullCheck = "AL0016";

    // ═══════════════════════════════════════════════════════════════════════════
    // VERSION MANAGEMENT (AL0017-AL0025)
    // These analyzers enforce centralized package version management.
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>AL0017: Hardcoded version in Directory.Packages.props - use $(VariableName).</summary>
    public const string HardcodedPackageVersion = "AL0017";

    /// <summary>AL0018: Version.props not imported.</summary>
    public const string VersionPropsNotImported = "AL0018";

    /// <summary>AL0019: Undefined version variable.</summary>
    public const string UndefinedVersionVariable = "AL0019";

    // ═══════════════════════════════════════════════════════════════════════════
    // FORM BINDING (AL0020-AL0024)
    // These analyzers enforce correct form binding patterns in ASP.NET Core.
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>AL0020: IFormCollection requires explicit [FromForm] attribute.</summary>
    public const string FormCollectionRequiresExplicitAttribute = "AL0020";

    /// <summary>AL0021: Multiple structured form sources not allowed.</summary>
    public const string MultipleStructuredFormSources = "AL0021";

    /// <summary>AL0022: Cannot mix IFormCollection with [FromForm] DTO.</summary>
    public const string MixedFormCollectionAndDto = "AL0022";

    /// <summary>AL0023: Unsupported [FromForm] type.</summary>
    public const string UnsupportedFormType = "AL0023";

    /// <summary>AL0024: [FromForm] and [FromBody] conflict.</summary>
    public const string FormAndBodyConflict = "AL0024";

    // ═══════════════════════════════════════════════════════════════════════════
    // PERFORMANCE (AL0025+)
    // These analyzers suggest performance improvements.
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>AL0025: Anonymous function can be made static.</summary>
    public const string PreferStaticLambda = "AL0025";

    // ═══════════════════════════════════════════════════════════════════════════
    // BANNED APIS (AL0026+)
    // These analyzers flag usage of deprecated or problematic APIs.
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>AL0026: Avoid DateTime time accessors - use TimeProvider instead.</summary>
    public const string AvoidDateTimeNow = "AL0026";

    /// <summary>AL0027: Avoid legacy JSON library - use System.Text.Json instead.</summary>
    public const string AvoidNewtonsoftJson = "AL0027";

    // ═══════════════════════════════════════════════════════════════════════════
    // ROSLYN UTILITIES (AL0028-AL0031)
    // These analyzers suggest using ANcpLua.Roslyn.Utilities extension methods.
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>AL0028: Use IsEqualTo instead of SymbolEqualityComparer.Equals.</summary>
    public const string UseIsEqualTo = "AL0028";

    /// <summary>AL0029: Use HasAttribute instead of GetAttributes() patterns.</summary>
    public const string UseHasAttribute = "AL0029";

    /// <summary>AL0030: Use Implements/InheritsFrom instead of type hierarchy loops.</summary>
    public const string UseTypeHierarchyExtensions = "AL0030";

    /// <summary>AL0031: Use IsMethodNamed/TryGetConstantValue instead of verbose patterns.</summary>
    public const string UseOperationExtensions = "AL0031";

    /// <summary>AL0032: Use OrEmpty() instead of null-coalescing with empty collections.</summary>
    public const string UseOrEmpty = "AL0032";

    /// <summary>AL0033: Use ToImmutableArrayOrEmpty() instead of null-conditional with fallback.</summary>
    public const string UseToImmutableArrayOrEmpty = "AL0033";

    /// <summary>AL0034: Use WhereNotNull() instead of Where with null check.</summary>
    public const string UseWhereNotNull = "AL0034";

    /// <summary>AL0035: Use GetFullyQualifiedName/GetMetadataName() instead of ToDisplayString with format.</summary>
    public const string UseToDisplayStringExtensions = "AL0035";

    /// <summary>AL0036: Use Guard.NotNull instead of ?? throw new ArgumentNullException.</summary>
    public const string UseGuardNotNull = "AL0036";

    /// <summary>AL0037: Use TryParse extensions instead of verbose TryParse patterns.</summary>
    public const string UseTryParseExtensions = "AL0037";

    /// <summary>AL0038: Use GetOrNull instead of TryGetValue patterns.</summary>
    public const string UseGetOrNull = "AL0038";

    /// <summary>AL0039: Use StringComparison extensions for clearer intent.</summary>
    public const string UseStringComparisonExtensions = "AL0039";

    /// <summary>AL0040: Use attribute argument extraction extensions.</summary>
    public const string UseAttributeExtensions = "AL0040";

    // ═══════════════════════════════════════════════════════════════════════════
    // AOT/TRIM TESTING (AL0041-AL0044)
    // These analyzers enforce correct usage of AOT/Trim testing attributes.
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>AL0041: Method with [AotTest] or [TrimTest] must return int.</summary>
    public const string AotTestMustReturnInt = "AL0041";

    /// <summary>AL0042: [AotTest]/[TrimTest] method should return 100 on success.</summary>
    public const string AotTestExitCode100 = "AL0042";

    /// <summary>AL0043: [TrimSafe] code must not call methods with [RequiresUnreferencedCode].</summary>
    public const string TrimSafeViolation = "AL0043";

    /// <summary>AL0044: [AotSafe] code must not call methods with [RequiresDynamicCode].</summary>
    public const string AotSafeViolation = "AL0044";

    /// <summary>AL0045: Use Guard.NotNullOrEmpty instead of if (string.IsNullOrEmpty) throw.</summary>
    public const string UseGuardNotNullOrEmpty = "AL0045";

    /// <summary>AL0046: Use Guard.NotNullOrWhiteSpace instead of if (string.IsNullOrWhiteSpace) throw.</summary>
    public const string UseGuardNotNullOrWhiteSpace = "AL0046";

    /// <summary>AL0047: Use Guard.NotZero instead of if (x == 0) throw ArgumentOutOfRangeException.</summary>
    public const string UseGuardNotZero = "AL0047";

    /// <summary>AL0048: Use Guard.NotNegative instead of if (x &lt; 0) throw ArgumentOutOfRangeException.</summary>
    public const string UseGuardNotNegative = "AL0048";

    /// <summary>AL0049: Use Guard.Positive instead of if (x &lt;= 0) throw ArgumentOutOfRangeException.</summary>
    public const string UseGuardPositive = "AL0049";

    /// <summary>AL0050: Use Guard.NotEmpty instead of if (guid == Guid.Empty) throw.</summary>
    public const string UseGuardNotEmptyGuid = "AL0050";

    /// <summary>AL0051: Use Guard.DefinedEnum instead of if (!Enum.IsDefined) throw patterns.</summary>
    public const string UseGuardDefinedEnum = "AL0051";

    // ═══════════════════════════════════════════════════════════════════════════
    // AOT/TRIM UNSAFE DETECTION (AL0052-AL0053)
    // These analyzers detect misuse of [AotUnsafe]/[TrimUnsafe] attributes.
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>AL0052: [AotSafe] code must not call [AotUnsafe] code.</summary>
    public const string AotSafeCallsAotUnsafe = "AL0052";

    /// <summary>AL0053: [AotUnsafe] attribute applied to code that doesn't use AOT-incompatible patterns.</summary>
    public const string UnnecessaryAotUnsafe = "AL0053";

    // ═══════════════════════════════════════════════════════════════════════════
    // DIAGNOSTICS ALIGNMENT (AL0054-AL0056)
    // These analyzers validate consistency between Descriptors.cs and documentation.
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>AL0054: Diagnostic defined in Descriptors.cs is missing from diagnostics.md.</summary>
    public const string DiagnosticMissingFromDocs = "AL0054";

    /// <summary>AL0055: Diagnostic defined in Descriptors.cs is missing from AnalyzerReleases.*.md.</summary>
    public const string DiagnosticMissingFromReleaseNotes = "AL0055";

    /// <summary>AL0056: Diagnostic title/severity/category mismatch between Descriptors.cs and documentation.</summary>
    public const string DiagnosticDocumentationMismatch = "AL0056";

    // ═══════════════════════════════════════════════════════════════════════════
    // THREADING (AL0057-AL0060)
    // These analyzers detect common threading anti-patterns.
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>AL0057: Avoid async void methods except for event handlers.</summary>
    public const string AvoidAsyncVoid = "AL0057";

    /// <summary>AL0058: Avoid lock on 'this' - external code can cause deadlocks.</summary>
    public const string AvoidLockOnThis = "AL0058";

    /// <summary>AL0059: Avoid lock on typeof(T) - type objects are globally visible.</summary>
    public const string AvoidLockOnType = "AL0059";

    /// <summary>AL0060: Avoid lock on string literal - interned strings are globally visible.</summary>
    public const string AvoidLockOnString = "AL0060";

    // ═══════════════════════════════════════════════════════════════════════════
    // OPENTELEMETRY/GENAI (AL0061-AL0075)
    // These analyzers enforce OpenTelemetry and GenAI semantic convention compliance.
    // Migrated from qyl.Analyzers QYL001-QYL015.
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>AL0061: Activity/Span missing semantic convention attributes.</summary>
    public const string ActivityMissingSemconv = "AL0061";

    /// <summary>AL0062: Deprecated semantic convention attribute.</summary>
    public const string DeprecatedSemconv = "AL0062";

    /// <summary>AL0063: ActivitySource not registered with AddSource().</summary>
    public const string UnregisteredActivitySource = "AL0063";

    /// <summary>AL0064: GenAI span missing required attributes.</summary>
    public const string GenAiMissingRequiredAttributes = "AL0064";

    /// <summary>AL0065: Use gen_ai.client.token.usage histogram for token metrics.</summary>
    public const string UseTokenUsageHistogram = "AL0065";

    /// <summary>AL0066: GenAI operation name should follow semantic conventions.</summary>
    public const string InvalidGenAiOperationName = "AL0066";

    /// <summary>AL0067: Meter not registered with AddMeter().</summary>
    public const string UnregisteredMeter = "AL0067";

    /// <summary>AL0068: Metric instrument name should follow naming conventions.</summary>
    public const string InvalidMetricName = "AL0068";

    /// <summary>AL0069: ServiceDefaults configuration incomplete.</summary>
    public const string IncompleteServiceDefaults = "AL0069";

    /// <summary>AL0070: Collector endpoint should use OTLP protocol.</summary>
    public const string NonOtlpCollectorEndpoint = "AL0070";

    /// <summary>AL0071: [Meter] class must be partial static.</summary>
    public const string MeterClassMustBePartialStatic = "AL0071";

    /// <summary>AL0072: [Counter]/[Histogram] method must be partial.</summary>
    public const string MetricMethodMustBePartial = "AL0072";

    /// <summary>AL0073: [Traced] attribute must have non-empty ActivitySourceName.</summary>
    public const string TracedActivitySourceNameEmpty = "AL0073";

    /// <summary>AL0074: Deprecated GenAI semantic convention attribute.</summary>
    public const string DeprecatedGenAiAttribute = "AL0074";

    /// <summary>AL0075: High-cardinality tag on metrics (user.id, request.id, etc.).</summary>
    public const string HighCardinalityMetricTag = "AL0075";

    /// <summary>AL0076: AddServiceDefaults called but AddOpenTelemetry missing.</summary>
    public const string MissingOTelConfiguration = "AL0076";

    /// <summary>AL0077: Duplicate instrumentation - method has both auto and manual tracing.</summary>
    public const string DuplicateInstrumentation = "AL0077";

    /// <summary>AL0078: ActivitySource name doesn't follow reverse-DNS naming convention.</summary>
    public const string InvalidActivitySourceName = "AL0078";

    /// <summary>AL0079: Complex async flow detected; manual span recommended.</summary>
    public const string ManualSpanRecommended = "AL0079";

    // ═══════════════════════════════════════════════════════════════════════════
    // RESILIENCE (AL0080+)
    // These analyzers enforce resilience best practices for HTTP clients.
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>AL0080: HTTP client registered without resilience policies.</summary>
    public const string MissingResilienceConfiguration = "AL0080";

    /// <summary>AL0081: Service doesn't expose health check endpoint.</summary>
    public const string MissingHealthChecks = "AL0081";

    // ═══════════════════════════════════════════════════════════════════════════
    // CONFIGURATION (AL0082+)
    // These analyzers detect configuration anti-patterns.
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>AL0082: Hardcoded connection string detected.</summary>
    public const string ConsiderConnectionString = "AL0082";

    /// <summary>AL0083: HTTP endpoint used where HTTPS is expected.</summary>
    public const string InsecureEndpoint = "AL0083";

    /// <summary>AL0084: Direct URL used instead of service discovery.</summary>
    public const string MissingServiceDiscovery = "AL0084";

    /// <summary>AL0085: Attribute value violates OTel semantic convention spec.</summary>
    public const string InvalidAttributeValue = "AL0085";

    /// <summary>AL0087: Prefer constant attribute over string literal for semantic convention names.</summary>
    public const string PreferConstantAttribute = "AL0087";

    /// <summary>AL0086: Attribute set with wrong type (e.g., string instead of int for token counts).</summary>
    public const string IncorrectAttributeType = "AL0086";

    // ═══════════════════════════════════════════════════════════════════════════
    // SENSITIVE DATA DETECTION (AL0088)
    // These analyzers detect potential PII or credentials in telemetry.
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>AL0088: Potential PII or credential detected in span attribute.</summary>
    public const string SensitiveDataInAttribute = "AL0088";

    /// <summary>AL0089: OTEL_EXPORTER_OTLP_ENDPOINT not configured.</summary>
    public const string MissingOtlpConfiguration = "AL0089";

    /// <summary>AL0093: Missing resource attributes (service.name, service.version).</summary>
    public const string MissingResourceAttributes = "AL0093";

    // ═══════════════════════════════════════════════════════════════════════════
    // OPENTELEMETRY EXPORT (AL0090+)
    // These analyzers detect export configuration issues.
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>AL0090: OTLP exporter doesn't have compression enabled.</summary>
    public const string UncompressedExport = "AL0090";

    /// <summary>AL0091: Single-span export configured instead of batch export.</summary>
    public const string BatchExportDisabled = "AL0091";

    // ═══════════════════════════════════════════════════════════════════════════
    // SAMPLING (AL0092)
    // These analyzers suggest telemetry sampling configuration.
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>AL0092: High-volume service without sampling configured.</summary>
    public const string ConsiderSampling = "AL0092";

    // ═══════════════════════════════════════════════════════════════════════════
    // AOT/TRIM GAPS (AL0094-AL0096)
    // These analyzers detect AOT/Trim issues not covered by built-in IL2XXX/IL3XXX.
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>AL0094: Avoid 'dynamic' keyword in AOT-published code.</summary>
    public const string AvoidDynamicKeyword = "AL0094";

    /// <summary>AL0095: Avoid Expression.Compile() in AOT context.</summary>
    public const string AvoidExpressionCompile = "AL0095";

    /// <summary>AL0096: Enable EventSourceSupport for AOT with telemetry.</summary>
    public const string EnableEventSourceSupport = "AL0096";

    // ═══════════════════════════════════════════════════════════════════════════
    // AOT REFLECTION (AL0097-AL0100) — Reserved for ANcpLua.Analyzers.AotReflection
    // These IDs are used by the AotReflection source generator package.
    // Do NOT reuse: AL0097 (InvalidTarget), AL0098 (TypeMustBePartial),
    //               AL0099 (IndexerNotSupported), AL0100 (GenericMethodNotSupported)
    // ═══════════════════════════════════════════════════════════════════════════
}
