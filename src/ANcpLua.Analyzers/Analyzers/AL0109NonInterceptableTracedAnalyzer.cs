
namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0109: Detects [Traced] on abstract, extern, or partial definition methods that cannot be intercepted.
/// </summary>
/// <remarks>
///     <para>
///         The [Traced] attribute on a method triggers compile-time interception to create spans.
///         Certain method kinds cannot be intercepted by the source generator:
///         <list type="bullet">
///             <item><b>Abstract methods</b>: Have no implementation to intercept</item>
///             <item><b>Extern methods</b>: Implemented externally (P/Invoke)</item>
///             <item><b>Partial definitions</b>: Only the definition half, no body to wrap</item>
///         </list>
///     </para>
///     <para>
///         The [Traced] attribute will be silently ignored on these methods, which may
///         mislead developers into thinking spans are being created.
///     </para>
///     <para>
///         Example of problematic code:
///         <code>
///         public abstract class BaseService {
///             [Traced("MyApp")]
///             public abstract Task ProcessAsync();  // Cannot be intercepted!
///         }
///         </code>
///     </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0109NonInterceptableTracedAnalyzer : AlAnalyzer {
    /// <summary>The diagnostic identifier for AL0109.</summary>
    public const string DiagnosticId = "AL0109";

    private const string TracedAttributeFullName = "Qyl.Instrumentation.Instrumentation.TracedAttribute";

    private static readonly DiagnosticDescriptor s_rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.OpenTelemetry,
        DiagnosticSeverities.Suggestion);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_rule];

    /// <summary>Registers symbol actions to analyze methods with [Traced] for interceptability.</summary>
    protected override void RegisterActions(AnalysisContext context) {
        context.RegisterCompilationStartAction(compilationContext => {
            if (compilationContext.Compilation.GetTypeByMetadataName(TracedAttributeFullName) is not { } tracedType) {
                return;
            }

            compilationContext.RegisterSymbolAction(
                ctx => AnalyzeMethod(ctx, tracedType),
                SymbolKind.Method);
        });
    }

    private static void AnalyzeMethod(SymbolAnalysisContext context, INamedTypeSymbol tracedType) {
        if (context.Symbol is not IMethodSymbol { IsAbstract: true } and not IMethodSymbol { IsExtern: true } and not IMethodSymbol { IsPartialDefinition: true }) {
            return;
        }

        var method = (IMethodSymbol)context.Symbol;

        if (method.HasAttribute(tracedType)) {
            context.ReportDiagnostic(Diagnostic.Create(
                s_rule,
                method.Locations.FirstOrDefault() ?? Location.None,
                method.Name));
        }
    }
}
