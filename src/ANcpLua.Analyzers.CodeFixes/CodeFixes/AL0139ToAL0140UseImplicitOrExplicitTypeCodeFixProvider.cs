using ANcpLua.Analyzers.Analyzers;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Simplification;

namespace ANcpLua.Analyzers.CodeFixes.CodeFixes;

/// <summary>
///     Code fix for AL0139/AL0140: converts between explicit local types and <c>var</c>.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(Al0139ToAl0140UseImplicitOrExplicitTypeCodeFixProvider))]
[Shared]
public sealed partial class Al0139ToAl0140UseImplicitOrExplicitTypeCodeFixProvider
    : AlCodeFixProvider<TypeSyntax> {
    private const string UseImplicitTypeTitle = "Use implicit type";
    private const string UseExplicitTypeTitle = "Use explicit type";

    /// <summary>Gets the diagnostic IDs this provider can fix.</summary>
    public override ImmutableArray<string> FixableDiagnosticIds =>
        [
            Al0139ToAl0140UseImplicitOrExplicitTypeAnalyzer.DiagnosticIdAl0139,
            Al0139ToAl0140UseImplicitOrExplicitTypeAnalyzer.DiagnosticIdAl0140
        ];

    /// <inheritdoc />
    protected override CodeAction? CreateCodeAction(
        Document document,
        TypeSyntax syntax,
        SyntaxNode root,
        Diagnostic diagnostic) =>
        diagnostic.Id switch {
            Al0139ToAl0140UseImplicitOrExplicitTypeAnalyzer.DiagnosticIdAl0139 =>
                CodeAction.Create(
                    UseImplicitTypeTitle,
                    _ => UseImplicitType(document, root, syntax),
                    UseImplicitTypeTitle),
            Al0139ToAl0140UseImplicitOrExplicitTypeAnalyzer.DiagnosticIdAl0140 =>
                CodeAction.Create(
                    UseExplicitTypeTitle,
                    ct => UseExplicitTypeAsync(document, root, syntax, ct),
                    UseExplicitTypeTitle),
            _ => null
        };

    private static Task<Document> UseImplicitType(
        Document document,
        SyntaxNode root,
        TypeSyntax typeSyntax) {
        var implicitType = SyntaxFactory.IdentifierName(SyntaxFacts.GetText(SyntaxKind.VarKeyword))
            .WithTriviaFrom(typeSyntax);

        return Task.FromResult(document.WithSyntaxRoot(root.ReplaceNode(typeSyntax, implicitType)));
    }

    private static async Task<Document> UseExplicitTypeAsync(
        Document document,
        SyntaxNode root,
        TypeSyntax typeSyntax,
        CancellationToken cancellationToken) {
        if (await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false) is not { } semanticModel) {
            return document;
        }

        if (GetExplicitType(semanticModel, typeSyntax, cancellationToken) is not { } type) {
            return document;
        }

        var generator = SyntaxGenerator.GetGenerator(document);
        var explicitType = ((TypeSyntax)generator.TypeExpression(type))
            .WithTriviaFrom(typeSyntax)
            .WithAdditionalAnnotations(Simplifier.AddImportsAnnotation);

        return document.WithSyntaxRoot(root.ReplaceNode(typeSyntax, explicitType));
    }

    private static ITypeSymbol? GetExplicitType(
        SemanticModel semanticModel,
        TypeSyntax typeSyntax,
        CancellationToken cancellationToken) =>
        typeSyntax.Parent switch {
            VariableDeclarationSyntax { Variables.Count: 1 } declaration =>
                GetInitializerType(semanticModel, declaration, cancellationToken),
            ForEachStatementSyntax foreachStatement =>
                semanticModel.GetForEachStatementInfo(foreachStatement).ElementType,
            DeclarationExpressionSyntax declarationExpression =>
                GetDeclarationExpressionType(semanticModel, declarationExpression, typeSyntax, cancellationToken),
            _ => GetTypeInfoType(semanticModel, typeSyntax, cancellationToken)
        };

    private static ITypeSymbol? GetInitializerType(
        SemanticModel semanticModel,
        VariableDeclarationSyntax declaration,
        CancellationToken cancellationToken) {
        if (declaration.Variables[0].Initializer?.Value is not { } initializer) {
            return null;
        }

        var typeInfo = semanticModel.GetTypeInfo(initializer, cancellationToken);
        return typeInfo.ConvertedType ?? typeInfo.Type;
    }

    private static ITypeSymbol? GetDeclarationExpressionType(
        SemanticModel semanticModel,
        DeclarationExpressionSyntax declaration,
        TypeSyntax typeSyntax,
        CancellationToken cancellationToken) {
        if (declaration.Designation is SingleVariableDesignationSyntax designation &&
            semanticModel.GetDeclaredSymbol(designation, cancellationToken) is ILocalSymbol local) {
            return local.Type;
        }

        var declarationType = semanticModel.GetTypeInfo(declaration, cancellationToken);
        return declarationType.Type ?? declarationType.ConvertedType ??
               GetTypeInfoType(semanticModel, typeSyntax, cancellationToken);
    }

    private static ITypeSymbol? GetTypeInfoType(
        SemanticModel semanticModel,
        TypeSyntax typeSyntax,
        CancellationToken cancellationToken) {
        var typeInfo = semanticModel.GetTypeInfo(typeSyntax, cancellationToken);
        return typeInfo.Type ?? typeInfo.ConvertedType;
    }
}
