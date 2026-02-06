namespace ANcpLua.Analyzers.AotReflection;

internal static partial class ClassMetadataGenerator {
    public static void WriteClassMetadata(IndentedStringBuilder sb, TypeModel type) {
        sb.AppendLine("public static global::ANcpLua.Analyzers.AotReflection.ClassMetadata Metadata { get; } = new global::ANcpLua.Analyzers.AotReflection.ClassMetadata");
        sb.BeginBlock();

        sb.AppendLine($"Type = {GenerationHelpers.GetTypeOf(type.FullyQualifiedName)},");
        sb.AppendLine($"Name = {GenerationHelpers.StringLiteral(type.Name)},");
        sb.AppendLine($"Namespace = {GenerationHelpers.StringLiteral(type.Namespace)},");
        sb.AppendLine($"IsStatic = {type.IsStatic.ToString().ToLowerInvariant()},");
        sb.AppendLine($"IsSealed = {type.IsSealed.ToString().ToLowerInvariant()},");
        sb.AppendLine($"IsAbstract = {type.IsAbstract.ToString().ToLowerInvariant()},");
        sb.AppendLine("BaseType = null,");

        PropertyCodeGenerator.WritePropertyMetadataArray(sb, type);
        MethodCodeGenerator.WriteMethodMetadataArray(sb, type);
        FieldCodeGenerator.WriteFieldMetadataArray(sb, type);
        MethodCodeGenerator.WriteConstructorMetadataArray(sb, type);

        sb.EndBlock("};");
    }

    public static void WriteConvenienceMethods(IndentedStringBuilder sb, TypeModel type) {
        WriteGetPropertyValue(sb, type);
        WriteSetPropertyValue(sb, type);

        sb.AppendLine("public static object? InvokeMethod(object? instance, string name, params object?[] args)");
        sb.BeginBlock();
        sb.AppendLine("return Metadata.InvokeMethod(instance, name, args);");
        sb.EndBlock();

        sb.AppendLine("public static object CreateInstance(params object?[] args)");
        sb.BeginBlock();
        sb.AppendLine("return Metadata.CreateInstance(args);");
        sb.EndBlock();
    }

    private static void WriteGetPropertyValue(IndentedStringBuilder sb, TypeModel type) {
        sb.AppendLine("public static object? GetPropertyValue(object instance, string name)");
        sb.BeginBlock();

        if (type.Properties.IsEmpty || !type.Properties.Any(static property => property.HasGetter)) {
            sb.AppendLine("throw new global::System.ArgumentException(\"No readable properties available.\", nameof(name));");
            sb.EndBlock();
            return;
        }

        sb.AppendLine("return name switch");
        sb.BeginBlock();

        foreach (var property in type.Properties) {
            if (!property.HasGetter) {
                continue;
            }

            var access = property.IsStatic
                ? $"{property.ContainingTypeFullyQualified}.{property.Name}"
                : $"(({property.ContainingTypeFullyQualified})instance!).{property.Name}";

            sb.AppendLine($"{GenerationHelpers.StringLiteral(property.Name)} => {access},");
        }

        sb.AppendLine("_ => throw new global::System.ArgumentException(\"Unknown property name.\", nameof(name))");
        sb.EndBlock("};");
        sb.EndBlock();
    }

    private static void WriteSetPropertyValue(IndentedStringBuilder sb, TypeModel type) {
        sb.AppendLine("public static void SetPropertyValue(object instance, string name, object? value)");
        sb.BeginBlock();

        if (type.Properties.IsEmpty || !type.Properties.Any(static property => property.HasSetter && !property.IsInitOnly)) {
            sb.AppendLine("throw new global::System.ArgumentException(\"No writable properties available.\", nameof(name));");
            sb.EndBlock();
            return;
        }

        sb.AppendLine("switch (name)");
        sb.BeginBlock();

        foreach (var property in type.Properties) {
            if (!property.HasSetter || property.IsInitOnly) {
                continue;
            }

            var target = property.IsStatic
                ? $"{property.ContainingTypeFullyQualified}.{property.Name}"
                : $"(({property.ContainingTypeFullyQualified})instance!).{property.Name}";
            var assignment = $"{target} = ({property.TypeFullyQualified})value!;";

            sb.AppendLine($"case {GenerationHelpers.StringLiteral(property.Name)}:");
            sb.AppendLine(assignment);
            sb.AppendLine("break;");
        }

        sb.AppendLine("default:");
        sb.AppendLine("throw new global::System.ArgumentException(\"Unknown property name.\", nameof(name));");
        sb.EndBlock();
        sb.EndBlock();
    }
}
