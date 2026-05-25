using ANcpLua.Analyzers.Analyzers;

namespace ANcpLua.Analyzers.CodeFixes.CodeFixes;

/// <summary>
///     Code fix for AL1211: Converts attribute argument access patterns to extension methods.
/// </summary>
/// <remarks>
///     <list type="bullet">
///         <item><c>(T)attr.ConstructorArguments[0].Value</c> to <c>(T)attr.GetConstructorArgument&lt;T&gt;(0)</c></item>
///     </list>
/// </remarks>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(Al1211UseAttributeExtensionsCodeFixProvider))]
[Shared]
public sealed partial class Al1211UseAttributeExtensionsCodeFixProvider : CodeFixProvider {
    public override ImmutableArray<string> FixableDiagnosticIds => [Al1211UseAttributeExtensionsAnalyzer.DiagnosticId];

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context) {
        if (await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false) is not
            { } root) {
            return;
        }

        var diagnostic = context.Diagnostics.First();
        var node = root.FindNode(diagnostic.Location.SourceSpan);

        // Find the element access pattern: attr.ConstructorArguments[i] or attr.ConstructorArguments[i].Value
        if (TryExtractConstructorArgumentsPattern(node, out var attrExpr, out var indexExpr) &&
            TryInferTypeArgument(node, out var typeArgument)) {
            context.RegisterCodeFix(
                CodeAction.Create(
                    CodeFixResources.AL1211CodeFixTitle,
                    _ => ConvertToGetConstructorArgument(context.Document, root, node, attrExpr, indexExpr, typeArgument),
                    Al1211UseAttributeExtensionsAnalyzer.DiagnosticId),
                diagnostic);
        }
    }

    private static bool TryExtractConstructorArgumentsPattern(
        SyntaxNode node,
        [NotNullWhen(true)] out ExpressionSyntax? attrExpr,
        [NotNullWhen(true)] out ExpressionSyntax? indexExpr) {
        attrExpr = null;
        indexExpr = null;

        switch (node) {
            // Pattern: attr.ConstructorArguments[i].Value
            case MemberAccessExpressionSyntax {
                Name.Identifier.Text: "Value", Expression: ElementAccessExpressionSyntax {
                    Expression: MemberAccessExpressionSyntax {
                        Name.Identifier.Text: "ConstructorArguments"
                    } constructorArgs1
                } elementAccess1
            }:
                attrExpr = constructorArgs1.Expression;
                indexExpr = elementAccess1.ArgumentList.Arguments.FirstOrDefault()?.Expression;
                return indexExpr is not null;
            default:
                return false;
        }
    }

    private static bool TryInferTypeArgument(
        SyntaxNode node,
        [NotNullWhen(true)] out TypeSyntax? typeArgument) {
        typeArgument = null;

        if (node is not ExpressionSyntax expression) {
            return false;
        }

        var current = expression;
        while (current.Parent is ParenthesizedExpressionSyntax parenthesized &&
               ReferenceEquals(parenthesized.Expression, current)) {
            current = parenthesized;
        }

        switch (current.Parent) {
            case CastExpressionSyntax cast when ReferenceEquals(cast.Expression, current):
                typeArgument = cast.Type.WithoutTrivia();
                return !IsObjectType(typeArgument);
            case BinaryExpressionSyntax asExpression
                when asExpression.IsKind(SyntaxKind.AsExpression) &&
                     ReferenceEquals(asExpression.Left, current):
                typeArgument = SyntaxFactory.ParseTypeName(asExpression.Right.ToString());
                return !IsObjectType(typeArgument);
            default:
                return false;
        }
    }

    private static bool IsObjectType(TypeSyntax type) {
        var candidate = type is NullableTypeSyntax nullable
            ? nullable.ElementType
            : type;
        var normalized = candidate.ToString().Replace(" ", string.Empty);

        return candidate is PredefinedTypeSyntax { Keyword.RawKind: (int)SyntaxKind.ObjectKeyword } ||
               normalized is "Object" or "System.Object" or "global::System.Object";
    }

    private static Task<Document> ConvertToGetConstructorArgument(
        Document document,
        SyntaxNode root,
        SyntaxNode nodeToReplace,
        ExpressionSyntax attrExpr,
        ExpressionSyntax indexExpr,
        TypeSyntax typeArgument) {
        // Create: attr.GetConstructorArgument<T>(index)
        var newExpression = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    attrExpr.WithoutTrivia(),
                    SyntaxFactory.GenericName(
                            SyntaxFactory.Identifier("GetConstructorArgument"))
                        .WithTypeArgumentList(
                            SyntaxFactory.TypeArgumentList(
                                SyntaxFactory.SingletonSeparatedList<TypeSyntax>(
                                    typeArgument.WithoutTrivia())))),
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.Argument(indexExpr.WithoutTrivia()))))
            .WithTriviaFrom(nodeToReplace);

        var newRoot = root.ReplaceNode(nodeToReplace, newExpression);
        return Task.FromResult(document.WithSyntaxRoot(newRoot));
    }
}
