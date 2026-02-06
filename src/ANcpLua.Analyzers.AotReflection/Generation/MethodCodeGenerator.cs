namespace ANcpLua.Analyzers.AotReflection;

internal static partial class MethodCodeGenerator {
    public static void WriteMethodMetadataArray(IndentedStringBuilder sb, TypeModel type) {
        if (type.Methods.IsEmpty) {
            sb.AppendLine("Methods = global::System.Array.Empty<global::ANcpLua.Analyzers.AotReflection.MethodMetadata>(),");
            return;
        }

        sb.AppendLine("Methods = new global::ANcpLua.Analyzers.AotReflection.MethodMetadata[]");
        sb.BeginBlock();

        foreach (var method in type.Methods) {
            sb.AppendLine("new global::ANcpLua.Analyzers.AotReflection.MethodMetadata");
            sb.BeginBlock();

            sb.AppendLine($"Name = {GenerationHelpers.StringLiteral(method.Name)},");
            sb.AppendLine($"ReturnType = {GenerationHelpers.GetTypeOf(method.ReturnTypeFullyQualified)},");
            sb.AppendLine($"IsStatic = {method.IsStatic.ToString().ToLowerInvariant()},");
            sb.AppendLine($"IsAsync = {method.IsAsync.ToString().ToLowerInvariant()},");
            sb.AppendLine($"IsExtension = {method.IsExtension.ToString().ToLowerInvariant()},");

            WriteParameterMetadataArray(sb, method.Parameters);

            sb.AppendLine($"ReflectionInfo = {GetMethodInfoExpression(method)},");
            sb.AppendLine($"Invoker = {GetInvokerExpression(method)}");

            sb.EndBlock("},");
        }

        sb.EndBlock("},");
    }

    public static void WriteConstructorMetadataArray(IndentedStringBuilder sb, TypeModel type) {
        if (type.Constructors.IsEmpty) {
            sb.AppendLine("Constructors = global::System.Array.Empty<global::ANcpLua.Analyzers.AotReflection.ConstructorMetadata>(),");
            return;
        }

        sb.AppendLine("Constructors = new global::ANcpLua.Analyzers.AotReflection.ConstructorMetadata[]");
        sb.BeginBlock();

        foreach (var constructor in type.Constructors) {
            sb.AppendLine("new global::ANcpLua.Analyzers.AotReflection.ConstructorMetadata");
            sb.BeginBlock();

            WriteParameterMetadataArray(sb, constructor.Parameters);

            sb.AppendLine($"Accessibility = {GenerationHelpers.StringLiteral(constructor.Accessibility)},");
            sb.AppendLine($"ReflectionInfo = {GetConstructorInfoExpression(constructor)},");
            sb.AppendLine($"Factory = {GetFactoryExpression(constructor)}");

            sb.EndBlock("},");
        }

        sb.EndBlock("},");
    }

    private static void WriteParameterMetadataArray(IndentedStringBuilder sb, EquatableArray<ParameterModel> parameters) {
        if (parameters.IsEmpty) {
            sb.AppendLine("Parameters = global::System.Array.Empty<global::ANcpLua.Analyzers.AotReflection.ParameterMetadata>(),");
            return;
        }

        sb.AppendLine("Parameters = new global::ANcpLua.Analyzers.AotReflection.ParameterMetadata[]");
        sb.BeginBlock();

        foreach (var parameter in parameters) {
            sb.AppendLine("new global::ANcpLua.Analyzers.AotReflection.ParameterMetadata");
            sb.BeginBlock();

            sb.AppendLine($"Name = {GenerationHelpers.StringLiteral(parameter.Name)},");
            sb.AppendLine($"Type = {GenerationHelpers.GetTypeOf(parameter.TypeFullyQualified)},");
            sb.AppendLine($"IsNullable = {parameter.IsNullable.ToString().ToLowerInvariant()},");
            sb.AppendLine($"HasDefaultValue = {parameter.HasDefaultValue.ToString().ToLowerInvariant()},");

            var defaultValue = parameter.HasDefaultValue && parameter.DefaultValueLiteral is not null
                ? parameter.DefaultValueLiteral
                : "null";
            sb.AppendLine($"DefaultValue = {defaultValue}");

            sb.EndBlock("},");
        }

        sb.EndBlock("},");
    }

    private static string GetMethodInfoExpression(MethodModel method) {
        var parameterTypes = GetParameterTypesArray(method.Parameters);
        return $"{GenerationHelpers.GetTypeOf(method.ContainingTypeFullyQualified)}.GetMethod({GenerationHelpers.StringLiteral(method.Name)}, {GenerationHelpers.BindingFlagsAll}, null, {parameterTypes}, null)";
    }

    private static string GetConstructorInfoExpression(ConstructorModel constructor) {
        var parameterTypes = GetParameterTypesArray(constructor.Parameters);
        return $"{GenerationHelpers.GetTypeOf(constructor.ContainingTypeFullyQualified)}.GetConstructor({GenerationHelpers.BindingFlagsAll}, null, {parameterTypes}, null)";
    }

    private static string GetInvokerExpression(MethodModel method) {
        var arguments = GetArgumentList(method.Parameters);
        var callTarget = method.IsStatic
            ? $"{method.ContainingTypeFullyQualified}.{method.Name}"
            : $"(({method.ContainingTypeFullyQualified})obj!).{method.Name}";

        if (method.ReturnsVoid) {
            return $"(obj, args) => {{ {callTarget}({arguments}); return null; }}";
        }

        return $"(obj, args) => {callTarget}({arguments})";
    }

    private static string GetFactoryExpression(ConstructorModel constructor) {
        var arguments = GetArgumentList(constructor.Parameters);
        return $"args => new {constructor.ContainingTypeFullyQualified}({arguments})";
    }

    private static string GetArgumentList(EquatableArray<ParameterModel> parameters) {
        if (parameters.IsEmpty) {
            return string.Empty;
        }

        var parts = new string[parameters.Length];
        for (var i = 0; i < parts.Length; i++) {
            var parameter = parameters[i];
            parts[i] = $"({parameter.TypeFullyQualified})args[{i}]!";
        }

        return string.Join(", ", parts);
    }

    private static string GetParameterTypesArray(EquatableArray<ParameterModel> parameters) {
        if (parameters.IsEmpty) {
            return "global::System.Type.EmptyTypes";
        }

        var types = new string[parameters.Length];
        for (var i = 0; i < types.Length; i++) {
            var parameter = parameters[i];
            types[i] = GenerationHelpers.GetTypeOf(parameter.TypeFullyQualified);
        }

        return $"new global::System.Type[] {{ {string.Join(", ", types)} }}";
    }
}
