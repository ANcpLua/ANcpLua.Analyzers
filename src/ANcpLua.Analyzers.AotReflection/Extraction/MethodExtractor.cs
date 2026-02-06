namespace ANcpLua.Analyzers.AotReflection;

internal static partial class MethodExtractor {
    public static DiagnosticFlow<EquatableArray<MethodModel>> ExtractMethods(
        INamedTypeSymbol type,
        AotReflectionOptions options,
        CancellationToken cancellationToken) {
        var methods = new List<MethodModel>();
        var diagnostics = new List<DiagnosticInfo>();
        var constructorMatch = Match.Method().Constructor();
        var finalizerMatch = Match.Method().Finalizer();
        var extensionMatch = Match.Method().Extension();
        var genericMatch = Match.Method().Generic();

        var members = options.IncludeInherited
            ? type.GetAllMembers()
            : type.GetMembers();

        foreach (var member in members) {
            cancellationToken.ThrowIfCancellationRequested();

            if (member is not IMethodSymbol method) {
                continue;
            }

            if (method.IsImplicitlyDeclared) {
                continue;
            }

            if (constructorMatch.Matches(method) || finalizerMatch.Matches(method)) {
                continue;
            }

            if (method.AssociatedSymbol is not null) {
                continue;
            }

            if (method.MethodKind != MethodKind.Ordinary) {
                continue;
            }

            if (!options.IncludePrivate && method.DeclaredAccessibility != Accessibility.Public) {
                continue;
            }

            if (genericMatch.Matches(method)) {
                diagnostics.Add(DiagnosticInfo.Create(
                    DiagnosticDescriptors.GenericMethodNotSupported,
                    method,
                    method.Name));
                continue;
            }

            var parameters = ParameterExtractor.ExtractParameters(method, cancellationToken);

            methods.Add(new MethodModel(
                Name: method.Name,
                ReturnTypeFullyQualified: method.ReturnType.GetFullyQualifiedName(),
                ContainingTypeFullyQualified: type.GetFullyQualifiedName(),
                Parameters: parameters,
                IsStatic: method.IsStatic,
                IsAsync: method.IsAsync,
                IsExtension: extensionMatch.Matches(method),
                IsGeneric: method.IsGenericMethod,
                ReturnsVoid: method.ReturnsVoid,
                Accessibility: method.DeclaredAccessibility.ToAccessibilityString()));
        }

        var flow = DiagnosticFlow.Ok(methods.Count is 0 ? default : methods.ToArray().ToEquatableArray());
        foreach (var diagnostic in diagnostics) {
            flow = flow.Warn(diagnostic);
        }

        return flow;
    }
}
