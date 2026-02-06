namespace ANcpLua.Analyzers.AotReflection;

internal static partial class DeclarationChainExtractor {
    public static DiagnosticFlow<EquatableArray<TypeDeclarationModel>> Extract(
        TypeDeclarationSyntax declaration,
        CancellationToken cancellationToken) {
        var diagnostics = new List<DiagnosticInfo>();
        var chain = new List<TypeDeclarationModel>();

        for (var current = declaration; current is not null; current = current.Parent as TypeDeclarationSyntax) {
            cancellationToken.ThrowIfCancellationRequested();

            if (!current.Modifiers.Any(SyntaxKind.PartialKeyword)) {
                diagnostics.Add(DiagnosticInfo.Create(
                    DiagnosticDescriptors.TypeMustBePartial,
                    current.Identifier,
                    current.Identifier.ValueText));
            }

            chain.Add(BuildModel(current));
        }

        chain.Reverse();

        if (diagnostics.Count > 0) {
            return DiagnosticFlow.Fail<EquatableArray<TypeDeclarationModel>>(diagnostics.ToArray());
        }

        return DiagnosticFlow.Ok(chain.Count is 0 ? default : chain.ToArray().ToEquatableArray());
    }

    private static TypeDeclarationModel BuildModel(TypeDeclarationSyntax declaration) {
        var modifiers = declaration.Modifiers.Select(static modifier => modifier.ValueText).ToList();
        if (!modifiers.Any(static modifier => string.Equals(modifier, "partial", StringComparison.Ordinal))) {
            modifiers.Add("partial");
        }

        var keyword = declaration.Keyword.ValueText;
        if (declaration is RecordDeclarationSyntax record) {
            if (record.ClassOrStructKeyword.IsKind(SyntaxKind.StructKeyword)) {
                keyword = "record struct";
            } else if (record.ClassOrStructKeyword.IsKind(SyntaxKind.ClassKeyword)) {
                keyword = "record class";
            } else {
                keyword = "record";
            }
        }

        var typeParameters = declaration.TypeParameterList?.ToString().Trim() ?? string.Empty;
        var constraints = declaration.ConstraintClauses
            .Select(static clause => clause.ToString().Trim())
            .Where(static clause => clause.Length > 0)
            .ToArray();

        return new TypeDeclarationModel(
            Name: declaration.Identifier.ValueText,
            Keyword: keyword,
            Modifiers: string.Join(" ", modifiers),
            TypeParameters: typeParameters,
            ConstraintClauses: constraints.Length is 0 ? default : constraints.ToEquatableArray());
    }
}
