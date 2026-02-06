namespace ANcpLua.Analyzers.AotReflection;

using System.Reflection;

public sealed class MethodMetadata {
    public string Name { get; set; } = string.Empty;

    public Type ReturnType { get; set; } = null!;

    public ParameterMetadata[] Parameters { get; set; } = Array.Empty<ParameterMetadata>();

    public bool IsStatic { get; set; }

    public bool IsAsync { get; set; }

    public bool IsExtension { get; set; }

    public MethodInfo ReflectionInfo { get; set; } = null!;

    public Func<object?, object?[], object?>? Invoker { get; set; }
}
