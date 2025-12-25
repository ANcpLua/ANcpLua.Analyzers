namespace ANcpLua.Analyzers.Core;

/// <summary>
///     Central registry of all diagnostic IDs following Roslyn naming conventions.
/// </summary>
public static class DiagnosticIds
{
    // ==========================================================================
    // Primary Constructor Analyzers (AL0001-AL0006)
    // ==========================================================================

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

    // ==========================================================================
    // IXmlSerializable Analyzers (AL0007-AL0009)
    // ==========================================================================

    /// <summary>AL0007: GetSchema should be explicitly implemented.</summary>
    public const string GetSchemaShouldBeExplicitlyImplemented = "AL0007";

    /// <summary>AL0008: GetSchema must return null and not be abstract.</summary>
    public const string GetSchemaMustReturnNull = "AL0008";

    /// <summary>AL0009: Don't call IXmlSerializable.GetSchema.</summary>
    public const string DontCallGetSchema = "AL0009";

    // ==========================================================================
    // Source Generator Support (AL0010)
    // ==========================================================================

    /// <summary>AL0010: Type should be partial for source generator support.</summary>
    public const string TypeShouldBePartial = "AL0010";

    // ==========================================================================
    // OpenTelemetry Analyzers (AL0011-AL0013)
    // ==========================================================================

    /// <summary>AL0011: Avoid lock keyword on non-Lock types.</summary>
    public const string AvoidLockKeywordOnNonLockTypes = "AL0011";

    /// <summary>AL0012: Deprecated semantic convention attribute.</summary>
    public const string DeprecatedSemanticConventionAttribute = "AL0012";

    /// <summary>AL0013: Missing telemetry schema URL.</summary>
    public const string MissingTelemetrySchemaUrl = "AL0013";

    // ==========================================================================
    // Refactorings (AR0001+)
    // ==========================================================================

    /// <summary>AR0001: Convert SCREAMING_SNAKE_CASE to PascalCase.</summary>
    public const string SnakeCaseToPascalCase = "AR0001";
}