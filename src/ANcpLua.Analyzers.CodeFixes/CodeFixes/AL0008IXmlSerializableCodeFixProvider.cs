using ANcpLua.Analyzers.Analyzers;

namespace ANcpLua.Analyzers.CodeFixes.CodeFixes;

/// <summary>
///     Code fix provider for AL0008 - makes GetSchema return null with expression body.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(Al0008IXmlSerializableCodeFixProvider))]
[Shared]
public sealed partial class Al0008IXmlSerializableCodeFixProvider : CodeFixProvider {
    public override ImmutableArray<string> FixableDiagnosticIds =>
        [Al0007ToAl0009IXmlSerializableAnalyzer.DiagnosticIdAl0008];

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context) {
        if (await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false) is not
            { } root) {
            return;
        }

        if (await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false)
            is not { } semanticModel) {
            return;
        }

        var ixmlSerializable = semanticModel.Compilation.GetTypeByMetadataName("System.Xml.Serialization.IXmlSerializable");
        var getSchemaMethod = ixmlSerializable?.GetMembers("GetSchema").OfType<IMethodSymbol>()
            .FirstOrDefault(m => m.Parameters.Length is 0);
        if (ixmlSerializable is null || getSchemaMethod is null) {
            return;
        }

        foreach (var diagnostic in context.Diagnostics) {
            if (diagnostic.Id != Al0007ToAl0009IXmlSerializableAnalyzer.DiagnosticIdAl0008) {
                continue;
            }

            var node = root.FindNode(diagnostic.Location.SourceSpan);
            if ((node as CSharpSyntaxNode
                 ?? node.FirstAncestorOrSelf<MethodDeclarationSyntax>() as CSharpSyntaxNode
                 ?? node.FirstAncestorOrSelf<BlockSyntax>() as CSharpSyntaxNode
                 ?? node.FirstAncestorOrSelf<ArrowExpressionClauseSyntax>()) is not { } target) {
                continue;
            }

            if (target.FirstAncestorOrSelf<MethodDeclarationSyntax>() is not { } methodDeclaration ||
                semanticModel.GetDeclaredSymbol(methodDeclaration, context.CancellationToken) is not { } methodSymbol ||
                !IsActualGetSchemaImplementation(methodSymbol, ixmlSerializable, getSchemaMethod)) {
                continue;
            }

            context.RegisterCodeFix(
                CodeAction.Create(
                    CodeFixResources.AL0008CodeFixTitle,
                    _ => FixAsync(context.Document, target, root),
                    nameof(CodeFixResources.AL0008CodeFixTitle)),
                diagnostic);
        }
    }

    private static Task<Document> FixAsync(Document document, CSharpSyntaxNode node, SyntaxNode root) {
        var newRoot = node switch {
            MethodDeclarationSyntax method when method.Modifiers.Any(SyntaxKind.AbstractKeyword)
                => RemoveAbstractAndAddNullBody(method, root),
            BlockSyntax block => ReplaceBlockWithNullArrow(block, root),
            ArrowExpressionClauseSyntax arrow => ReplaceArrowWithNull(arrow, root),
            _ => root
        };

        return Task.FromResult(document.WithSyntaxRoot(newRoot));
    }

    private static SyntaxNode RemoveAbstractAndAddNullBody(MethodDeclarationSyntax method, SyntaxNode root) {
        var abstractKeyword = method.Modifiers.First(static t => t.IsKind(SyntaxKind.AbstractKeyword));
        var newModifiers = method.Modifiers.Remove(abstractKeyword);

        var newMethod = method
            .WithModifiers(newModifiers)
            .WithSemicolonToken(default)
            .WithExpressionBody(CreateNullArrowExpression())
            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
            .WithLeadingTrivia(method.GetLeadingTrivia())
            .WithTrailingTrivia(method.GetTrailingTrivia());

        return root.ReplaceNode(method, newMethod);
    }

    private static SyntaxNode ReplaceBlockWithNullArrow(SyntaxNode block, SyntaxNode root) {
        if (block.Parent is not MethodDeclarationSyntax method) {
            return root;
        }

        var newMethod = method
            .WithBody(null)
            .WithExpressionBody(CreateNullArrowExpression())
            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
            .WithLeadingTrivia(method.GetLeadingTrivia())
            .WithTrailingTrivia(method.GetTrailingTrivia());

        return root.ReplaceNode(method, newMethod);
    }

    private static SyntaxNode ReplaceArrowWithNull(SyntaxNode arrow, SyntaxNode root) =>
        root.ReplaceNode(arrow, CreateNullArrowExpression());

    private static ArrowExpressionClauseSyntax CreateNullArrowExpression() =>
        SyntaxFactory.ArrowExpressionClause(
            SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression));

    private static bool IsActualGetSchemaImplementation(
        IMethodSymbol method,
        INamedTypeSymbol ixmlSerializable,
        IMethodSymbol interfaceGetSchema) {
        if (method.ExplicitInterfaceImplementations.Any(interfaceMethod =>
                interfaceMethod.IsEqualTo(interfaceGetSchema))) {
            return true;
        }

        if (method.ContainingType is not INamedTypeSymbol containingType ||
            !containingType.AllInterfaces.Contains(ixmlSerializable, SymbolEqualityComparer.Default)) {
            return false;
        }

        return method.Arity == interfaceGetSchema.Arity &&
               method.Parameters.Length == interfaceGetSchema.Parameters.Length &&
               method.Name == "GetSchema" &&
               method.ReturnType.IsEqualTo(interfaceGetSchema.ReturnType) &&
               containingType.FindImplementationForInterfaceMember(interfaceGetSchema) is { } implementation &&
               implementation.IsEqualTo(method);
    }
}
