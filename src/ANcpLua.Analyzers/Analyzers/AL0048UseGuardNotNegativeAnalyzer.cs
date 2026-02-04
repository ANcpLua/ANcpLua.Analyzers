using ANcpLua.Analyzers.Core;

namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0048: Suggests using Guard.NotNegative() instead of if (x less than 0) throw patterns.
/// </summary>
/// <remarks>
///     <list type="bullet">
///         <item><c>if (x &lt; 0) throw new ArgumentOutOfRangeException(nameof(x))</c> → <c>Guard.NotNegative(x)</c></item>
///         <item><c>if (0 &gt; x) throw new ArgumentOutOfRangeException(nameof(x))</c> → <c>Guard.NotNegative(x)</c></item>
///     </list>
///     <para>
///         NOTE: This analyzer only matches strict less-than (x &lt; 0). For x &lt;= 0, use AL0049 (Guard.Positive).
///     </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0048UseGuardNotNegativeAnalyzer : AlAnalyzer {
    private static readonly DiagnosticDescriptor Rule = CreateRule(
        DiagnosticIds.UseGuardNotNegative,
        DiagnosticCategories.RoslynUtilities,
        DiagnosticSeverities.Suggestion);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    /// <summary>Registers syntax or operation actions for analysis.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterOperationAction(AnalyzeConditional, OperationKind.Conditional);

    private static void AnalyzeConditional(OperationAnalysisContext context) {
        if (context.Operation is not IConditionalOperation conditional) {
            return;
        }

        // We're looking for if-statements, not ternary expressions
        // If-statements have a null WhenFalse for simple if without else
        // But IConditionalOperation can represent both - we need to check the syntax
        if (conditional.Syntax is not IfStatementSyntax) {
            return;
        }

        // Check if condition is a less-than comparison with zero
        var (isLessThanZero, operandName) = IsLessThanZeroComparison(conditional.Condition);
        if (!isLessThanZero || operandName is null) {
            return;
        }

        // Check if the WhenTrue branch throws ArgumentOutOfRangeException
        if (!ContainsArgumentOutOfRangeExceptionThrow(conditional.WhenTrue)) {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(Rule, conditional.Syntax.GetLocation(), operandName));
    }

    private static (bool isMatch, string? operandName) IsLessThanZeroComparison(IOperation? condition) {
        if (condition is null) {
            return (false, null);
        }

        // Unwrap parentheses
        while (condition is IParenthesizedOperation paren) {
            condition = paren.Operand;
        }

        if (condition is not IBinaryOperation binary) {
            return (false, null);
        }

        // Only match strict less-than, NOT less-than-or-equal (that's for Guard.Positive)
        // Patterns:
        // - x < 0  (OperatorKind.LessThan, left = x, right = 0)
        // - 0 > x  (OperatorKind.GreaterThan, left = 0, right = x)

        var leftOperand = OperationHelper.UnwrapConversions(binary.LeftOperand);
        var rightOperand = OperationHelper.UnwrapConversions(binary.RightOperand);

        return binary.OperatorKind switch {
            BinaryOperatorKind.LessThan when IsZeroConstant(rightOperand) =>
                (true, OperationHelper.GetOperandName(leftOperand, "value")),
            BinaryOperatorKind.GreaterThan when IsZeroConstant(leftOperand) =>
                (true, OperationHelper.GetOperandName(rightOperand, "value")),
            _ => (false, null)
        };
    }

    private static bool IsZeroConstant(IOperation? operation) {
        if (operation is null) {
            return false;
        }

        // Check for literal 0
        if (operation is ILiteralOperation literal &&
            literal.ConstantValue.HasValue) {
            var value = literal.ConstantValue.Value;
            return value switch {
                0 => true,
                0L => true,
                0.0 => true,
                0.0f => true,
                0m => true,
                _ => false
            };
        }

        return false;
    }

    private static bool ContainsArgumentOutOfRangeExceptionThrow(IOperation? operation) {
        if (operation is null) {
            return false;
        }

        // Handle block statement: if (x < 0) { throw ...; }
        if (operation is IBlockOperation block) {
            foreach (var statement in block.Operations) {
                if (IsArgumentOutOfRangeExceptionThrow(statement)) {
                    return true;
                }
            }

            return false;
        }

        // Handle direct throw statement: if (x < 0) throw ...;
        return IsArgumentOutOfRangeExceptionThrow(operation);
    }

    private static bool IsArgumentOutOfRangeExceptionThrow(IOperation? operation) {
        if (operation is null) {
            return false;
        }

        // Unwrap expression statement if present
        if (operation is IExpressionStatementOperation exprStmt) {
            operation = exprStmt.Operation;
        }

        // Check for throw operation
        if (operation is not IThrowOperation throwOp) {
            return false;
        }

        // Get the exception being thrown
        if (throwOp.Exception is not { } exception) {
            return false;
        }

        // Unwrap conversions
        exception = OperationHelper.UnwrapConversions(exception);

        // Check if it's creating an ArgumentOutOfRangeException
        if (exception is not IObjectCreationOperation { Type: { } exceptionType }) {
            return false;
        }

        // Check if it's ArgumentOutOfRangeException
        var typeName = exceptionType.ToDisplayString();
        return typeName is "System.ArgumentOutOfRangeException" or "ArgumentOutOfRangeException";
    }
}
