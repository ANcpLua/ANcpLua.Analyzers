
namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0124: Detects [AgentTraced] on abstract, extern, or partial definition methods that cannot be intercepted.
/// </summary>
/// <remarks>
///     The [AgentTraced] attribute on a method triggers compile-time interception to create agent spans.
///     Certain method kinds cannot be intercepted by the source generator:
///     <list type="bullet">
///         <item><b>Abstract methods</b>: Have no implementation to intercept</item>
///         <item><b>Extern methods</b>: Implemented externally (P/Invoke)</item>
///         <item><b>Partial definitions</b>: Only the definition half, no body to wrap</item>
///     </list>
///     The [AgentTraced] attribute will be silently ignored on these methods, which may
///     mislead developers into thinking agent spans are being created.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0124NonInterceptableAgentTracedAnalyzer : AlAnalyzer {
    /// <summary>The diagnostic identifier for AL0124.</summary>
    public const string DiagnosticId = "AL0124";

    private const string AgentTracedAttributeFullName = "Qyl.Instrumentation.Instrumentation.AgentTracedAttribute";

    private static readonly DiagnosticDescriptor s_rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.GenAI,
        DiagnosticSeverities.Suggestion);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_rule];

    /// <summary>Registers symbol actions to analyze methods with [AgentTraced] for interceptability.</summary>
    protected override void RegisterActions(AnalysisContext context) {
        context.RegisterCompilationStartAction(compilationContext => {
            if (compilationContext.Compilation.GetTypeByMetadataName(AgentTracedAttributeFullName) is not { } agentTracedType) {
                return;
            }

            compilationContext.RegisterSymbolAction(
                ctx => AnalyzeMethod(ctx, agentTracedType),
                SymbolKind.Method);
        });
    }

    private static void AnalyzeMethod(SymbolAnalysisContext context, INamedTypeSymbol agentTracedType) {
        if (context.Symbol is not IMethodSymbol { IsAbstract: true } and not IMethodSymbol { IsExtern: true } and not IMethodSymbol { IsPartialDefinition: true }) {
            return;
        }

        var method = (IMethodSymbol)context.Symbol;

        if (method.HasAttribute(agentTracedType)) {
            context.ReportDiagnostic(Diagnostic.Create(
                s_rule,
                method.Locations.FirstOrDefault() ?? Location.None,
                method.Name));
        }
    }
}
