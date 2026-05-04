
namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0034: Suggests using WhereNotNull() instead of Where with null check.
/// </summary>
/// <remarks>
///     <list type="bullet">
///         <item><c>.Where(x => x != null)</c> → <c>.WhereNotNull()</c></item>
///         <item><c>.Where(x => x is not null)</c> → <c>.WhereNotNull()</c></item>
///     </list>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0034UseWhereNotNullAnalyzer : AlAnalyzer {
    /// <summary>The diagnostic identifier for AL0034.</summary>
    public const string DiagnosticId = "AL0034";

    private static readonly DiagnosticDescriptor s_rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.RoslynUtilities,
        DiagnosticSeverity.Info);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_rule];

    /// <summary>Registers syntax or operation actions for analysis.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);

    private static void AnalyzeInvocation(OperationAnalysisContext context) {
        if (context.Operation is not IInvocationOperation invocation) {
            return;
        }

        var method = invocation.TargetMethod;

        if (method.Name != "Where") {
            return;
        }

        if (method.ContainingType is not { } containingType) {
            return;
        }

        var fullTypeName = containingType.ToDisplayString();
        if (!fullTypeName.ContainsOrdinal("System.Linq.Enumerable") &&
            !fullTypeName.ContainsOrdinal("Enumerable")) {
            return;
        }

        if (GetPredicateArgument(invocation) is not { } predicateArg) {
            return;
        }

        var predicateValue = predicateArg.Value;

        while (predicateValue is IConversionOperation conversion) {
            predicateValue = conversion.Operand;
        }

        if (predicateValue is IDelegateCreationOperation delegateCreation) {
            predicateValue = delegateCreation.Target;
        }

        if (predicateValue is IAnonymousFunctionOperation lambda && IsNullCheckLambda(lambda)) {
            context.ReportDiagnostic(Diagnostic.Create(s_rule, invocation.Syntax.GetLocation(),
                ".WhereNotNull()", ".Where(x => x != null)"));
        }
    }

    private static IArgumentOperation? GetPredicateArgument(IInvocationOperation invocation) {
        if (invocation.TargetMethod.Parameters.Length != 2) {
            return null;
        }

        foreach (var arg in invocation.Arguments) {
            if (arg.Parameter?.Name == "predicate") {
                return arg;
            }
        }

        return invocation.Arguments.Length >= 1 ? invocation.Arguments[0] : null;
    }

    private static bool IsNullCheckLambda(IAnonymousFunctionOperation lambda) {
        if (lambda.Symbol.Parameters.Length != 1) {
            return false;
        }

        var parameter = lambda.Symbol.Parameters[0];

        return lambda.Body.Operations is [
            IReturnOperation { ReturnedValue: { } returnValue }
        ] && IsNotNullCheck(returnValue, parameter);
    }

    private static bool IsNotNullCheck(IOperation? operation, IParameterSymbol parameter) {
        if (operation is null) {
            return false;
        }

        while (operation is IConversionOperation conversion) {
            operation = conversion.Operand;
        }

        return operation switch {
            IBinaryOperation {
                OperatorKind: BinaryOperatorKind.NotEquals
            } binary => IsParameterAndNull(binary.LeftOperand, binary.RightOperand, parameter) ||
                         IsParameterAndNull(binary.RightOperand, binary.LeftOperand, parameter),
            IIsPatternOperation isPattern when IsParameterReference(isPattern.Value, parameter)
                && isPattern.Pattern is INegatedPatternOperation {
                    Pattern: IConstantPatternOperation {
                        Value.ConstantValue: { HasValue: true, Value: null }
                    }
                } => true,
            _ => false
        };
    }

    private static bool IsParameterAndNull(IOperation left, IOperation right, IParameterSymbol parameter) => IsParameterReference(left, parameter) && IsNullLiteral(right);

    private static bool IsParameterReference(IOperation? operation, IParameterSymbol parameter) {
        while (operation is IConversionOperation conversion) {
            operation = conversion.Operand;
        }

        return operation is IParameterReferenceOperation paramRef &&
               paramRef.Parameter.IsEqualTo(parameter);
    }

    private static bool IsNullLiteral(IOperation? operation) {
        while (operation is IConversionOperation conversion) {
            operation = conversion.Operand;
        }

        return operation is ILiteralOperation { ConstantValue: { HasValue: true, Value: null } };
    }
}
