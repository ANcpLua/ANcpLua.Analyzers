
namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL1802: Detects Loom tools that do not declare any required capabilities.
/// </summary>
/// <remarks>
///     <para>
///         Every method annotated with [LoomTool] should also have at least one
///         [RequiresCapability] attribute on the method itself or on its containing type.
///         This enables fine-grained access control and ensures that the tool's permission
///         requirements are visible at compile time.
///     </para>
///     <para>
///         Example of correct usage:
///         <code>
///         [LoomTool]
///         [RequiresCapability("file:read")]
///         public Task&lt;string&gt; ReadFile(string path) { ... }
///         </code>
///     </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al1802ToolMustDeclareCapabilityAnalyzer : AlAnalyzer {
    /// <summary>The diagnostic identifier for AL1802.</summary>
    private const string DiagnosticId = "AL1802";

    private const string RequiresCapabilityAttributeName = "RequiresCapability";

    private static readonly DiagnosticDescriptor s_rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.GenAI,
        DiagnosticSeverities.HiddenByDefault);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_rule];

    /// <summary>Registers symbol actions to analyze methods with [LoomTool] for missing [RequiresCapability].</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterSymbolAction(AnalyzeMethod, SymbolKind.Method);

    private static void AnalyzeMethod(SymbolAnalysisContext context) {
        var method = (IMethodSymbol)context.Symbol;

        if (!Al1800ToAl1802LoomToolAttributes.IsLoomTool(method)) {
            return;
        }

        if (Al1800ToAl1802LoomToolAttributes.HasAttributeOnMethodOrType(method, RequiresCapabilityAttributeName)) {
            return;
        }

        context.ReportDiagnostic(
            s_rule,
            method.Locations.FirstOrDefault() ?? Location.None,
            method.Name);
    }
}
