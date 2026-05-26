namespace ANcpLua.Analyzers;

/// <summary>
///     Base class for all ANcpLua analyzers.
///     Extends <see cref="DiagnosticAnalyzerBase"/> with resource-based rule creation.
/// </summary>
public abstract partial class AlAnalyzer : DiagnosticAnalyzerBase {
    /// <summary>
    ///     Base URL for diagnostic help links. Resolves to the rule-band sections of
    ///     <c>README.md</c> in the analyzer repo. The previous <c>ancplua.mintlify.app</c>
    ///     URL was a 404; this matches the Microsoft pattern of "one stable URL per
    ///     shipping NuGet" without requiring an external docs site (the README's
    ///     <c>## Rules</c> section groups all 89 rules by band).
    /// </summary>
    public const string HelpLinkBase =
        "https://github.com/ANcpLua/ANcpLua.Analyzers/blob/main/README.md#rules";

    /// <summary>
    ///     Returns the full help link URL for a specific diagnostic ID. README anchors
    ///     are by band, not by per-rule ID, so every rule resolves to the same
    ///     well-known section; users scan the ID list on the page to find their rule.
    /// </summary>
    public static string HelpLink(string id) => HelpLinkBase;

    /// <inheritdoc />
    protected sealed override void InitializeCore(AnalysisContext context) => RegisterActions(context);

    /// <summary>Registers analysis actions to be performed during compilation.</summary>
    /// <param name="context">The analysis context to register actions with.</param>
    protected abstract void RegisterActions(AnalysisContext context);

    /// <summary>
    ///     Creates a <see cref="DiagnosticDescriptor"/> using resource-based localization.
    /// </summary>
    /// <param name="id">The diagnostic ID (e.g., "AL1208").</param>
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
            HelpLink(id));
    }
}
