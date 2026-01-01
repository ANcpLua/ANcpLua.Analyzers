namespace ANcpLua.Analyzers.Core;

/// <summary>
///     Base class for all ANcpLua analyzers.
/// </summary>
public abstract class ALAnalyzer : DiagnosticAnalyzer {
    protected const string HelpLinkBase = "https://github.com/ANcpLua/ANcpLua.Analyzers/blob/main/docs/";

    public sealed override void Initialize(AnalysisContext context) {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        RegisterActions(context);
    }

    protected abstract void RegisterActions(AnalysisContext context);
}

/// <summary>
///     Diagnostic categories for grouping related analyzers.
/// </summary>
public static class DiagnosticCategories {
    public const string Design = "Design";
    public const string Usage = "Usage";
    public const string Reliability = "Reliability";
    public const string Threading = "Threading";
    public const string OpenTelemetry = "OpenTelemetry";
    public const string Style = "Style";
    public const string VersionManagement = "VersionManagement";
}

/// <summary>
///     Central registry of all diagnostic IDs following Roslyn naming conventions.
/// </summary>
public static class DiagnosticIds {
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
}

/// <summary>
///     Extension methods that simplify diagnostic reporting by combining
///     Diagnostic.Create and ReportDiagnostic into a single call.
/// </summary>
internal static class DiagnosticReportingExtensions {
    public static void ReportDiagnostic(
        this SyntaxNodeAnalysisContext context,
        DiagnosticDescriptor descriptor,
        Location location,
        params object[] messageArgs) =>
        context.ReportDiagnostic(Diagnostic.Create(descriptor, location, messageArgs));

    public static void ReportDiagnostic(
        this SyntaxNodeAnalysisContext context,
        DiagnosticDescriptor descriptor,
        Location location) =>
        context.ReportDiagnostic(Diagnostic.Create(descriptor, location));

    public static void ReportDiagnostic(
        this OperationAnalysisContext context,
        DiagnosticDescriptor descriptor,
        Location location,
        params object[] messageArgs) =>
        context.ReportDiagnostic(Diagnostic.Create(descriptor, location, messageArgs));

    public static void ReportDiagnostic(
        this OperationAnalysisContext context,
        DiagnosticDescriptor descriptor,
        Location location) =>
        context.ReportDiagnostic(Diagnostic.Create(descriptor, location));

    public static void ReportDiagnostic(
        this SymbolAnalysisContext context,
        DiagnosticDescriptor descriptor,
        Location location,
        params object[] messageArgs) =>
        context.ReportDiagnostic(Diagnostic.Create(descriptor, location, messageArgs));

    public static void ReportDiagnostic(
        this SymbolAnalysisContext context,
        DiagnosticDescriptor descriptor,
        Location location) =>
        context.ReportDiagnostic(Diagnostic.Create(descriptor, location));
}
