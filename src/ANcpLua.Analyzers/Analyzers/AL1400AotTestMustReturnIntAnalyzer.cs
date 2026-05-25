
namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL1400: Methods decorated with [AotTest] or [TrimTest] must return int.
/// </summary>
/// <remarks>
///     <para>
///         The AOT/Trim testing framework expects test methods to return an integer
///         exit code to indicate success or failure. Methods with [AotTest] or [TrimTest]
///         attributes must have a return type of <c>int</c>.
///     </para>
///     <para>
///         This analyzer detects by attribute name string matching ("AotTest" or "TrimTest")
///         to avoid requiring a package reference to ANcpLua.AotTesting.Attributes.
///     </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al1400AotTestMustReturnIntAnalyzer : AlAnalyzer {
    /// <summary>The diagnostic identifier for AL1400.</summary>
    private const string DiagnosticId = "AL1400";

    private const string AotTestAttributeName = "AotTest";
    private const string TrimTestAttributeName = "TrimTest";

    private static readonly DiagnosticDescriptor s_rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.AotTesting,
        DiagnosticSeverity.Error);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_rule];

    /// <inheritdoc />
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterCompilationStartAction(OnCompilationStart);

    private static void OnCompilationStart(CompilationStartAnalysisContext context) {
        if (context.Compilation.GetSpecialType(SpecialType.System_Int32) is not { } intType) {
            return;
        }

        context.RegisterSymbolAction(
            ctx => AnalyzeMethod(ctx, intType),
            SymbolKind.Method);
    }

    private static void AnalyzeMethod(SymbolAnalysisContext context, INamedTypeSymbol intType) {
        if (context.Symbol is not IMethodSymbol method) {
            return;
        }

        var attributeName = method.HasAttributeByShortName(AotTestAttributeName) ? AotTestAttributeName
            : method.HasAttributeByShortName(TrimTestAttributeName) ? TrimTestAttributeName
            : null;

        if (attributeName is null || method.ReturnType.IsEqualTo(intType)) {
            return;
        }

        context.ReportDiagnostic(s_rule, method.Locations[0], method.Name, attributeName, method.ReturnType.ToDisplayString());
    }
}
