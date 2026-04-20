namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     Centralized registry for type-to-extension-method mappings.
///     Single source of truth used by analyzers that suggest TryExtensions or StringComparisonExtensions usage.
/// </summary>
internal static partial class MappingRegistry
{
    /// <summary>
    ///     Maps type names to their TryParse extension method names.
    /// </summary>
    /// <remarks>
    ///     Supports both fully-qualified names (e.g., "System.Int32") and
    ///     C# keyword aliases (e.g., "int").
    /// </remarks>
    private static readonly IReadOnlyDictionary<string, string> TryParseMappings =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["System.Int32"] = "TryParseInt32",
            ["int"] = "TryParseInt32",
            ["System.Int64"] = "TryParseInt64",
            ["long"] = "TryParseInt64",
            ["System.Double"] = "TryParseDouble",
            ["double"] = "TryParseDouble",
            ["System.Decimal"] = "TryParseDecimal",
            ["decimal"] = "TryParseDecimal",
            ["System.Boolean"] = "TryParseBool",
            ["bool"] = "TryParseBool",
            ["System.Guid"] = "TryParseGuid",
            ["System.DateTime"] = "TryParseDateTime",
            ["System.DateTimeOffset"] = "TryParseDateTimeOffset",
            ["System.TimeSpan"] = "TryParseTimeSpan",
            ["System.Byte"] = "TryParseByte",
            ["byte"] = "TryParseByte",
            ["System.Int16"] = "TryParseInt16",
            ["short"] = "TryParseInt16",
            ["System.Single"] = "TryParseSingle",
            ["float"] = "TryParseSingle"
        };

    /// <summary>
    ///     String methods that have StringComparison extension equivalents.
    /// </summary>
    /// <remarks>
    ///     Note: LastIndexOf is NOT included — no extension exists for it.
    ///     Replace is included — ReplaceOrdinal and ReplaceIgnoreCase exist.
    /// </remarks>
    private static readonly HashSet<string> StringComparisonMethods =
        new(StringComparer.Ordinal)
        {
            "Equals",
            "StartsWith",
            "EndsWith",
            "Contains",
            "IndexOf",
            "Replace"
        };

    /// <summary>
    ///     Maps StringComparison enum values to extension method suffixes.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, string> StringComparisonSuffixes =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Ordinal"] = "Ordinal",
            ["OrdinalIgnoreCase"] = "IgnoreCase",
            ["CurrentCulture"] = "CurrentCulture",
            ["CurrentCultureIgnoreCase"] = "CurrentCultureIgnoreCase",
            ["InvariantCulture"] = "InvariantCulture",
            ["InvariantCultureIgnoreCase"] = "InvariantCultureIgnoreCase"
        };

    /// <summary>
    ///     Gets the TryParse extension method name for a given type.
    /// </summary>
    /// <param name="typeName">The fully-qualified type name or C# keyword alias.</param>
    /// <returns>The extension method name, or null if no mapping exists.</returns>
    public static string? GetTryParseExtension(string typeName)
    {
        return TryParseMappings.GetOrNull(typeName);
    }

    /// <summary>
    ///     Checks if a string method has StringComparison extension equivalents.
    /// </summary>
    /// <param name="methodName">The string method name (e.g., "Equals", "Contains").</param>
    /// <returns>True if the method has extension equivalents.</returns>
    public static bool HasStringComparisonExtension(string methodName)
    {
        return StringComparisonMethods.Contains(methodName);
    }

    /// <summary>
    ///     Gets the extension method suffix for a StringComparison value.
    /// </summary>
    /// <param name="comparisonValue">The StringComparison enum member name.</param>
    /// <returns>The suffix to append to the method name, or null if not supported.</returns>
    public static string? GetStringComparisonSuffix(string comparisonValue)
    {
        return StringComparisonSuffixes.GetOrNull(comparisonValue);
    }
}