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

    /// <summary>
    ///     Determines whether the type represents <see cref="System.ArgumentNullException"/>.
    /// </summary>
    /// <param name="type">The type symbol to check.</param>
    /// <returns><c>true</c> if the type is ArgumentNullException; otherwise, <c>false</c>.</returns>
    public static bool IsArgumentNullException(ITypeSymbol? type) =>
        type?.ToDisplayString() is "System.ArgumentNullException" or "ArgumentNullException";

    /// <summary>
    ///     Determines whether the type represents <see cref="System.ArgumentException"/>.
    /// </summary>
    /// <param name="type">The type symbol to check.</param>
    /// <returns><c>true</c> if the type is ArgumentException; otherwise, <c>false</c>.</returns>
    public static bool IsArgumentException(ITypeSymbol? type) =>
        type?.ToDisplayString() is "System.ArgumentException" or "ArgumentException";

    /// <summary>
    ///     Determines whether the type represents <see cref="System.ArgumentOutOfRangeException"/>.
    /// </summary>
    /// <param name="type">The type symbol to check.</param>
    /// <returns><c>true</c> if the type is ArgumentOutOfRangeException; otherwise, <c>false</c>.</returns>
    public static bool IsArgumentOutOfRangeException(ITypeSymbol? type) =>
        type?.ToDisplayString() is "System.ArgumentOutOfRangeException" or "ArgumentOutOfRangeException";

    /// <summary>
    ///     Determines whether the type represents any argument exception type
    ///     (ArgumentException, ArgumentNullException, or ArgumentOutOfRangeException).
    /// </summary>
    /// <param name="type">The type symbol to check.</param>
    /// <returns><c>true</c> if the type is any argument exception; otherwise, <c>false</c>.</returns>
    public static bool IsAnyArgumentException(ITypeSymbol? type) =>
        IsArgumentException(type) || IsArgumentNullException(type) || IsArgumentOutOfRangeException(type);
}
