namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL1219: Collapses chained <c>s == "a" || s == "b" || s == "c"</c> into
///     <c>s.EqualsAnyOrdinal("a", "b", "c")</c>.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al1219UseStringComparisonAnyExtensionsAnalyzer : AlAnalyzer {
    /// <summary>The diagnostic identifier for AL1219.</summary>
    private const string DiagnosticId = "AL1219";

    private const string StringComparisonExtensionsMetadataName = "ANcpLua.Roslyn.Utilities.StringComparisonExtensions";

    private static readonly DiagnosticDescriptor s_rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.RoslynUtilities,
        DiagnosticSeverities.Suggestion);

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_rule];

    /// <inheritdoc />
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterCompilationStartAction(OnCompilationStart);

    private static void OnCompilationStart(CompilationStartAnalysisContext context) {
        // EqualsAnyOrdinal() lives in ANcpLua.Roslyn.Utilities.StringComparisonExtensions. Only fire when
        // that type is present and callable from this compilation; otherwise the suggestion would reference
        // a symbol the consumer cannot resolve.
        if (context.Compilation.GetTypeByMetadataName(StringComparisonExtensionsMetadataName) is not { } gateType) {
            return;
        }

        if (!context.Compilation.IsSymbolAccessibleWithin(gateType, context.Compilation.Assembly)) {
            return;
        }

        context.RegisterOperationAction(AnalyzeBinaryOperator, OperationKind.BinaryOperator);
    }

    private static void AnalyzeBinaryOperator(OperationAnalysisContext context) {
        if (context.Operation is not IBinaryOperation { OperatorKind: BinaryOperatorKind.ConditionalOr } topOr) {
            return;
        }

        // Skip nested ORs — the outermost OR handles the full chain
        if (context.Operation.Parent is IBinaryOperation { OperatorKind: BinaryOperatorKind.ConditionalOr }) {
            return;
        }

        // Flatten: s == "a" || s == "b" || s == "c" → list of (receiver, constant) pairs
        string? receiver = null;
        var constants = new List<string>();

        if (!CollectEqualityChain(topOr, ref receiver, constants) || constants.Count < 2 || receiver is null) {
            return;
        }

        var args = string.Join(", ", constants);
        context.ReportDiagnostic(s_rule, topOr.Syntax.GetLocation(),
            $"{receiver}.EqualsAnyOrdinal({args})");
    }

    private static bool CollectEqualityChain(IOperation op, ref string? receiver, IList<string> constants) {
        if (op is IBinaryOperation { OperatorKind: BinaryOperatorKind.ConditionalOr } or) {
            return CollectEqualityChain(or.LeftOperand, ref receiver, constants) &&
                   CollectEqualityChain(or.RightOperand, ref receiver, constants);
        }

        if (op is not IBinaryOperation { OperatorKind: BinaryOperatorKind.Equals } eq) {
            return false;
        }

        var (left, right) = (eq.LeftOperand, eq.RightOperand);

        // Try left = variable, right = constant
        if (left.Type?.SpecialType == SpecialType.System_String &&
            right.ConstantValue is { HasValue: true, Value: string rv }) {
            var name = left.GetOperandName("s");
            if (receiver is null) receiver = name;
            else if (receiver != name) return false;
            constants.Add($"\"{rv}\"");
            return true;
        }

        // Try left = constant, right = variable
        if (right.Type?.SpecialType == SpecialType.System_String &&
            left.ConstantValue is { HasValue: true, Value: string lv }) {
            var name = right.GetOperandName("s");
            if (receiver is null) receiver = name;
            else if (receiver != name) return false;
            constants.Add($"\"{lv}\"");
            return true;
        }

        return false;
    }
}
