
namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0073: Validates [Traced] attribute has non-empty ActivitySourceName.
/// </summary>
/// <remarks>
///     <para>
///         The [Traced] attribute requires a valid ActivitySourceName because:
///         <list type="bullet">
///             <item>The source name identifies where spans originate</item>
///             <item>It must match a registered ActivitySource in the tracing pipeline</item>
///             <item>Empty names prevent proper span correlation and filtering</item>
///         </list>
///     </para>
///     <para>
///         Example of correct usage:
///         <code>
///         [Traced("MyApp.Orders")]  // Valid: descriptive source name
///         public class OrderService { }
///
///         [Traced("")]  // Error: empty source name
///         public class BadService { }
///         </code>
///     </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0073TracedActivitySourceNameAnalyzer : AlAnalyzer {
    /// <summary>The diagnostic identifier for AL0073.</summary>
    public const string DiagnosticId = "AL0073";

    private const string TracedAttributeFullName = "Qyl.Instrumentation.Instrumentation.TracedAttribute";

    private static readonly DiagnosticDescriptor s_rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.OpenTelemetry,
        DiagnosticSeverities.RequiredFix);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_rule];

    /// <summary>Registers symbol actions to analyze types and methods with [Traced] attribute.</summary>
    protected override void RegisterActions(AnalysisContext context) {
        context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
        context.RegisterSymbolAction(AnalyzeMethod, SymbolKind.Method);
    }

    private static void AnalyzeNamedType(SymbolAnalysisContext context) =>
        AnalyzeSymbol(context, (INamedTypeSymbol)context.Symbol);

    private static void AnalyzeMethod(SymbolAnalysisContext context) =>
        AnalyzeSymbol(context, (IMethodSymbol)context.Symbol);

    private static void AnalyzeSymbol(SymbolAnalysisContext context, ISymbol symbol) {
        if (context.Compilation.GetTypeByMetadataName(TracedAttributeFullName) is not { } tracedAttributeType) {
            return;
        }

        foreach (var attribute in symbol.GetAttributes()) {
            if (!attribute.AttributeClass.IsEqualTo(tracedAttributeType)) {
                continue;
            }

            string? activitySourceName = null;

            if (attribute.ConstructorArguments is [{ Value: string ctorArg }, ..]) {
                activitySourceName = ctorArg;
            }

            foreach (var namedArg in attribute.NamedArguments) {
                if (namedArg is { Key: "ActivitySourceName", Value.Value: string namedValue }) {
                    activitySourceName = namedValue;
                    break;
                }
            }

            if (string.IsNullOrWhiteSpace(activitySourceName)) {
                var location = attribute.ApplicationSyntaxReference?.GetSyntax(context.CancellationToken).GetLocation()
                               ?? Location.None;

                context.ReportDiagnostic(Diagnostic.Create(s_rule, location, symbol.Name));
            }
        }
    }
}
