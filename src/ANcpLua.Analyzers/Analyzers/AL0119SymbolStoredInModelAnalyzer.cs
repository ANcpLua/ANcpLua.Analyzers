
namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0119: Detects fields or properties that store ISymbol-derived types, which breaks incremental generator caching.
/// </summary>
/// <remarks>
///     ISymbol types do not implement value equality. When stored in source generator model types
///     (records, classes used in Select/Where transforms), the incremental pipeline cache is invalidated
///     on every keystroke because the model's Equals returns false even when the data hasn't changed.
///     Extract needed data as strings, display names, or primitive values instead.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0119SymbolStoredInModelAnalyzer : AlAnalyzer {
    /// <summary>The diagnostic identifier for AL0119.</summary>
    public const string DiagnosticId = "AL0119";

    private static readonly DiagnosticDescriptor Rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.RoslynUtilities,
        DiagnosticSeverities.Suggestion);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    /// <summary>Registers a compilation-start action to resolve ISymbol and scan type members.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterCompilationStartAction(OnCompilationStart);

    private static void OnCompilationStart(CompilationStartAnalysisContext context) {
        if (context.Compilation.GetTypeByMetadataName("Microsoft.CodeAnalysis.ISymbol") is not { } iSymbolType) {
            return;
        }

        context.RegisterSymbolAction(
            ctx => AnalyzeNamedType(ctx, iSymbolType),
            SymbolKind.NamedType);
    }

    private static void AnalyzeNamedType(SymbolAnalysisContext context, INamedTypeSymbol iSymbolType) {
        var namedType = (INamedTypeSymbol)context.Symbol;

        if (namedType.TypeKind is not (TypeKind.Class or TypeKind.Struct)) {
            return;
        }

        foreach (var member in namedType.GetMembers()) {
            if (member is not IFieldSymbol { IsImplicitlyDeclared: false } and not IPropertySymbol { IsImplicitlyDeclared: false }) {
                continue;
            }

            var memberType = member is IFieldSymbol field ? field.Type : ((IPropertySymbol)member).Type;

            if (IsOrContainsSymbolType(memberType, iSymbolType)) {
                context.ReportDiagnostic(Diagnostic.Create(
                    Rule,
                    member.Locations.FirstOrDefault() ?? Location.None,
                    member.Name,
                    memberType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
            }
        }
    }

    private static bool IsOrContainsSymbolType(ITypeSymbol type, INamedTypeSymbol iSymbolType) {
        if (IsSymbolType(type, iSymbolType)) {
            return true;
        }

        if (type is INamedTypeSymbol { IsGenericType: true } namedType) {
            foreach (var typeArg in namedType.TypeArguments) {
                if (IsSymbolType(typeArg, iSymbolType)) {
                    return true;
                }
            }
        }

        if (type is IArrayTypeSymbol arrayType) {
            return IsSymbolType(arrayType.ElementType, iSymbolType);
        }

        return false;
    }

    private static bool IsSymbolType(ITypeSymbol type, INamedTypeSymbol iSymbolType) =>
        type.IsEqualTo(iSymbolType) || type.AllInterfaces.Any(i => i.IsEqualTo(iSymbolType));
}
