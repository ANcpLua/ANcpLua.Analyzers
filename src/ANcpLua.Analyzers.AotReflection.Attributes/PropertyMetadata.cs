namespace ANcpLua.Analyzers.AotReflection;

#if NET6_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif
using System.Reflection;

public sealed class PropertyMetadata {
#if NET6_0_OR_GREATER
    [DynamicallyAccessedMembers(
        DynamicallyAccessedMemberTypes.PublicProperties |
        DynamicallyAccessedMemberTypes.NonPublicProperties)]
#endif
    public Type Type { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    public PropertyInfo ReflectionInfo { get; set; } = null!;

    public bool IsStatic { get; set; }

    public bool IsNullable { get; set; }

    public Func<object?, object?>? Getter { get; set; }

    public Action<object?, object?>? Setter { get; set; }

    public ClassMetadata ClassMetadata { get; internal set; } = null!;
}
