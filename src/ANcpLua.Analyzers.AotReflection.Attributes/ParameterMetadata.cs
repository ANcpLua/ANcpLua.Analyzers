namespace ANcpLua.Analyzers.AotReflection;

public sealed class ParameterMetadata {
    public string Name { get; set; } = string.Empty;

    public Type Type { get; set; } = null!;

    public bool IsNullable { get; set; }

    public bool HasDefaultValue { get; set; }

    public object? DefaultValue { get; set; }
}
