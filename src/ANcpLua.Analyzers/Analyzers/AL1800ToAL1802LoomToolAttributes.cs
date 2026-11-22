namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     Shared <c>[LoomTool]</c> attribute discovery for the AL1800–AL1802 tool-governance rules.
/// </summary>
/// <remarks>
///     Attributes are matched by short name (with or without the <c>Attribute</c> suffix) via
///     <see cref="SymbolExtensions.HasAttributeByShortName" />, so any namespace's
///     <c>LoomToolAttribute</c> counts.
/// </remarks>
internal static partial class Al1800ToAl1802LoomToolAttributes {
    private const string LoomToolAttributeName = "LoomTool";

    /// <summary>Returns <c>true</c> when the method itself carries <c>[LoomTool]</c>.</summary>
    public static bool IsLoomTool(IMethodSymbol method) =>
        method.HasAttributeByShortName(LoomToolAttributeName);

    /// <summary>
    ///     Returns <c>true</c> when the attribute is declared on the method or on its containing type.
    /// </summary>
    public static bool HasAttributeOnMethodOrType(IMethodSymbol method, string attributeName) =>
        method.HasAttributeByShortName(attributeName) ||
        method.ContainingType?.HasAttributeByShortName(attributeName) is true;
}
