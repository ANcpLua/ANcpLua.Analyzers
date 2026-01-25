using ANcpLua.Analyzers.Core;

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
    private static readonly LocalizableResourceString Title = new(
        nameof(Resources.AL0034AnalyzerTitle), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableResourceString MessageFormat = new(
        nameof(Resources.AL0034AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableResourceString Description = new(
        nameof(Resources.AL0034AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.UseWhereNotNull,
        Title, MessageFormat, DiagnosticCategories.RoslynUtilities,
        DiagnosticSeverity.Info, true, Description,
        HelpLinkBase);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    /// <summary>Registers syntax or operation actions for analysis.</summary>

    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);

    private static void AnalyzeInvocation(OperationAnalysisContext context) {
        if (context.Operation is not IInvocationOperation invocation) {
            return;
        }

        var method = invocation.TargetMethod;

        // Check for LINQ Where method
        if (method.Name != "Where") {
            return;
        }

        // Must be from System.Linq.Enumerable (or Queryable)
        if (method.ContainingType is not { } containingType) {
            return;
        }

        // Use the full display string to check - it should contain "System.Linq.Enumerable"
        // Use IndexOf instead of Contains for netstandard2.0 compatibility
        var fullTypeName = containingType.ToDisplayString();
        if (fullTypeName.IndexOf("System.Linq.Enumerable", StringComparison.Ordinal) < 0 &&
            fullTypeName.IndexOf("Enumerable", StringComparison.Ordinal) < 0) {
            return;
        }

        // Get the predicate argument (could be arg[0] for extension or arg[1] for static)
        if (GetPredicateArgument(invocation) is not { } predicateArg) {
            return;
        }

        // Get the predicate argument value and extract the lambda
        var predicateValue = predicateArg.Value;

        // Unwrap conversions to get to the lambda
        while (predicateValue is IConversionOperation conversion) {
            predicateValue = conversion.Operand;
        }

        // Check for lambda wrapped in delegate creation (common case)
        if (predicateValue is IDelegateCreationOperation delegateCreation) {
            predicateValue = delegateCreation.Target;
        }

        // Check if it's a lambda that just checks for not null
        if (predicateValue is IAnonymousFunctionOperation lambda && IsNullCheckLambda(lambda)) {
            context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.Syntax.GetLocation(),
                ".WhereNotNull()", ".Where(x => x != null)"));
        }
    }

    private static IArgumentOperation? GetPredicateArgument(IInvocationOperation invocation) {
        // Where has overloads with 1 or 2 predicates (with and without index)
        // We only handle the simple predicate case
        var method = invocation.TargetMethod;

        // Must have exactly 2 parameters: source and predicate (for extension methods)
        // In the operation model, extension method calls might show as instance calls
        if (method.Parameters.Length != 2) {
            return null;
        }

        // Find the predicate parameter (should be named "predicate")
        foreach (var arg in invocation.Arguments) {
            if (arg.Parameter?.Name == "predicate") {
                return arg;
            }
        }

        // Fallback: for instance-style calls, predicate is usually Arguments[0]
        if (invocation.Arguments.Length >= 1) {
            return invocation.Arguments[0];
        }

        return null;
    }

    private static bool IsNullCheckLambda(IAnonymousFunctionOperation lambda) {
        // Lambda should have exactly one parameter
        if (lambda.Symbol.Parameters.Length != 1) {
            return false;
        }

        var parameter = lambda.Symbol.Parameters[0];

        // The body should be a single expression that checks the parameter is not null
        var body = lambda.Body;
        // If it's a block, look for a single return statement
        return body.Operations is [
            IReturnOperation {
                ReturnedValue: { } returnValue
            }
        ] && IsNotNullCheck(returnValue, parameter);
    }

    private static bool IsNotNullCheck(IOperation? operation, IParameterSymbol parameter) {
        if (operation is null) {
            return false;
        }

        // Unwrap conversions
        while (operation is IConversionOperation conversion) {
            operation = conversion.Operand;
        }

        return operation switch {
            // Pattern: x != null
            IBinaryOperation {
                OperatorKind: BinaryOperatorKind.NotEquals
            } binary => IsParameterAndNull(binary.LeftOperand, binary.RightOperand, parameter) || IsParameterAndNull(binary.RightOperand, binary.LeftOperand, parameter),
            // Pattern: x is not null
            IIsPatternOperation isPattern when IsParameterReference(isPattern.Value, parameter) && isPattern.Pattern is INegatedPatternOperation {
                Pattern: IConstantPatternOperation {
                    Value.ConstantValue: {
                        HasValue: true, Value: null
                    }
                }
            } => true,
            _ => false
        };

        // Pattern: x is { } (object pattern matching not null)
        // This is more complex and we'll skip for simplicity - the main patterns are covered
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
