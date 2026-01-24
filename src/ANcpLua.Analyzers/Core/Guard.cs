using System.Runtime.CompilerServices;

namespace ANcpLua.Roslyn.Utilities;

/// <summary>
///     Provides guard clauses for argument validation with clean, expressive syntax.
/// </summary>
/// <remarks>
///     <para>
///         This class eliminates common boilerplate patterns like <c>value ?? throw new ArgumentNullException()</c>
///         and null-forgiving operator (<c>!</c>) usage by providing fluent guard methods that return the validated value.
///     </para>
///     <para>
///         All methods use CallerArgumentExpressionAttribute to automatically capture
///         the parameter name, eliminating the need to pass <c>nameof(parameter)</c>.
///     </para>
///     <list type="bullet">
///         <item>
///             <description>
///                 <see cref="NotNull{T}" /> - Validates non-null and returns the value (throws on null)
///             </description>
///         </item>
///         <item>
///             <description>
///                 <see cref="NotNullOrElse{T}(T?, T)" /> - Returns value or fallback (reference/value types)
///             </description>
///         </item>
///         <item>
///             <description>
///                 <see cref="NotNullOrElse{T}(T?, Func{T})" /> - Returns value or lazily computed fallback
///             </description>
///         </item>
///         <item>
///             <description>
///                 <see cref="NotNullOrEmpty(string?, string?)" /> - Validates non-null and non-empty strings
///             </description>
///         </item>
///         <item>
///             <description>
///                 <see cref="NotNullOrWhiteSpace(string?, string?)" /> - Validates meaningful string content
///             </description>
///         </item>
///     </list>
/// </remarks>
/// <example>
///     <code>
/// // Before: Manual validation with verbose syntax
/// public void Process(string? name, IService? service)
/// {
///     _name = name ?? throw new ArgumentNullException(nameof(name));
///     _service = service ?? throw new ArgumentNullException(nameof(service));
/// }
///
/// // After: Clean, expressive guards
/// public void Process(string? name, IService? service)
/// {
///     _name = Guard.NotNull(name);
///     _service = Guard.NotNull(service);
/// }
/// </code>
/// </example>
#if ANCPLUA_ROSLYN_PUBLIC
public static partial class Guard
#else
internal static partial class Guard
#endif
{
    /// <summary>
    ///     Validates that a value is not <c>null</c> and returns it.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The value to validate.</param>
    /// <param name="paramName">
    ///     The name of the parameter (automatically captured via CallerArgumentExpressionAttribute).
    /// </param>
    /// <returns>The validated non-null value.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value" /> is <c>null</c>.</exception>
    /// <remarks>
    ///     <para>
    ///         This method eliminates the need for the null-forgiving operator (<c>!</c>) and
    ///         verbose <c>?? throw</c> patterns while maintaining proper null-state analysis.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    /// // Validates and returns the value, throws if null
    /// var validated = Guard.NotNull(possiblyNullValue);
    ///
    /// // Can be used inline
    /// ProcessItem(Guard.NotNull(item));
    /// </code>
    /// </example>
    public static T NotNull<T>([NotNull] T? value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
        => value ?? throw new ArgumentNullException(paramName);

    /// <summary>
    ///     Returns the value if not <c>null</c>, otherwise returns the specified default value.
    /// </summary>
    /// <typeparam name="T">The type of the value (must be a reference type).</typeparam>
    /// <param name="value">The value to check.</param>
    /// <param name="defaultValue">The default value to return if <paramref name="value" /> is <c>null</c>.</param>
    /// <returns>The original <paramref name="value" /> if not <c>null</c>; otherwise, <paramref name="defaultValue" />.</returns>
    /// <remarks>
    ///     <para>
    ///         Unlike <see cref="NotNull{T}" />, this method does not throw an exception.
    ///         Use this when a fallback value is acceptable.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    /// var name = Guard.NotNullOrElse(user?.Name, "Anonymous");
    /// var service = Guard.NotNullOrElse(optionalService, DefaultService.Instance);
    /// </code>
    /// </example>
    /// <seealso cref="NotNullOrElse{T}(T?, Func{T})" />
    public static T NotNullOrElse<T>(T? value, T defaultValue) where T : class
        => value ?? defaultValue;

    /// <summary>
    ///     Returns the value if not <c>null</c>, otherwise computes a default using the factory.
    /// </summary>
    /// <typeparam name="T">The type of the value (must be a reference type).</typeparam>
    /// <param name="value">The value to check.</param>
    /// <param name="factory">A factory function to compute the default value (only called if <paramref name="value" /> is <c>null</c>).</param>
    /// <returns>The original <paramref name="value" /> if not <c>null</c>; otherwise, the result of <paramref name="factory" />.</returns>
    /// <remarks>
    ///     <para>
    ///         Use this when the default value is expensive to compute and should only
    ///         be calculated when needed (lazy evaluation).
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    /// var config = Guard.NotNullOrElse(cachedConfig, () => LoadConfigFromDisk());
    /// var service = Guard.NotNullOrElse(injectedService, () => new ExpensiveService());
    /// </code>
    /// </example>
    /// <seealso cref="NotNullOrElse{T}(T?, T)" />
    public static T NotNullOrElse<T>(T? value, Func<T> factory) where T : class
        => value ?? factory();

    /// <summary>
    ///     Returns the value if it has a value, otherwise returns the specified default value.
    /// </summary>
    /// <typeparam name="T">The underlying value type.</typeparam>
    /// <param name="value">The nullable value to check.</param>
    /// <param name="defaultValue">The default value to return if <paramref name="value" /> has no value.</param>
    /// <returns>The <paramref name="value" /> if it has a value; otherwise, <paramref name="defaultValue" />.</returns>
    /// <remarks>
    ///     <para>
    ///         This is the value type overload of <see cref="NotNullOrElse{T}(T?, T)" />.
    ///         Useful for nullable value types like <c>int?</c>, <c>bool?</c>, etc.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    /// int? maybeCount = GetCount();
    /// var count = Guard.NotNullOrElse(maybeCount, 0);
    /// </code>
    /// </example>
    /// <seealso cref="NotNullOrElse{T}(T?, Func{T})" />
    public static T NotNullOrElse<T>(T? value, T defaultValue) where T : struct
        => value ?? defaultValue;

    /// <summary>
    ///     Returns the value if it has a value, otherwise computes a default using the factory.
    /// </summary>
    /// <typeparam name="T">The underlying value type.</typeparam>
    /// <param name="value">The nullable value to check.</param>
    /// <param name="factory">A factory function to compute the default value (only called if <paramref name="value" /> has no value).</param>
    /// <returns>The <paramref name="value" /> if it has a value; otherwise, the result of <paramref name="factory" />.</returns>
    /// <remarks>
    ///     <para>
    ///         Use this when the default value is expensive to compute and should only
    ///         be calculated when needed (lazy evaluation).
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    /// int? maybeCount = GetCount();
    /// var count = Guard.NotNullOrElse(maybeCount, () => ComputeExpensiveDefault());
    /// </code>
    /// </example>
    /// <seealso cref="NotNullOrElse{T}(T?, T)" />
    public static T NotNullOrElse<T>(T? value, Func<T> factory) where T : struct
        => value ?? factory();

    /// <summary>
    ///     Validates that a string is not <c>null</c> or empty and returns it.
    /// </summary>
    /// <param name="value">The string to validate.</param>
    /// <param name="paramName">
    ///     The name of the parameter (automatically captured via CallerArgumentExpressionAttribute).
    /// </param>
    /// <returns>The validated non-null, non-empty string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value" /> is empty.</exception>
    /// <example>
    ///     <code>
    /// public void SetName(string? name)
    /// {
    ///     _name = Guard.NotNullOrEmpty(name);
    /// }
    /// </code>
    /// </example>
    public static string NotNullOrEmpty([NotNull] string? value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value is null)
            throw new ArgumentNullException(paramName);
        if (value.Length is 0)
            throw new ArgumentException("Value cannot be empty.", paramName);
        return value;
    }

    /// <summary>
    ///     Validates that a string is not <c>null</c>, empty, or whitespace-only and returns it.
    /// </summary>
    /// <param name="value">The string to validate.</param>
    /// <param name="paramName">
    ///     The name of the parameter (automatically captured via CallerArgumentExpressionAttribute).
    /// </param>
    /// <returns>The validated string containing meaningful content.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value" /> is empty or contains only whitespace.</exception>
    /// <example>
    ///     <code>
    /// public void SetDescription(string? description)
    /// {
    ///     _description = Guard.NotNullOrWhiteSpace(description);
    /// }
    /// </code>
    /// </example>
    public static string NotNullOrWhiteSpace([NotNull] string? value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value is null)
            throw new ArgumentNullException(paramName);
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Value cannot be empty or whitespace.", paramName);
        return value;
    }

    /// <summary>
    ///     Returns the string if not <c>null</c> or empty, otherwise returns the specified default value.
    /// </summary>
    /// <param name="value">The string to check.</param>
    /// <param name="defaultValue">The default value to return if <paramref name="value" /> is <c>null</c> or empty.</param>
    /// <returns>The original <paramref name="value" /> if not <c>null</c> or empty; otherwise, <paramref name="defaultValue" />.</returns>
    /// <example>
    ///     <code>
    /// var displayName = Guard.NotNullOrEmptyOrElse(user.DisplayName, user.Username);
    /// </code>
    /// </example>
    /// <seealso cref="NotNullOrEmptyOrElse(string?, Func{string})" />
    public static string NotNullOrEmptyOrElse(string? value, string defaultValue)
        => value is { Length: > 0 } ? value : defaultValue;

    /// <summary>
    ///     Returns the string if not <c>null</c> or empty, otherwise computes a default using the factory.
    /// </summary>
    /// <param name="value">The string to check.</param>
    /// <param name="factory">A factory function to compute the default value (only called if <paramref name="value" /> is <c>null</c> or empty).</param>
    /// <returns>The original <paramref name="value" /> if not <c>null</c> or empty; otherwise, the result of <paramref name="factory" />.</returns>
    /// <example>
    ///     <code>
    /// var displayName = Guard.NotNullOrEmptyOrElse(user.DisplayName, () => GenerateDisplayName(user));
    /// </code>
    /// </example>
    /// <seealso cref="NotNullOrEmptyOrElse(string?, string)" />
    public static string NotNullOrEmptyOrElse(string? value, Func<string> factory)
        => value is { Length: > 0 } ? value : factory();

    /// <summary>
    ///     Returns the string if not <c>null</c>, empty, or whitespace, otherwise returns the specified default value.
    /// </summary>
    /// <param name="value">The string to check.</param>
    /// <param name="defaultValue">
    ///     The default value to return if <paramref name="value" /> is <c>null</c>, empty, or whitespace.
    /// </param>
    /// <returns>
    ///     The original <paramref name="value" /> if it contains meaningful content;
    ///     otherwise, <paramref name="defaultValue" />.
    /// </returns>
    /// <example>
    ///     <code>
    /// var title = Guard.NotNullOrWhiteSpaceOrElse(article.Title, "Untitled");
    /// </code>
    /// </example>
    /// <seealso cref="NotNullOrWhiteSpaceOrElse(string?, Func{string})" />
    public static string NotNullOrWhiteSpaceOrElse(string? value, string defaultValue)
        => value is not null && !string.IsNullOrWhiteSpace(value) ? value : defaultValue;

    /// <summary>
    ///     Returns the string if not <c>null</c>, empty, or whitespace, otherwise computes a default using the factory.
    /// </summary>
    /// <param name="value">The string to check.</param>
    /// <param name="factory">
    ///     A factory function to compute the default value (only called if <paramref name="value" /> is <c>null</c>, empty, or whitespace).
    /// </param>
    /// <returns>
    ///     The original <paramref name="value" /> if it contains meaningful content;
    ///     otherwise, the result of <paramref name="factory" />.
    /// </returns>
    /// <example>
    ///     <code>
    /// var title = Guard.NotNullOrWhiteSpaceOrElse(article.Title, () => GenerateTitle(article));
    /// </code>
    /// </example>
    /// <seealso cref="NotNullOrWhiteSpaceOrElse(string?, string)" />
    public static string NotNullOrWhiteSpaceOrElse(string? value, Func<string> factory)
        => value is not null && !string.IsNullOrWhiteSpace(value) ? value : factory();

    /// <summary>
    ///     Validates that a collection is not <c>null</c> or empty and returns it.
    /// </summary>
    /// <typeparam name="T">The type of elements in the collection.</typeparam>
    /// <param name="value">The collection to validate.</param>
    /// <param name="paramName">
    ///     The name of the parameter (automatically captured via CallerArgumentExpressionAttribute).
    /// </param>
    /// <returns>The validated non-null, non-empty collection.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value" /> is empty.</exception>
    /// <example>
    ///     <code>
    /// public void ProcessItems(IReadOnlyList&lt;Item&gt;? items)
    /// {
    ///     var validItems = Guard.NotNullOrEmpty(items);
    ///     foreach (var item in validItems) { ... }
    /// }
    /// </code>
    /// </example>
    public static IReadOnlyCollection<T> NotNullOrEmpty<T>(
        [NotNull] IReadOnlyCollection<T>? value,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value is null)
            throw new ArgumentNullException(paramName);
        if (value.Count is 0)
            throw new ArgumentException("Collection cannot be empty.", paramName);
        return value;
    }

    /// <summary>
    ///     Validates that a value is within the specified range (inclusive).
    /// </summary>
    /// <typeparam name="T">The type of the value (must implement <see cref="IComparable{T}" />).</typeparam>
    /// <param name="value">The value to validate.</param>
    /// <param name="min">The minimum allowed value (inclusive).</param>
    /// <param name="max">The maximum allowed value (inclusive).</param>
    /// <param name="paramName">
    ///     The name of the parameter (automatically captured via CallerArgumentExpressionAttribute).
    /// </param>
    /// <returns>The validated value.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     Thrown when <paramref name="value" /> is less than <paramref name="min" /> or greater than <paramref name="max" />.
    /// </exception>
    /// <example>
    ///     <code>
    /// public void SetPage(int page)
    /// {
    ///     _page = Guard.InRange(page, 1, 100);
    /// }
    /// </code>
    /// </example>
    public static T InRange<T>(T value, T min, T max, [CallerArgumentExpression(nameof(value))] string? paramName = null)
        where T : IComparable<T>
    {
        if (value.CompareTo(min) < 0 || value.CompareTo(max) > 0)
            throw new ArgumentOutOfRangeException(paramName, value, $"Value must be between {min} and {max}.");
        return value;
    }

    /// <summary>
    ///     Validates that an index is within valid bounds for a collection.
    /// </summary>
    /// <param name="index">The index to validate.</param>
    /// <param name="count">The total count of elements (exclusive upper bound).</param>
    /// <param name="paramName">
    ///     The name of the parameter (automatically captured via CallerArgumentExpressionAttribute).
    /// </param>
    /// <returns>The validated index.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     Thrown when <paramref name="index" /> is negative or greater than or equal to <paramref name="count" />.
    /// </exception>
    /// <example>
    ///     <code>
    /// public T GetItem(int index)
    /// {
    ///     Guard.ValidIndex(index, _items.Count);
    ///     return _items[index];
    /// }
    /// </code>
    /// </example>
    public static int ValidIndex(int index, int count, [CallerArgumentExpression(nameof(index))] string? paramName = null)
    {
        if (index < 0 || index >= count)
            throw new ArgumentOutOfRangeException(paramName, index, $"Index must be between 0 and {count - 1}.");
        return index;
    }

    /// <summary>
    ///     Validates that a condition is <c>true</c>.
    /// </summary>
    /// <param name="condition">The condition to validate.</param>
    /// <param name="message">The error message if the condition is <c>false</c>.</param>
    /// <param name="paramName">
    ///     The name of the parameter (automatically captured via CallerArgumentExpressionAttribute).
    /// </param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="condition" /> is <c>false</c>.</exception>
    /// <example>
    ///     <code>
    /// public void SetAge(int age)
    /// {
    ///     Guard.That(age >= 0, "Age cannot be negative.");
    ///     _age = age;
    /// }
    /// </code>
    /// </example>
    public static void That(
        [DoesNotReturnIf(false)] bool condition,
        string message,
        [CallerArgumentExpression(nameof(condition))] string? paramName = null)
    {
        if (!condition)
            throw new ArgumentException(message, paramName);
    }

    /// <summary>
    ///     Validates that a value matches a predicate.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The value to validate.</param>
    /// <param name="predicate">The predicate that must return <c>true</c> for the value to be valid.</param>
    /// <param name="message">The error message if the predicate returns <c>false</c>.</param>
    /// <param name="paramName">
    ///     The name of the parameter (automatically captured via CallerArgumentExpressionAttribute).
    /// </param>
    /// <returns>The validated value.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="predicate" /> returns <c>false</c>.</exception>
    /// <example>
    ///     <code>
    /// public void SetEmail(string email)
    /// {
    ///     _email = Guard.Satisfies(email, e => e.Contains('@'), "Invalid email format.");
    /// }
    /// </code>
    /// </example>
    public static T Satisfies<T>(
        T value,
        Func<T, bool> predicate,
        string message,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (!predicate(value))
            throw new ArgumentException(message, paramName);
        return value;
    }
}