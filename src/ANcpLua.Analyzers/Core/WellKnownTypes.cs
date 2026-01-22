namespace ANcpLua.Analyzers.Core;

/// <summary>
///     Well-known types used by analyzers.
///     Following Roslyn's WellKnownType pattern for efficient symbol resolution.
/// </summary>
internal enum WellKnownType {
    // Binding attributes
    FromFormAttribute,
    FromBodyAttribute,

    // Form types
    IFormFile,
    IFormFileCollection,
    IFormCollection,

    Count // Must be last - used for array sizing
}

internal static class WellKnownTypeNames {
    private static readonly string[] Names = [
        "Microsoft.AspNetCore.Mvc.FromFormAttribute",
        "Microsoft.AspNetCore.Mvc.FromBodyAttribute",
        "Microsoft.AspNetCore.Http.IFormFile",
        "Microsoft.AspNetCore.Http.IFormFileCollection",
        "Microsoft.AspNetCore.Http.IFormCollection"
    ];

    public static string GetName(WellKnownType type) => Names[(int)type];
}

/// <summary>
///     Caches resolved INamedTypeSymbol instances for well-known types.
///     Create once per compilation via <see cref="Create" />.
/// </summary>
internal sealed class WellKnownTypeCache {
    private readonly INamedTypeSymbol?[] _cache = new INamedTypeSymbol?[(int)WellKnownType.Count];
    private readonly Compilation _compilation;

    private WellKnownTypeCache(Compilation compilation) => _compilation = compilation;

    public static WellKnownTypeCache Create(Compilation compilation) => new(compilation);

    public INamedTypeSymbol? Get(WellKnownType type) {
        var index = (int)type;
        return _cache[index] ??= _compilation.GetTypeByMetadataName(WellKnownTypeNames.GetName(type));
    }

    public bool IsType(ITypeSymbol? symbol, WellKnownType type) {
        if (symbol is null) {
            return false;
        }

        var wellKnown = Get(type);
        return wellKnown is not null &&
               SymbolEqualityComparer.Default.Equals(symbol.OriginalDefinition, wellKnown.OriginalDefinition);
    }

    public bool HasAttribute(ISymbol symbol, WellKnownType attributeType) {
        if (Get(attributeType) is not { } attrSymbol) {
            return false;
        }

        return symbol.GetAttributes().Any(attr =>
            attr.AttributeClass is not null &&
            SymbolEqualityComparer.Default.Equals(attr.AttributeClass.OriginalDefinition,
                attrSymbol.OriginalDefinition));
    }
}
