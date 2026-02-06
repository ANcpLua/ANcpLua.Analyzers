namespace ANcpLua.Analyzers.AotReflection;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
public sealed class AotReflectionAttribute : Attribute {
    public bool IncludeProperties { get; set; } = true;
    public bool IncludeMethods { get; set; } = true;
    public bool IncludeFields { get; set; } = false;
    public bool IncludeConstructors { get; set; } = true;
    public bool IncludeInherited { get; set; } = false;
    public bool IncludePrivate { get; set; } = false;
}
