namespace ANcpLua.Analyzers.AotReflection;

internal readonly partial record struct AotReflectionOptions(
    bool IncludeProperties,
    bool IncludeMethods,
    bool IncludeFields,
    bool IncludeConstructors,
    bool IncludeInherited,
    bool IncludePrivate)
    : IEquatable<AotReflectionOptions> {
    public static AotReflectionOptions Default => new(
        IncludeProperties: true,
        IncludeMethods: true,
        IncludeFields: false,
        IncludeConstructors: true,
        IncludeInherited: false,
        IncludePrivate: false);

    public static AotReflectionOptions From(ImmutableArray<AttributeData> attributes)
        => attributes.Length is 0 ? Default : From(attributes[0]);

    public static AotReflectionOptions From(AttributeData attribute) {
        var options = Default;
        foreach (var argument in attribute.NamedArguments) {
            if (argument.Value.Value is not bool value) {
                continue;
            }

            options = argument.Key switch {
                nameof(IncludeProperties) => options with { IncludeProperties = value },
                nameof(IncludeMethods) => options with { IncludeMethods = value },
                nameof(IncludeFields) => options with { IncludeFields = value },
                nameof(IncludeConstructors) => options with { IncludeConstructors = value },
                nameof(IncludeInherited) => options with { IncludeInherited = value },
                nameof(IncludePrivate) => options with { IncludePrivate = value },
                _ => options
            };
        }

        return options;
    }
}
