
namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0123: Detects multiple [DuckDbColumn] properties on the same type with duplicate Ordinal values.
/// </summary>
/// <remarks>
///     Each property in a [DuckDbTable] type that has a [DuckDbColumn] attribute with an explicit Ordinal
///     value must have a unique ordinal. Duplicate ordinals cause non-deterministic column ordering in the
///     generated INSERT SQL, which can lead to data corruption when column types differ.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0123DuckDbColumnConflictingOrdinalAnalyzer : AlAnalyzer {
    /// <summary>The diagnostic identifier for AL0123.</summary>
    public const string DiagnosticId = "AL0123";

    private const string DuckDbColumnAttributeFullName = "Qyl.Collector.Storage.DuckDbColumnAttribute";

    private static readonly DiagnosticDescriptor Rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.Design,
        DiagnosticSeverities.Suggestion);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    /// <summary>Registers a compilation-start action to resolve DuckDbColumnAttribute.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterCompilationStartAction(OnCompilationStart);

    private static void OnCompilationStart(CompilationStartAnalysisContext context) {
        if (context.Compilation.GetTypeByMetadataName(DuckDbColumnAttributeFullName) is not { } attributeType) {
            return;
        }

        context.RegisterSymbolAction(
            ctx => AnalyzeNamedType(ctx, attributeType),
            SymbolKind.NamedType);
    }

    private static void AnalyzeNamedType(SymbolAnalysisContext context, INamedTypeSymbol attributeType) {
        var namedType = (INamedTypeSymbol)context.Symbol;

        if (namedType.TypeKind is not (TypeKind.Class or TypeKind.Struct)) {
            return;
        }

        // Collect properties with [DuckDbColumn(Ordinal = N)]
        var ordinalMap = new Dictionary<int, (string Name, Location Location)>();

        foreach (var member in namedType.GetMembers()) {
            if (member is not IPropertySymbol property) {
                continue;
            }

            foreach (var attr in property.GetAttributes()) {
                if (!attr.AttributeClass.IsEqualTo(attributeType)) {
                    continue;
                }

                foreach (var namedArg in attr.NamedArguments) {
                    if (namedArg.Key is not "Ordinal" || namedArg.Value.Value is not int ordinal) {
                        continue;
                    }

                    var location = property.Locations.FirstOrDefault() ?? Location.None;

                    if (ordinalMap.TryGetValue(ordinal, out var existing)) {
                        context.ReportDiagnostic(Diagnostic.Create(
                            Rule,
                            location,
                            existing.Name,
                            property.Name,
                            namedType.Name,
                            ordinal));
                    } else {
                        ordinalMap[ordinal] = (property.Name, location);
                    }
                }
            }
        }
    }
}
