namespace ANcpLua.Analyzers.AotReflection;

internal readonly partial record struct FieldModel(
    string Name,
    string TypeFullyQualified,
    string ContainingTypeFullyQualified,
    bool IsStatic,
    bool IsReadOnly,
    bool IsConst,
    string? ConstValue,
    string Accessibility)
    : IEquatable<FieldModel>;
