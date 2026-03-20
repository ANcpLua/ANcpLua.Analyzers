namespace ANcpLua.Analyzers;

/// <summary>
///     Base class for all ANcpLua analyzers.
///     Extends <see cref="DiagnosticAnalyzerBase"/> with resource-based rule creation.
/// </summary>
public abstract partial class AlAnalyzer : DiagnosticAnalyzerBase {
    /// <summary>Base URL for diagnostic help links.</summary>
    public const string HelpLinkBase = "https://ancplua.mintlify.app/analyzers/rules/";

    /// <summary>Returns the full help link URL for a specific diagnostic ID.</summary>
    public static string HelpLink(string id) => HelpLinkBase + id;

    /// <inheritdoc />
    protected sealed override void InitializeCore(AnalysisContext context) => RegisterActions(context);

    /// <summary>Registers analysis actions to be performed during compilation.</summary>
    /// <param name="context">The analysis context to register actions with.</param>
    protected abstract void RegisterActions(AnalysisContext context);

    /// <summary>
    ///     Creates a <see cref="DiagnosticDescriptor"/> using resource-based localization.
    /// </summary>
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
            HelpLink(id));
    }
}
