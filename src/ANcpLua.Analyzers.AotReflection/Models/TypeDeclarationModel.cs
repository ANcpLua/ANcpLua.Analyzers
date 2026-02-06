namespace ANcpLua.Analyzers.AotReflection;

internal readonly partial record struct TypeDeclarationModel(
    string Name,
    string Keyword,
    string Modifiers,
    string TypeParameters,
    EquatableArray<string> ConstraintClauses)
    : IEquatable<TypeDeclarationModel>;
