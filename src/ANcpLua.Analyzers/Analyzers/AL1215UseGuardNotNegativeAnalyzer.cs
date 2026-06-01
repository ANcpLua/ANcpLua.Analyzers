
namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL1215: Suggests using Guard.NotNegative() instead of if (x less than 0) throw patterns.
/// </summary>
/// <remarks>
///     <list type="bullet">
///         <item><c>if (x &lt; 0) throw new ArgumentOutOfRangeException(nameof(x))</c> → <c>Guard.NotNegative(x)</c></item>
///         <item><c>if (0 &gt; x) throw new ArgumentOutOfRangeException(nameof(x))</c> → <c>Guard.NotNegative(x)</c></item>
///     </list>
///     <para>
///         NOTE: This analyzer only matches strict less-than (x &lt; 0). For x &lt;= 0, use AL1216 (Guard.Positive).
///     </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al1215UseGuardNotNegativeAnalyzer : AlAnalyzer {
    /// <summary>The diagnostic identifier for AL1215.</summary>
    public const string DiagnosticId = "AL1215";

    private const string GuardMetadataName = "ANcpLua.Roslyn.Utilities.Guard";

    private static readonly DiagnosticDescriptor s_rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.RoslynUtilities,
        DiagnosticSeverities.Suggestion);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_rule];

    /// <summary>Registers syntax or operation actions for analysis.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterCompilationStartAction(OnCompilationStart);

    private static void OnCompilationStart(CompilationStartAnalysisContext context) {
        // Guard.* lives in ANcpLua.Roslyn.Utilities.Guard. Only fire when that type is present and
        // callable from this compilation; otherwise the code fix would rewrite to a symbol the
        // consumer cannot resolve. Projects that do not reference ANcpLua.Roslyn.Utilities are unaffected.
        if (context.Compilation.GetTypeByMetadataName(GuardMetadataName) is not { } guardType) {
            return;
        }

        if (!context.Compilation.IsSymbolAccessibleWithin(guardType, context.Compilation.Assembly)) {
            return;
        }

        context.RegisterOperationAction(AnalyzeConditional, OperationKind.Conditional);
    }

    private static void AnalyzeConditional(OperationAnalysisContext context) {
        if (context.Operation is not IConditionalOperation { Syntax: IfStatementSyntax } conditional) {
            return;
        }

        var (isLessThanZero, operandName) = IsLessThanZeroComparison(conditional.Condition);
        if (!isLessThanZero || operandName is null) {
            return;
        }

        if (conditional.WhenFalse is not null) {
            return;
        }

        if (!HasSingleArgumentOutOfRangeExceptionThrow(conditional.WhenTrue)) {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(s_rule, conditional.Syntax.GetLocation(), operandName));
    }

    private static (bool isMatch, string? operandName) IsLessThanZeroComparison(IOperation? condition) {
        if (condition is null) {
            return (false, null);
        }

        while (condition is IParenthesizedOperation paren) {
            condition = paren.Operand;
        }

        if (condition is not IBinaryOperation binary) {
            return (false, null);
        }

        var left = binary.LeftOperand.UnwrapAllConversions();
        var right = binary.RightOperand.UnwrapAllConversions();

        return binary.OperatorKind switch {
            BinaryOperatorKind.LessThan when IsZeroConstant(right) => (true, left.GetOperandName()),
            BinaryOperatorKind.GreaterThan when IsZeroConstant(left) => (true, right.GetOperandName()),
            _ => (false, null)
        };
    }

    private static bool IsZeroConstant(IOperation? operation) =>
        operation is ILiteralOperation { ConstantValue: { HasValue: true, Value: 0 or 0L or 0.0 or 0.0f or 0m } };

    private static bool HasSingleArgumentOutOfRangeExceptionThrow(IOperation? operation) =>
        operation switch {
            null => false,
            IBlockOperation block => block.Operations.Length == 1 && IsArgumentOutOfRangeExceptionThrow(block.Operations[0]),
            _ => IsArgumentOutOfRangeExceptionThrow(operation)
        };

    private static bool IsArgumentOutOfRangeExceptionThrow(IOperation? operation) {
        if (operation is IExpressionStatementOperation exprStmt) {
            operation = exprStmt.Operation;
        }

        if (operation is not IThrowOperation { Exception: { } exception }) {
            return false;
        }

        if (exception.UnwrapAllConversions() is not IObjectCreationOperation { Type: { } exceptionType }) {
            return false;
        }

        return exceptionType.ToDisplayString()
            is "System.ArgumentOutOfRangeException" or "ArgumentOutOfRangeException";
    }
}
