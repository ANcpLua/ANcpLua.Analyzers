namespace ANcpLua.Analyzers.AotReflection;

internal static partial class FieldCodeGenerator {
    public static void WriteFieldMetadataArray(IndentedStringBuilder sb, TypeModel type) {
        if (type.Fields.IsEmpty) {
            sb.AppendLine("Fields = global::System.Array.Empty<global::ANcpLua.Analyzers.AotReflection.FieldMetadata>(),");
            return;
        }

        sb.AppendLine("Fields = new global::ANcpLua.Analyzers.AotReflection.FieldMetadata[]");
        sb.BeginBlock();

        foreach (var field in type.Fields) {
            sb.AppendLine("new global::ANcpLua.Analyzers.AotReflection.FieldMetadata");
            sb.BeginBlock();

            sb.AppendLine($"Name = {GenerationHelpers.StringLiteral(field.Name)},");
            sb.AppendLine($"Type = {GenerationHelpers.GetTypeOf(field.TypeFullyQualified)},");
            sb.AppendLine($"IsStatic = {field.IsStatic.ToString().ToLowerInvariant()},");
            sb.AppendLine($"IsReadOnly = {field.IsReadOnly.ToString().ToLowerInvariant()},");
            sb.AppendLine($"IsConst = {field.IsConst.ToString().ToLowerInvariant()},");

            var constValue = field.IsConst && field.ConstValue is not null ? field.ConstValue : "null";
            sb.AppendLine($"ConstValue = {constValue},");

            sb.AppendLine($"ReflectionInfo = {GenerationHelpers.GetTypeOf(field.ContainingTypeFullyQualified)}.GetField({GenerationHelpers.StringLiteral(field.Name)}, {GenerationHelpers.BindingFlagsAll}),");
            sb.AppendLine($"Getter = {GetGetterExpression(field)},");
            sb.AppendLine($"Setter = {GetSetterExpression(field)}");

            sb.EndBlock("},");
        }

        sb.EndBlock("},");
    }

    private static string GetGetterExpression(FieldModel field) {
        if (field.IsConst && field.ConstValue is not null) {
            return $"_ => {field.ConstValue}";
        }

        if (field.IsStatic) {
            return $"_ => {field.ContainingTypeFullyQualified}.{field.Name}";
        }

        return $"obj => (({field.ContainingTypeFullyQualified})obj!).{field.Name}";
    }

    private static string GetSetterExpression(FieldModel field) {
        if (field.IsConst || field.IsReadOnly) {
            return "null";
        }

        var castValue = $"({field.TypeFullyQualified})value!";

        if (field.IsStatic) {
            return $"(_, value) => {field.ContainingTypeFullyQualified}.{field.Name} = {castValue}";
        }

        return $"(obj, value) => (({field.ContainingTypeFullyQualified})obj!).{field.Name} = {castValue}";
    }
}
