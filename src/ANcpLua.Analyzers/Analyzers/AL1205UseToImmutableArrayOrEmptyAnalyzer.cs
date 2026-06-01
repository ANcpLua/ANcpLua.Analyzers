
namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL1205: Suggests using ToImmutableArrayOrEmpty() instead of null-conditional with fallback.
/// </summary>
/// <remarks>
///     <c>collection?.ToImmutableArray() ?? ImmutableArray&lt;T&gt;.Empty</c> →
///     <c>collection.ToImmutableArrayOrEmpty()</c>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al1205UseToImmutableArrayOrEmptyAnalyzer : AlAnalyzer {
    /// <summary>The diagnostic identifier for AL1205.</summary>
    public const string DiagnosticId = "AL1205";

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
        // ToImmutableArrayOrEmpty() lives in ANcpLua.Roslyn.Utilities.EnumerableExtensions. Only fire
        // when present and callable from this compilation; otherwise the code fix would rewrite to a
        // symbol the consumer cannot resolve.
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

        if (!IsImmutableArrayEmpty(coalesce.WhenNull)) {
            return;
        }

        if (!TryGetToImmutableArraySource(coalesce.Value, out var sourceName)) {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(s_rule, coalesce.Syntax.GetLocation(),
            $"{sourceName}.ToImmutableArrayOrEmpty()", "?.ToImmutableArray() ?? ImmutableArray.Empty"));
    }

    private static bool IsImmutableArrayEmpty(IOperation? operation) {
        if (operation is null) {
            return false;
        }

        operation = operation.UnwrapAllConversions();

        return operation switch {
            IFieldReferenceOperation { Field: { Name: "Empty", IsStatic: true } } fieldRef
                when IsImmutableArrayType(fieldRef.Field.ContainingType) => true,
            IDefaultValueOperation defaultOp
                when IsImmutableArrayType(defaultOp.Type) => true,
            _ => false
        };
    }

    private static bool IsImmutableArrayType(ITypeSymbol? type) =>
        type is not null &&
        type.OriginalDefinition.ToDisplayString() == "System.Collections.Immutable.ImmutableArray<T>";

    private static bool TryGetToImmutableArraySource(IOperation? operation, out string sourceName) {
        sourceName = "collection";

        if (operation is null) {
            return false;
        }

        operation = operation.UnwrapAllConversions();

        if (operation is IConditionalAccessOperation {
            WhenNotNull: IInvocationOperation { TargetMethod.Name: "ToImmutableArray" }
        } conditionalAccess) {
            sourceName = GetOperandDisplayName(conditionalAccess.Operation);
            return true;
        }

        return false;
    }

    private static string GetOperandDisplayName(IOperation operation) =>
        operation.GetOperandName("collection");
}
