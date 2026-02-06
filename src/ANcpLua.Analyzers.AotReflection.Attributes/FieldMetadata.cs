namespace ANcpLua.Analyzers.AotReflection;

using System.Reflection;

public sealed class FieldMetadata {
    public string Name { get; set; } = string.Empty;

    public Type Type { get; set; } = null!;

    public bool IsStatic { get; set; }

    public bool IsReadOnly { get; set; }

    public bool IsConst { get; set; }

    public object? ConstValue { get; set; }

    public FieldInfo ReflectionInfo { get; set; } = null!;

    public Func<object?, object?>? Getter { get; set; }

    public Action<object?, object?>? Setter { get; set; }
}
