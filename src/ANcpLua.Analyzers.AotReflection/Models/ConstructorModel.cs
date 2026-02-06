namespace ANcpLua.Analyzers.AotReflection;

internal readonly partial record struct ConstructorModel(
    string ContainingTypeFullyQualified,
    EquatableArray<ParameterModel> Parameters,
    string Accessibility)
    : IEquatable<ConstructorModel>;
