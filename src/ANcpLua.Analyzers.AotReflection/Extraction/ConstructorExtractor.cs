namespace ANcpLua.Analyzers.AotReflection;

internal static partial class ConstructorExtractor {
    public static DiagnosticFlow<EquatableArray<ConstructorModel>> ExtractConstructors(
        INamedTypeSymbol type,
        AotReflectionOptions options,
        CancellationToken cancellationToken) {
        var constructors = new List<ConstructorModel>();
        var diagnostics = new List<DiagnosticInfo>();
        var constructorMatch = Match.Method().Constructor();

        var members = options.IncludeInherited
            ? type.GetAllMembers()
            : type.GetMembers();

        foreach (var member in members) {
            cancellationToken.ThrowIfCancellationRequested();

            if (member is not IMethodSymbol method) {
                continue;
            }

            if (!constructorMatch.Matches(method)) {
                continue;
            }

            if (method.MethodKind == MethodKind.StaticConstructor) {
                continue;
            }

            if (!options.IncludePrivate && method.DeclaredAccessibility != Accessibility.Public) {
                continue;
            }

            var parameters = ParameterExtractor.ExtractParameters(method, cancellationToken);

            constructors.Add(new ConstructorModel(
                ContainingTypeFullyQualified: type.GetFullyQualifiedName(),
                Parameters: parameters,
                Accessibility: method.DeclaredAccessibility.ToAccessibilityString()));
        }

        var flow = DiagnosticFlow.Ok(constructors.Count is 0 ? default : constructors.ToArray().ToEquatableArray());
        foreach (var diagnostic in diagnostics) {
            flow = flow.Warn(diagnostic);
        }

        return flow;
    }
}
