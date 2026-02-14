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

