
namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0129: Detects Loom tools that do not declare their side effect.
/// </summary>
/// <remarks>
///     <para>
///         Every method annotated with [LoomTool] should also have [ToolSideEffect] on the
///         method itself or on its containing type. This makes the governance surface explicit
///         and enables automated policy enforcement.
///     </para>
///     <para>
///         Example of correct usage:
///         <code>
///         [LoomTool]
///         [ToolSideEffect(ToolSideEffects.ReadsOnly)]
///         public Task&lt;string&gt; ReadFile(string path) { ... }
///         </code>
///     </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0129ToolMustDeclareSideEffectAnalyzer : AlAnalyzer {
    /// <summary>The diagnostic identifier for AL0129.</summary>
    private const string DiagnosticId = "AL0129";

    private const string LoomToolAttributeName = "LoomTool";
    private const string ToolSideEffectAttributeName = "ToolSideEffect";

    private static readonly DiagnosticDescriptor s_rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.GenAI,
        DiagnosticSeverities.HiddenByDefault);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_rule];

    /// <summary>Registers symbol actions to analyze methods with [LoomTool] for missing [ToolSideEffect].</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterSymbolAction(AnalyzeMethod, SymbolKind.Method);

    private static void AnalyzeMethod(SymbolAnalysisContext context) {
        var method = (IMethodSymbol)context.Symbol;

        if (!method.HasAttributeByShortName(LoomToolAttributeName)) {
            return;
        }

        if (HasAttributeOnMethodOrType(method, ToolSideEffectAttributeName)) {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            s_rule,
            method.Locations.FirstOrDefault() ?? Location.None,
            method.Name));
    }

    private static bool HasAttributeOnMethodOrType(IMethodSymbol method, string attributeName) =>
        method.HasAttributeByShortName(attributeName) ||
        method.ContainingType?.HasAttributeByShortName(attributeName) is true;
}
