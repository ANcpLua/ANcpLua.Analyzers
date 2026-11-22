namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     Attribute lookup shared by the AL14xx AOT rules.
/// </summary>
internal static class Al14xxAotAttributeLookup {
    /// <summary>
    ///     Returns <see langword="true" /> when <paramref name="method" /> or any of its containing types
    ///     (walking outward through nested types) carries an attribute with the given short name.
    /// </summary>
    internal static bool HasAttributeOnMethodOrContainingTypes(IMethodSymbol method, string attributeName) {
        if (method.HasAttributeByShortName(attributeName)) {
            return true;
        }

        for (var type = method.ContainingType; type is not null; type = type.ContainingType) {
            if (type.HasAttributeByShortName(attributeName)) {
                return true;
            }
        }

        return false;
    }
}
