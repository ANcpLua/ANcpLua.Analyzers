namespace ANcpLua.Analyzers.AotReflection;

internal static partial class TypeExtractor {
    public static DiagnosticFlow<TypeModel> ExtractTypeModel(
        GeneratorAttributeSyntaxContext context,
        CancellationToken cancellationToken) {
        cancellationToken.ThrowIfCancellationRequested();

        if (context.TargetSymbol is not INamedTypeSymbol typeSymbol ||
            context.TargetNode is not TypeDeclarationSyntax typeDeclaration) {
            return DiagnosticFlow.Fail<TypeModel>(DiagnosticInfo.Create(
                DiagnosticDescriptors.InvalidTarget,
                context.TargetNode));
        }

        var typeMatch = Match.Type().Class();
        var structMatch = Match.Type().Struct();
        var recordMatch = Match.Type().Record();

        var typeFlow = SemanticGuard.ForType(typeSymbol)
            .Must(
                symbol => typeMatch.Matches(symbol) || structMatch.Matches(symbol) || recordMatch.Matches(symbol),
                DiagnosticInfo.Create(DiagnosticDescriptors.InvalidTarget, typeSymbol))
            .ToFlow();

        var declarationsFlow = DeclarationChainExtractor.Extract(typeDeclaration, cancellationToken);
        var baseFlow = DiagnosticFlow.Zip(typeFlow, declarationsFlow);

        var options = AotReflectionOptions.From(context.Attributes);

        return baseFlow.Then(tuple => {
            var (symbol, declarations) = tuple;

            var propertiesFlow = options.IncludeProperties
                ? PropertyExtractor.ExtractProperties(symbol, options, cancellationToken)
                : DiagnosticFlow.Ok(default(EquatableArray<PropertyModel>));

            var methodsFlow = options.IncludeMethods
                ? MethodExtractor.ExtractMethods(symbol, options, cancellationToken)
                : DiagnosticFlow.Ok(default(EquatableArray<MethodModel>));

            var fieldsFlow = options.IncludeFields
                ? FieldExtractor.ExtractFields(symbol, options, cancellationToken)
                : DiagnosticFlow.Ok(default(EquatableArray<FieldModel>));

            var constructorsFlow = options.IncludeConstructors
                ? ConstructorExtractor.ExtractConstructors(symbol, options, cancellationToken)
                : DiagnosticFlow.Ok(default(EquatableArray<ConstructorModel>));

            var membersFlow = DiagnosticFlow.Zip(propertiesFlow, methodsFlow, fieldsFlow);
            var allMembersFlow = DiagnosticFlow.Zip(membersFlow, constructorsFlow);

            return allMembersFlow.Select(members => {
                var (memberTuple, constructors) = members;
                var (properties, methods, fields) = memberTuple;

                var namespaceName = symbol.ContainingNamespace.IsGlobalNamespace
                    ? string.Empty
                    : symbol.ContainingNamespace.ToDisplayString();

                var interfaces = symbol.AllInterfaces.Length is 0
                    ? default
                    : symbol.AllInterfaces.Select(@interface => @interface.GetFullyQualifiedName()).ToArray().ToEquatableArray();

                var baseType = symbol.BaseType is null || symbol.BaseType.SpecialType == SpecialType.System_Object
                    ? null
                    : symbol.BaseType.GetFullyQualifiedName();

                return new TypeModel(
                    FullyQualifiedName: symbol.GetFullyQualifiedName(),
                    Namespace: namespaceName,
                    Name: symbol.Name,
                    Accessibility: symbol.DeclaredAccessibility.ToAccessibilityString(),
                    IsStatic: symbol.IsStatic,
                    IsSealed: symbol.IsSealed,
                    IsAbstract: symbol.IsAbstract,
                    BaseTypeFullyQualified: baseType,
                    Interfaces: interfaces,
                    DeclarationChain: declarations,
                    Properties: properties,
                    Methods: methods,
                    Fields: fields,
                    Constructors: constructors,
                    Options: options);
            });
        });
    }
}
