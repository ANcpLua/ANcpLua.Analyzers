
namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0110: Detects [TracedTag] on parameters with out or ref modifiers.
/// </summary>
/// <remarks>
///     <para>
///         The [TracedTag] attribute records parameter values as span attributes at the
///         start of method execution. Parameters with <c>out</c> or <c>ref</c> modifiers
///         are problematic because:
///         <list type="bullet">
///             <item><b>out parameters</b>: Have no meaningful value at method entry</item>
///             <item><b>ref parameters</b>: May be mutated during execution, making the recorded value misleading</item>
///             <item>The interceptor captures values before the method body executes</item>
///         </list>
///     </para>
///     <para>
///         Example of problematic code:
///         <code>
///         [Traced("MyApp")]
///         public bool TryParse([TracedTag] string input, [TracedTag] out int result) {
///             // 'result' has no value when the tag would be recorded
///         }
///         </code>
///     </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0110TracedTagOnOutRefParameterAnalyzer : AlAnalyzer {
    /// <summary>The diagnostic identifier for AL0110.</summary>
    public const string DiagnosticId = "AL0110";

    private const string TracedTagAttributeFullName = "Qyl.Instrumentation.Instrumentation.TracedTagAttribute";

    private static readonly DiagnosticDescriptor s_rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.OpenTelemetry,
        DiagnosticSeverities.RequiredFix);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_rule];

    /// <summary>Registers symbol actions to analyze method parameters for [TracedTag] on out/ref.</summary>
    protected override void RegisterActions(AnalysisContext context) {
        context.RegisterCompilationStartAction(compilationContext => {
            if (compilationContext.Compilation.GetTypeByMetadataName(TracedTagAttributeFullName) is not { } tracedTagType) {
                return;
            }

            compilationContext.RegisterSymbolAction(
                ctx => AnalyzeMethod(ctx, tracedTagType),
                SymbolKind.Method);
        });
    }

    private static void AnalyzeMethod(SymbolAnalysisContext context, INamedTypeSymbol tracedTagType) {
        var method = (IMethodSymbol)context.Symbol;

        foreach (var param in method.Parameters) {
            if (param.RefKind is RefKind.Out or RefKind.Ref &&
                param.GetAttributes().Any(a => a.AttributeClass.IsEqualTo(tracedTagType))) {
                context.ReportDiagnostic(Diagnostic.Create(
                    s_rule,
                    param.Locations.FirstOrDefault() ?? Location.None,
                    param.Name));
            }
        }
    }
}
