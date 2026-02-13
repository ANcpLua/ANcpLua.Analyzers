using Microsoft.CodeAnalysis;

namespace ANcpLua.Analyzers.Core;

/// <summary>
///     Provides helper methods for checking argument exception types.
/// </summary>
internal static partial class OperationHelper {
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
