
namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL1502: Detects classes implementing ISourceGenerator instead of IIncrementalGenerator.
/// </summary>
/// <remarks>
///     ISourceGenerator (v1 API) runs the full generation pipeline on every keystroke.
///     IIncrementalGenerator provides caching and incremental updates, running only when
///     inputs actually change. This dramatically improves IDE responsiveness.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al1502UseIncrementalGeneratorAnalyzer : AlAnalyzer {
    /// <summary>The diagnostic identifier for AL1502.</summary>
    private const string DiagnosticId = "AL1502";

    private static readonly DiagnosticDescriptor s_rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.RoslynUtilities,
        DiagnosticSeverities.Suggestion);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_rule];

    /// <summary>Registers a compilation-start action to resolve ISourceGenerator.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterCompilationStartAction(OnCompilationStart);

    private static void OnCompilationStart(CompilationStartAnalysisContext context) {
        if (context.Compilation.GetTypeByMetadataName("Microsoft.CodeAnalysis.ISourceGenerator") is not { } sourceGeneratorType) {
            return;
        }

        context.RegisterSymbolAction(
            ctx => AnalyzeNamedType(ctx, sourceGeneratorType),
            SymbolKind.NamedType);
    }

    private static void AnalyzeNamedType(SymbolAnalysisContext context, INamedTypeSymbol sourceGeneratorType) {
        var namedType = (INamedTypeSymbol)context.Symbol;

        if (namedType.TypeKind is not TypeKind.Class || namedType.IsAbstract) {
            return;
        }

        if (!namedType.Implements(sourceGeneratorType)) {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            s_rule,
            namedType.Locations.FirstOrDefault() ?? Location.None,
            namedType.Name));
    }
}
