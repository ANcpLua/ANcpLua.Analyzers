
namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0122: Detects [DuckDbTable] types that are not declared as partial.
/// </summary>
/// <remarks>
///     The DuckDbInsertGenerator source generator creates additional methods on types marked with
///     [DuckDbTable]: AddParameters, MapFromReader, and BuildMultiRowInsertSql. The type must be
///     partial so the generator can add these members. Without partial, the generator silently
///     skips the type and no methods are generated.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0122DuckDbTableMustBePartialAnalyzer : AlAnalyzer {
    /// <summary>The diagnostic identifier for AL0122.</summary>
    public const string DiagnosticId = "AL0122";

    private const string DuckDbTableAttributeFullName = "Qyl.Collector.Storage.DuckDbTableAttribute";

    private static readonly DiagnosticDescriptor s_rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.Design,
        DiagnosticSeverities.RequiredFix);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_rule];

    /// <summary>Registers a compilation-start action to resolve DuckDbTableAttribute.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterCompilationStartAction(OnCompilationStart);

    private static void OnCompilationStart(CompilationStartAnalysisContext context) {
        if (context.Compilation.GetTypeByMetadataName(DuckDbTableAttributeFullName) is not { } attributeType) {
            return;
        }

        context.RegisterSyntaxNodeAction(
            ctx => AnalyzeTypeDeclaration(ctx, attributeType),
            SyntaxKind.ClassDeclaration,
            SyntaxKind.StructDeclaration,
            SyntaxKind.RecordDeclaration,
            SyntaxKind.RecordStructDeclaration);
    }

    private static void AnalyzeTypeDeclaration(SyntaxNodeAnalysisContext context, INamedTypeSymbol attributeType) {
        var typeDeclaration = (TypeDeclarationSyntax)context.Node;

        if (typeDeclaration.Modifiers.Any(SyntaxKind.PartialKeyword)) {
            return;
        }

        if (typeDeclaration.AttributeLists.Count is 0) {
            return;
        }

        if (context.SemanticModel.GetDeclaredSymbol(typeDeclaration, context.CancellationToken) is not { } typeSymbol
            || !typeSymbol.HasAttribute(attributeType)) {
            return;
        }

        context.ReportDiagnostic(s_rule,
            typeDeclaration.Identifier.GetLocation(),
            typeSymbol.Name);
    }
}
