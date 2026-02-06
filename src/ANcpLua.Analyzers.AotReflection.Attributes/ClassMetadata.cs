namespace ANcpLua.Analyzers.AotReflection;

#if NET6_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

public sealed class ClassMetadata {
#if NET6_0_OR_GREATER
    [DynamicallyAccessedMembers(
        DynamicallyAccessedMemberTypes.PublicConstructors |
        DynamicallyAccessedMemberTypes.NonPublicConstructors |
        DynamicallyAccessedMemberTypes.PublicMethods |
        DynamicallyAccessedMemberTypes.NonPublicMethods |
        DynamicallyAccessedMemberTypes.PublicFields |
        DynamicallyAccessedMemberTypes.NonPublicFields |
        DynamicallyAccessedMemberTypes.PublicProperties |
        DynamicallyAccessedMemberTypes.NonPublicProperties)]
#endif
    public Type Type { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    public string Namespace { get; set; } = string.Empty;

    public bool IsStatic { get; set; }

    public bool IsSealed { get; set; }

    public bool IsAbstract { get; set; }

    public ClassMetadata? BaseType { get; set; }

    private PropertyMetadata[] _properties = Array.Empty<PropertyMetadata>();

    public PropertyMetadata[] Properties {
        get => _properties;
        set {
            _properties = value ?? Array.Empty<PropertyMetadata>();
            foreach (var property in _properties) {
                property.ClassMetadata = this;
            }
        }
    }

    public MethodMetadata[] Methods { get; set; } = Array.Empty<MethodMetadata>();

    public FieldMetadata[] Fields { get; set; } = Array.Empty<FieldMetadata>();

    public ConstructorMetadata[] Constructors { get; set; } = Array.Empty<ConstructorMetadata>();

    public object? GetPropertyValue(object instance, string name) {
        foreach (var property in Properties) {
            if (!string.Equals(property.Name, name, StringComparison.Ordinal)) {
                continue;
            }

            if (property.Getter is null) {
                throw new InvalidOperationException($"Property '{name}' does not have a getter.");
            }

            return property.Getter(property.IsStatic ? null : instance);
        }

        throw new ArgumentException("Unknown property name.", nameof(name));
    }

    public void SetPropertyValue(object instance, string name, object? value) {
        foreach (var property in Properties) {
            if (!string.Equals(property.Name, name, StringComparison.Ordinal)) {
                continue;
            }

            if (property.Setter is null) {
                throw new InvalidOperationException($"Property '{name}' does not have a setter.");
            }

            property.Setter(property.IsStatic ? null : instance, value);
            return;
        }

        throw new ArgumentException("Unknown property name.", nameof(name));
    }

    public object? InvokeMethod(object? instance, string name, params object?[] args) {
        args ??= Array.Empty<object?>();

        foreach (var method in Methods) {
            if (!string.Equals(method.Name, name, StringComparison.Ordinal)) {
                continue;
            }

            if (method.Parameters.Length != args.Length) {
                continue;
            }

            if (method.Invoker is null) {
                throw new InvalidOperationException($"Method '{name}' does not have an invoker.");
            }

            return method.Invoker(method.IsStatic ? null : instance, args);
        }

        throw new ArgumentException("Unknown method name or parameter count.", nameof(name));
    }

    public object CreateInstance(params object?[] args) {
        args ??= Array.Empty<object?>();

        foreach (var constructor in Constructors) {
            if (constructor.Parameters.Length != args.Length) {
                continue;
            }

            if (constructor.Factory is null) {
                throw new InvalidOperationException("Constructor does not have a factory.");
            }

            return constructor.Factory(args);
        }

        throw new ArgumentException("No matching constructor found.", nameof(args));
    }
}
