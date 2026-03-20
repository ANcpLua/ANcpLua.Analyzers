
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
    /// <summary>The diagnostic identifier for AL0051.</summary>
    public const string DiagnosticId = "AL0051";

    private static readonly DiagnosticDescriptor Rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.RoslynUtilities,
        DiagnosticSeverities.Suggestion);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    /// <summary>Registers syntax or operation actions for analysis.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterOperationAction(AnalyzeConditional, OperationKind.Conditional);

    private static void AnalyzeConditional(OperationAnalysisContext context) {
        if (context.Operation is not IConditionalOperation { Syntax: IfStatementSyntax } conditional) {
            return;
        }

        var (isMatch, operandName) = IsNegatedEnumIsDefinedCheck(conditional.Condition);
        if (!isMatch || operandName is null) {
            return;
        }

        if (!ContainsArgumentExceptionThrow(conditional.WhenTrue)) {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(Rule, conditional.Syntax.GetLocation(), operandName));
    }

    private static (bool isMatch, string? operandName) IsNegatedEnumIsDefinedCheck(IOperation? condition) {
        if (condition is null) {
            return (false, null);
        }

        while (condition is IParenthesizedOperation paren) {
            condition = paren.Operand;
        }

        if (condition is not IUnaryOperation { OperatorKind: UnaryOperatorKind.Not } unary) {
            return (false, null);
        }

        var operand = unary.Operand.UnwrapAllConversions();

        while (operand is IParenthesizedOperation parenOp) {
            operand = parenOp.Operand;
        }

        if (operand is not IInvocationOperation invocation) {
            return (false, null);
        }

        var method = invocation.TargetMethod;

        if (method.Name != "IsDefined" ||
            method.ContainingType?.ToDisplayString() is not ("System.Enum" or "Enum")) {
            return (false, null);
        }

        var valueName = (method.IsGenericMethod, invocation.Arguments.Length) switch {
            (true, >= 1) => invocation.Arguments[0].Value.GetOperandName(),
            (false, >= 2) => invocation.Arguments[1].Value.GetOperandName(),
            _ => null
        };

        return valueName is not null ? (true, valueName) : (false, null);
    }

    private static bool ContainsArgumentExceptionThrow(IOperation? operation) =>
        operation switch {
            null => false,
            IBlockOperation block => block.Operations.Any(IsArgumentExceptionThrow),
            _ => IsArgumentExceptionThrow(operation)
        };

    private static bool IsArgumentExceptionThrow(IOperation? operation) {
        if (operation is IExpressionStatementOperation exprStmt) {
            operation = exprStmt.Operation;
        }

        if (operation is not IThrowOperation { Exception: { } exception }) {
            return false;
        }

        if (exception.UnwrapAllConversions() is not IObjectCreationOperation { Type: { } exceptionType }) {
            return false;
        }

        return exceptionType.ToDisplayString() is
            "System.ArgumentException" or "ArgumentException" or
            "System.ArgumentOutOfRangeException" or "ArgumentOutOfRangeException";
    }
}
