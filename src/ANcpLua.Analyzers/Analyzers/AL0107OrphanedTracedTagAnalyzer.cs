
namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0107: Detects [TracedTag] on parameters where neither the method nor its declaring type has [Traced].
/// </summary>
/// <remarks>
///     <para>
///         The [TracedTag] attribute is only meaningful when used on parameters of methods that
///         participate in tracing via [Traced]. When neither the method nor its declaring type
///         (including base types) has [Traced], the tag will be silently ignored:
///         <list type="bullet">
///             <item>No span is created, so there is nothing to attach the tag to</item>
///             <item>The attribute becomes dead metadata with no runtime effect</item>
///             <item>It may mislead developers into thinking tags are being recorded</item>
///         </list>
///     </para>
///     <para>
///         Example of problematic code:
///         <code>
///         public class OrderService {
///             // No [Traced] on class or method
///             public void Process([TracedTag] string orderId) { }
///         }
///         </code>
///     </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0107OrphanedTracedTagAnalyzer : AlAnalyzer {
    /// <summary>The diagnostic identifier for AL0107.</summary>
    public const string DiagnosticId = "AL0107";

    private const string TracedAttributeFullName = "Qyl.Instrumentation.Instrumentation.TracedAttribute";
    private const string TracedTagAttributeFullName = "Qyl.Instrumentation.Instrumentation.TracedTagAttribute";

    private static readonly DiagnosticDescriptor s_rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.OpenTelemetry,
        DiagnosticSeverities.Suggestion);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_rule];

    /// <summary>Registers symbol actions to analyze method parameters for orphaned [TracedTag].</summary>
    protected override void RegisterActions(AnalysisContext context) {
        context.RegisterCompilationStartAction(compilationContext => {
            var tracedType = compilationContext.Compilation.GetTypeByMetadataName(TracedAttributeFullName);
            var tracedTagType = compilationContext.Compilation.GetTypeByMetadataName(TracedTagAttributeFullName);

            if (tracedType is null || tracedTagType is null) {
                return;
            }

            compilationContext.RegisterSymbolAction(
                ctx => AnalyzeMethod(ctx, tracedType, tracedTagType),
                SymbolKind.Method);
        });
    }

    private static void AnalyzeMethod(
        SymbolAnalysisContext context,
        INamedTypeSymbol tracedType,
        INamedTypeSymbol tracedTagType) {
        var method = (IMethodSymbol)context.Symbol;

        if (method.HasAttribute(tracedType) || HasTracedOnType(method.ContainingType, tracedType)) {
            return;
        }

        foreach (var param in method.Parameters) {
            if (param.HasAttribute(tracedTagType)) {
                context.ReportDiagnostic(Diagnostic.Create(
                    s_rule,
                    param.Locations.FirstOrDefault() ?? Location.None,
                    param.Name));
            }
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
