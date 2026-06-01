
namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL1204: Suggests using OrEmpty() extension instead of null-coalescing with empty collections.
/// </summary>
/// <remarks>
///     <list type="bullet">
///         <item><c>collection ?? Array.Empty&lt;T&gt;()</c> → <c>collection.OrEmpty()</c></item>
///         <item><c>collection ?? Enumerable.Empty&lt;T&gt;()</c> → <c>collection.OrEmpty()</c></item>
///         <item><c>collection ?? []</c> → <c>collection.OrEmpty()</c></item>
///     </list>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al1204UseOrEmptyAnalyzer : AlAnalyzer {
    /// <summary>The diagnostic identifier for AL1204.</summary>
    public const string DiagnosticId = "AL1204";

    private const string EnumerableExtensionsMetadataName = "ANcpLua.Roslyn.Utilities.EnumerableExtensions";

    private static readonly DiagnosticDescriptor s_rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.RoslynUtilities,
        DiagnosticSeverity.Info);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_rule];

    /// <summary>Registers syntax or operation actions for analysis.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterCompilationStartAction(OnCompilationStart);

    private static void OnCompilationStart(CompilationStartAnalysisContext context) {
        // OrEmpty() lives in ANcpLua.Roslyn.Utilities.EnumerableExtensions. Only fire when present
        // and callable from this compilation; otherwise the code fix would rewrite to a symbol the
        // consumer cannot resolve.
        if (context.Compilation.GetTypeByMetadataName(EnumerableExtensionsMetadataName) is not { } gateType) {
            return;
        }

        if (!context.Compilation.IsSymbolAccessibleWithin(gateType, context.Compilation.Assembly)) {
            return;
        }

        context.RegisterOperationAction(AnalyzeCoalesce, OperationKind.Coalesce);
    }

    private static void AnalyzeCoalesce(OperationAnalysisContext context) {
        if (context.Operation is not ICoalesceOperation coalesce) {
            return;
        }

        if (!IsEmptyCollectionExpression(coalesce.WhenNull)) {
            return;
        }

        // OrEmpty() returns IEnumerable<T>, so only fire when the result type matches.
        // Concrete types (Dictionary, List, string[], etc.) would lose type information.
        var resultType = coalesce.Type;
        if (resultType is null || resultType.SpecialType == SpecialType.System_String) {
            return;
        }

        if (!IsEnumerableType(resultType)) {
            return;
        }

        var leftName = GetOperandDisplayName(coalesce.Value);
        context.ReportDiagnostic(Diagnostic.Create(s_rule, coalesce.Syntax.GetLocation(),
            $"{leftName}.OrEmpty()", "null-coalescing with empty collection"));
    }

    private static bool IsEmptyCollectionExpression(IOperation? operation) {
        if (operation is null) {
            return false;
        }

        operation = operation.UnwrapAllConversions();

        switch (operation) {
            case ICollectionExpressionOperation { Elements.Length: 0 }:
                return true;
            case IInvocationOperation { TargetMethod: { Name: "Empty", IsStatic: true, Parameters.Length: 0, ContainingType: { } containingType } }
                when (containingType.ContainingNamespace?.ToDisplayString() is { } ns
                      && (containingType.Name == "Array" && ns == "System" ||
                          containingType.Name == "Enumerable" && ns == "System.Linq")):
                return true;
            case IArrayCreationOperation {
                DimensionSizes: [{ ConstantValue: { HasValue: true, Value: 0 } }]
            }:
            case IArrayCreationOperation {
                Initializer.ElementValues.Length: 0
            }:
                return true;
            default:
                return false;
        }
    }

    private static bool IsEnumerableType(ITypeSymbol type) =>
        type is INamedTypeSymbol { IsGenericType: true } named
        && named.OriginalDefinition.ToDisplayString() == "System.Collections.Generic.IEnumerable<T>";

    private static string GetOperandDisplayName(IOperation operation) =>
        operation.GetOperandName("collection");
}
