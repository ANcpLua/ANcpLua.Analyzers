namespace ANcpLua.Analyzers.AotReflection;

internal readonly partial record struct PropertyModel(
    string Name,
    string TypeFullyQualified,
    string ContainingTypeFullyQualified,
    bool IsStatic,
    bool IsNullable,
    bool HasGetter,
    bool HasSetter,
    bool IsInitOnly,
    string Accessibility)
    : IEquatable<PropertyModel>;
