namespace ANcpLua.Analyzers.AotReflection;

internal readonly partial record struct ParameterModel(
    string Name,
    string TypeFullyQualified,
    bool IsNullable,
    bool HasDefaultValue,
    string? DefaultValueLiteral)
    : IEquatable<ParameterModel>;
