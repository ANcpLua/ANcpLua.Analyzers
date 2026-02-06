namespace ANcpLua.Analyzers.AotReflection;

internal static partial class ParameterExtractor {
    public static EquatableArray<ParameterModel> ExtractParameters(IMethodSymbol method, CancellationToken cancellationToken) {
        if (method.Parameters.Length is 0) {
            return default;
        }

        var parameters = new ParameterModel[method.Parameters.Length];
        for (var i = 0; i < method.Parameters.Length; i++) {
            var parameter = method.Parameters[i];
            var hasDefaultValue = parameter.HasExplicitDefaultValue;
            var defaultLiteral = hasDefaultValue
                ? LiteralFormatter.GetDefaultValueLiteral(parameter, cancellationToken)
                : null;

            parameters[i] = new ParameterModel(
                Name: parameter.Name,
                TypeFullyQualified: parameter.Type.GetFullyQualifiedName(),
                IsNullable: IsNullable(parameter.Type),
                HasDefaultValue: hasDefaultValue && defaultLiteral is not null,
                DefaultValueLiteral: defaultLiteral);
        }

        return parameters.ToEquatableArray();
    }

    private static bool IsNullable(ITypeSymbol typeSymbol) {
        if (typeSymbol is INamedTypeSymbol { ConstructedFrom.SpecialType: SpecialType.System_Nullable_T }) {
            return true;
        }

        return typeSymbol.NullableAnnotation == NullableAnnotation.Annotated;
    }
}
