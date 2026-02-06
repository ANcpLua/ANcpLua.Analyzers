namespace ANcpLua.Analyzers.AotReflection;

internal readonly partial record struct TypeModel(
    string FullyQualifiedName,
    string Namespace,
    string Name,
    string Accessibility,
    bool IsStatic,
    bool IsSealed,
    bool IsAbstract,
    string? BaseTypeFullyQualified,
    EquatableArray<string> Interfaces,
    EquatableArray<TypeDeclarationModel> DeclarationChain,
    EquatableArray<PropertyModel> Properties,
    EquatableArray<MethodModel> Methods,
    EquatableArray<FieldModel> Fields,
    EquatableArray<ConstructorModel> Constructors,
    AotReflectionOptions Options)
    : IEquatable<TypeModel>;
