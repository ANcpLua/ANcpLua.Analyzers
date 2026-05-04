
namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0108: Detects [NoTrace] on a method whose declaring type has no class-level [Traced].
/// </summary>
/// <remarks>
///     <para>
///         The [NoTrace] attribute is used to opt a method out of class-level [Traced]
///         auto-instrumentation. When the declaring type (including base types) does not
///         have [Traced], the [NoTrace] attribute has no effect:
///         <list type="bullet">
///             <item>There is no class-level tracing to opt out of</item>
///             <item>The attribute becomes misleading dead metadata</item>
///             <item>It may confuse developers about the instrumentation scope</item>
///         </list>
///     </para>
///     <para>
///         Example of problematic code:
///         <code>
///         public class OrderService {
///             // No [Traced] on class
///             [NoTrace]  // Redundant!
///             public void HelperMethod() { }
///         }
///         </code>
///     </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0108RedundantNoTraceAnalyzer : AlAnalyzer {
    /// <summary>The diagnostic identifier for AL0108.</summary>
    public const string DiagnosticId = "AL0108";

    private const string TracedAttributeFullName = "Qyl.Instrumentation.Instrumentation.TracedAttribute";
    private const string NoTraceAttributeFullName = "Qyl.Instrumentation.Instrumentation.NoTraceAttribute";

    private static readonly DiagnosticDescriptor s_rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.OpenTelemetry,
        DiagnosticSeverities.HiddenByDefault);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_rule];

    /// <summary>Registers symbol actions to analyze methods for redundant [NoTrace].</summary>
    protected override void RegisterActions(AnalysisContext context) {
        context.RegisterCompilationStartAction(compilationContext => {
            var tracedType = compilationContext.Compilation.GetTypeByMetadataName(TracedAttributeFullName);
            var noTraceType = compilationContext.Compilation.GetTypeByMetadataName(NoTraceAttributeFullName);

            if (tracedType is null || noTraceType is null) {
                return;
            }

            compilationContext.RegisterSymbolAction(
                ctx => AnalyzeMethod(ctx, tracedType, noTraceType),
                SymbolKind.Method);
        });
    }

    private static void AnalyzeMethod(
        SymbolAnalysisContext context,
        INamedTypeSymbol tracedType,
        INamedTypeSymbol noTraceType) {
        var method = (IMethodSymbol)context.Symbol;

        if (method.HasAttribute(noTraceType) && !HasTracedOnType(method.ContainingType, tracedType)) {
            context.ReportDiagnostic(Diagnostic.Create(
                s_rule,
                method.Locations.FirstOrDefault() ?? Location.None,
                method.Name));
        }
    }

    private static bool HasTracedOnType(INamedTypeSymbol? type, INamedTypeSymbol tracedType) {
        for (var current = type; current is not null; current = current.BaseType) {
            if (current.HasAttribute(tracedType)) {
                return true;
            }
        }

        return false;
    }
}
