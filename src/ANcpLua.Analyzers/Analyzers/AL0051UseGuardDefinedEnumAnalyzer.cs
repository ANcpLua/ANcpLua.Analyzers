using ANcpLua.Analyzers.Core;

namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0051: Suggests using Guard.DefinedEnum() instead of if (!Enum.IsDefined) throw patterns.
/// </summary>
/// <remarks>
///     <list type="bullet">
///         <item><c>if (!Enum.IsDefined(typeof(MyEnum), value)) throw new ArgumentException(...)</c> -> <c>Guard.DefinedEnum(value)</c></item>
///         <item><c>if (!Enum.IsDefined&lt;MyEnum&gt;(value)) throw new ArgumentException(...)</c> -> <c>Guard.DefinedEnum(value)</c></item>
///         <item><c>if (!Enum.IsDefined(typeof(MyEnum), value)) throw new ArgumentOutOfRangeException(...)</c> -> <c>Guard.DefinedEnum(value)</c></item>
///     </list>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0051UseGuardDefinedEnumAnalyzer : AlAnalyzer {
    private static readonly DiagnosticDescriptor Rule = CreateRule(
        DiagnosticIds.UseGuardDefinedEnum,
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
        if (conditional.Syntax is not IfStatementSyntax) {
            return;
        }

        // Check if condition is !Enum.IsDefined(...)
        var (isMatch, operandName) = IsNegatedEnumIsDefinedCheck(conditional.Condition);
        if (!isMatch || operandName is null) {
            return;
        }

        // Check if the WhenTrue branch throws ArgumentException or ArgumentOutOfRangeException
        if (!ContainsArgumentExceptionThrow(conditional.WhenTrue)) {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(Rule, conditional.Syntax.GetLocation(), operandName));
    }

    private static (bool isMatch, string? operandName) IsNegatedEnumIsDefinedCheck(IOperation? condition) {
        if (condition is null) {
            return (false, null);
        }

        // Unwrap parentheses
        while (condition is IParenthesizedOperation paren) {
            condition = paren.Operand;
        }

        // Check for negation: !Enum.IsDefined(...)
        if (condition is not IUnaryOperation { OperatorKind: UnaryOperatorKind.Not } unary) {
            return (false, null);
        }

        // Get the operand of the negation
        var operand = unary.Operand.UnwrapAllConversions();

        // Unwrap parentheses on the operand too
        while (operand is IParenthesizedOperation parenOp) {
            operand = parenOp.Operand;
        }

        // Check if it's an Enum.IsDefined invocation
        if (operand is not IInvocationOperation invocation) {
            return (false, null);
        }

        var method = invocation.TargetMethod;

        // Check if it's Enum.IsDefined
        if (method.Name != "IsDefined" ||
            method.ContainingType?.ToDisplayString() is not ("System.Enum" or "Enum")) {
            return (false, null);
        }

        // Extract the enum value argument
        // Pattern 1: Enum.IsDefined(Type enumType, object value) - 2 args
        // Pattern 2: Enum.IsDefined<TEnum>(TEnum value) - 1 arg (generic)
        string? valueName = null;

        if (method.IsGenericMethod && invocation.Arguments.Length >= 1) {
            // Generic version: Enum.IsDefined<T>(value)
            valueName = invocation.Arguments[0].Value.GetOperandName();
        } else if (!method.IsGenericMethod && invocation.Arguments.Length >= 2) {
            // Non-generic version: Enum.IsDefined(typeof(T), value)
            valueName = invocation.Arguments[1].Value.GetOperandName();
        }

        return valueName is not null ? (true, valueName) : (false, null);
    }

    private static bool ContainsArgumentExceptionThrow(IOperation? operation) {
        if (operation is null) {
            return false;
        }

        // Handle block statement: if (!Enum.IsDefined(...)) { throw ...; }
        if (operation is IBlockOperation block) {
            foreach (var statement in block.Operations) {
                if (IsArgumentExceptionThrow(statement)) {
                    return true;
                }
            }

            return false;
        }

        // Handle direct throw statement: if (!Enum.IsDefined(...)) throw ...;
        return IsArgumentExceptionThrow(operation);
    }

    private static bool IsArgumentExceptionThrow(IOperation? operation) {
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

        // Unwrap conversions and check if it's creating an exception
        if (exception.UnwrapAllConversions() is not IObjectCreationOperation { Type: { } exceptionType }) {
            return false;
        }

        // Check if it's ArgumentException or ArgumentOutOfRangeException
        var typeName = exceptionType.ToDisplayString();
        return typeName is
            "System.ArgumentException" or "ArgumentException" or
            "System.ArgumentOutOfRangeException" or "ArgumentOutOfRangeException";
    }
}
