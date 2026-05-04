namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0125: Collapses chained <c>s == "a" || s == "b" || s == "c"</c> into
///     <c>s.EqualsAnyOrdinal("a", "b", "c")</c>.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0125UseStringComparisonAnyExtensionsAnalyzer : AlAnalyzer {
    /// <summary>The diagnostic identifier for AL0125.</summary>
    private const string DiagnosticId = "AL0125";

    private static readonly DiagnosticDescriptor s_rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.RoslynUtilities,
        DiagnosticSeverities.Suggestion);

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_rule];

    /// <inheritdoc />
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterOperationAction(AnalyzeBinaryOperator, OperationKind.BinaryOperator);

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
