using System.Runtime.CompilerServices;

namespace ANcpLua.Analyzers;

/// <summary>
///     Base class for all ANcpLua analyzers.
///     Extends <see cref="DiagnosticAnalyzerBase"/> with resource-based rule creation.
/// </summary>
public abstract partial class AlAnalyzer : DiagnosticAnalyzerBase {
    /// <inheritdoc />
    protected sealed override void InitializeCore(AnalysisContext context) => RegisterActions(context);

    /// <summary>Registers analysis actions to be performed during compilation.</summary>
    /// <param name="context">The analysis context to register actions with.</param>
    protected abstract void RegisterActions(AnalysisContext context);

    /// <summary>
    ///     Creates a <see cref="DiagnosticDescriptor"/> using resource-based localization.
    ///     The <c>helpLinkUri</c> is derived from <paramref name="callerFile"/>
    ///     (compiler-supplied via <see cref="CallerFilePathAttribute"/>): the file's
    ///     basename minus the <c>Analyzer</c> suffix and the <c>(AL|Al)NNNN</c>
    ///     prefix becomes the symbolic name in the URL. This relies on the
    ///     "file basename equals class name" convention that the <c>--enforce-ids</c>
    ///     DocsGenerator mode already locks down. URL composition lives in
    ///     <see cref="RuleDocs"/> so the docs generator (which doesn't link against
    ///     <c>ANcpLua.Roslyn.Utilities</c>) can call it without resolving this class.
    /// </summary>
    /// <param name="id">The diagnostic ID (e.g., "AL1208").</param>
    /// <param name="category">The diagnostic category from <see cref="DiagnosticCategories"/>.</param>
    /// <param name="severity">The diagnostic severity.</param>
    /// <param name="isEnabledByDefault">Whether the diagnostic is enabled by default.</param>
    /// <param name="callerFile">Compiler-injected; do not pass.</param>
    /// <returns>A configured <see cref="DiagnosticDescriptor"/>.</returns>
    protected static DiagnosticDescriptor CreateRule(
        string id,
        string category,
        DiagnosticSeverity severity,
        bool isEnabledByDefault = true,
        [CallerFilePath] string callerFile = "") {
        var symbolic = RuleDocs.SymbolicNameFromFile(callerFile);
        return new DiagnosticDescriptor(
            id,
            new LocalizableResourceString($"{id}AnalyzerTitle", Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString($"{id}AnalyzerMessageFormat", Resources.ResourceManager, typeof(Resources)),
            category,
            severity,
            isEnabledByDefault,
            new LocalizableResourceString($"{id}AnalyzerDescription", Resources.ResourceManager, typeof(Resources)),
            RuleDocs.HelpLink(id, symbolic));
    }
}
