namespace ANcpLua.Analyzers;

/// <summary>
///     Base class for all ANcpLua analyzers.
///     Extends <see cref="DiagnosticAnalyzerBase"/> with resource-based rule creation.
/// </summary>
public abstract partial class AlAnalyzer : DiagnosticAnalyzerBase {
    /// <summary>
    ///     Base URL for diagnostic help links. Resolves to per-rule anchors in
    ///     <c>docs/ANcpLua.Analyzers.md</c>, emitted by
    ///     <c>tools/ANcpLua.Analyzers.DocsGenerator</c>. Matches the Microsoft pattern
    ///     of "one stable URL per shipping NuGet" — every analyzer in this package
    ///     points at the same generated rule reference.
    /// </summary>
    public const string HelpLinkBase =
        "https://github.com/ANcpLua/ANcpLua.Analyzers"
        + "/blob/main/docs/ANcpLua.Analyzers.md#";

    /// <summary>
    ///     Returns the full help link URL for a specific diagnostic ID. The id is
    ///     lower-cased to match the anchor GitHub renders from the <c>### ALXXXX</c>
    ///     heading the DocsGenerator emits.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Globalization", "CA1308:Normalize strings to uppercase",
        Justification = "GitHub heading anchors are always lowercased; the URL must match the rendered anchor.")]
    public static string HelpLink(string id) =>
        HelpLinkBase + id.ToLowerInvariant();

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
