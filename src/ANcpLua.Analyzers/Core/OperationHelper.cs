namespace ANcpLua.Analyzers.Core;

/// <summary>
///     Provides helper methods for working with Roslyn IOperation instances.
/// </summary>
internal static partial class OperationHelper {
    /// <summary>
    ///     Unwraps conversion operations to get to the underlying operand.
    /// </summary>
    /// <param name="operation">The operation to unwrap.</param>
    /// <returns>The underlying operation after removing all conversion wrappers.</returns>
    public static IOperation? UnwrapConversions(IOperation? operation) {
        while (operation is IConversionOperation conversion) {
            operation = conversion.Operand;
        }

        return operation;
    }

    /// <summary>
    ///     Gets a human-readable display name for an operation.
    ///     Useful for generating diagnostic messages.
    /// </summary>
    /// <param name="operation">The operation to get the name for.</param>
    /// <param name="fallback">The fallback name if the operation type is not recognized.</param>
    /// <returns>A display name for the operation.</returns>
    public static string GetOperandName(IOperation? operation, string fallback = "value") {
        if (operation is null) {
            return fallback;
        }

        // Unwrap conversions first
        operation = UnwrapConversions(operation);

        return operation switch {
            ILocalReferenceOperation local => local.Local.Name,
            IParameterReferenceOperation param => param.Parameter.Name,
            IPropertyReferenceOperation prop => prop.Property.Name,
            IFieldReferenceOperation field => field.Field.Name,
            IInvocationOperation inv => $"{inv.TargetMethod.Name}()",
            _ => fallback
        };
    }
}
