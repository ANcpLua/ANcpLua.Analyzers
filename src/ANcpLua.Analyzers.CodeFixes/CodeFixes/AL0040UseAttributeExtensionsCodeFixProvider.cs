using ANcpLua.Analyzers.Core;

namespace ANcpLua.Analyzers.CodeFixes.CodeFixes;

/// <summary>
///     Code fix for AL0040: Converts attribute argument access patterns to extension methods.
/// </summary>
/// <remarks>
///     <list type="bullet">
///         <item><c>attr.ConstructorArguments[0].Value</c> → <c>attr.GetConstructorArgument&lt;object&gt;(0)</c></item>
///     </list>
/// </remarks>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(Al0040UseAttributeExtensionsCodeFixProvider))]
[Shared]
public sealed partial class Al0040UseAttributeExtensionsCodeFixProvider : CodeFixProvider {
    public override ImmutableArray<string> FixableDiagnosticIds => [DiagnosticIds.UseAttributeExtensions];

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context) {
        if (await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false) is not
            { } root) {
            return;
        }

        var diagnostic = context.Diagnostics.First();
        var node = root.FindNode(diagnostic.Location.SourceSpan);

        // Find the element access pattern: attr.ConstructorArguments[i] or attr.ConstructorArguments[i].Value
        if (TryExtractConstructorArgumentsPattern(node, out var attrExpr, out var indexExpr)) {
            context.RegisterCodeFix(
                CodeAction.Create(
                    CodeFixResources.AL0040CodeFixTitle,
                    _ => ConvertToGetConstructorArgument(context.Document, root, node, attrExpr, indexExpr),
                    nameof(Al0040UseAttributeExtensionsCodeFixProvider)),
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
            // Pattern 1: attr.ConstructorArguments[i].Value
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
            // Pattern 2: attr.ConstructorArguments[i] (without .Value)
            case ElementAccessExpressionSyntax {
                Expression: MemberAccessExpressionSyntax {
                    Name.Identifier.Text: "ConstructorArguments"
                } constructorArgs2
            } elementAccess2:
                attrExpr = constructorArgs2.Expression;
                indexExpr = elementAccess2.ArgumentList.Arguments.FirstOrDefault()?.Expression;
                return indexExpr is not null;
            default:
                return false;
        }
    }

    private static Task<Document> ConvertToGetConstructorArgument(
        Document document,
        SyntaxNode root,
        SyntaxNode nodeToReplace,
        ExpressionSyntax attrExpr,
        ExpressionSyntax indexExpr) {
        // Create: attr.GetConstructorArgument<object>(index)
        // Note: We use <object> as we can't infer the type without semantic analysis
        var newExpression = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    attrExpr.WithoutTrivia(),
                    SyntaxFactory.GenericName(
                            SyntaxFactory.Identifier("GetConstructorArgument"))
                        .WithTypeArgumentList(
                            SyntaxFactory.TypeArgumentList(
                                SyntaxFactory.SingletonSeparatedList<TypeSyntax>(
                                    SyntaxFactory.PredefinedType(
                                        SyntaxFactory.Token(SyntaxKind.ObjectKeyword)))))),
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.Argument(indexExpr.WithoutTrivia()))))
            .WithTriviaFrom(nodeToReplace);

        var newRoot = root.ReplaceNode(nodeToReplace, newExpression);
        return Task.FromResult(document.WithSyntaxRoot(newRoot));
    }
}
