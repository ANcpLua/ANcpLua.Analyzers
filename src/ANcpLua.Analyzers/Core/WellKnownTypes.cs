using ANcpLua.Roslyn.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.Shared.Diagnostics;

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

    // Primitives
    SystemGuid,
    TimeSpan,
    DateTimeOffset,
    DateOnly,
    TimeOnly,
    Uri,

    // Roslyn types
    AttributeData,
    TypedConstant,

    Count // Must be last - used for array sizing
}

internal static partial class WellKnownTypeNames {
    private static readonly string[] Names = [
        "Microsoft.AspNetCore.Mvc.FromFormAttribute",
        "Microsoft.AspNetCore.Mvc.FromBodyAttribute",
        "Microsoft.AspNetCore.Http.IFormFile",
        "Microsoft.AspNetCore.Http.IFormFileCollection",
        "Microsoft.AspNetCore.Http.IFormCollection",
        "System.Guid",
        "System.TimeSpan",
        "System.DateTimeOffset",
        "System.DateOnly",
        "System.TimeOnly",
        "System.Uri",
        "Microsoft.CodeAnalysis.AttributeData",
        "Microsoft.CodeAnalysis.TypedConstant"
    ];

    public static string GetName(WellKnownType type) => Names[(int)type];
}

/// <summary>
///     Caches resolved INamedTypeSymbol instances for well-known types.
///     Create once per compilation via <see cref="Create" />.
/// </summary>
internal sealed partial class WellKnownTypeCache {
    private readonly INamedTypeSymbol?[] _cache = new INamedTypeSymbol?[(int)WellKnownType.Count];
    private readonly Compilation _compilation;

    private WellKnownTypeCache(Compilation compilation) => _compilation = compilation;

    public static WellKnownTypeCache Create(Compilation compilation) {
        Throw.IfNull(compilation);
        return new WellKnownTypeCache(compilation);
    }

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
               symbol.OriginalDefinition.IsEqualTo(wellKnown.OriginalDefinition);
    }

    public bool HasAttribute(ISymbol symbol, WellKnownType attributeType) {
        return Get(attributeType) is { } attrSymbol && symbol.HasAttribute(attrSymbol);
    }
}
