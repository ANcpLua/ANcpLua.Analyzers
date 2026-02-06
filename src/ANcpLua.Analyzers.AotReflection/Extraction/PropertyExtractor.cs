namespace ANcpLua.Analyzers.AotReflection;

internal static partial class PropertyExtractor {
    public static DiagnosticFlow<EquatableArray<PropertyModel>> ExtractProperties(
        INamedTypeSymbol type,
        AotReflectionOptions options,
        CancellationToken cancellationToken) {
        var diagnostics = new List<DiagnosticInfo>();
        var properties = new List<PropertyModel>();
        var indexerMatch = Match.Property().Indexer();

        var members = options.IncludeInherited
            ? type.GetAllMembers()
            : type.GetMembers();

        foreach (var member in members) {
            cancellationToken.ThrowIfCancellationRequested();

            if (member is not IPropertySymbol property) {
                continue;
            }

            if (property.IsImplicitlyDeclared) {
                continue;
            }

            if (!options.IncludePrivate && property.DeclaredAccessibility != Accessibility.Public) {
                continue;
            }

            if (indexerMatch.Matches(property)) {
                diagnostics.Add(DiagnosticInfo.Create(
                    DiagnosticDescriptors.IndexerNotSupported,
                    property,
                    property.Name));
                continue;
            }

            var hasGetter = property.GetMethod is not null;
            var hasSetter = property.SetMethod is not null;

            properties.Add(new PropertyModel(
                Name: property.Name,
                TypeFullyQualified: property.Type.GetFullyQualifiedName(),
                ContainingTypeFullyQualified: type.GetFullyQualifiedName(),
                IsStatic: property.IsStatic,
                IsNullable: IsNullable(property.Type),
                HasGetter: hasGetter,
                HasSetter: hasSetter,
                IsInitOnly: property.SetMethod?.IsInitOnly == true,
                Accessibility: property.DeclaredAccessibility.ToAccessibilityString()));
        }

        var flow = DiagnosticFlow.Ok(properties.Count is 0 ? default : properties.ToArray().ToEquatableArray());
        foreach (var diagnostic in diagnostics) {
            flow = flow.Warn(diagnostic);
        }

        return flow;
    }

    private static bool IsNullable(ITypeSymbol typeSymbol) {
        if (typeSymbol is INamedTypeSymbol { ConstructedFrom.SpecialType: SpecialType.System_Nullable_T }) {
            return true;
        }

        return typeSymbol.NullableAnnotation == NullableAnnotation.Annotated;
    }
}
