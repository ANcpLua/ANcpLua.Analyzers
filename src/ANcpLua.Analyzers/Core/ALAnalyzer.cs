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
}
