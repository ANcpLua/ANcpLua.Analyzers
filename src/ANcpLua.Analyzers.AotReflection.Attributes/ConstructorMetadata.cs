namespace ANcpLua.Analyzers.AotReflection;

using System.Reflection;

public sealed class ConstructorMetadata {
    public ParameterMetadata[] Parameters { get; set; } = Array.Empty<ParameterMetadata>();

    public string Accessibility { get; set; } = "public";

    public ConstructorInfo ReflectionInfo { get; set; } = null!;

    public Func<object?[], object>? Factory { get; set; }
}
