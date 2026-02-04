using ANcpLua.Analyzers.Core;

namespace ANcpLua.Analyzers.CodeFixes.CodeFixes;

/// <summary>
///     Code fix provider for AL0073: Adds a default ActivitySourceName to [Traced] attribute.
/// </summary>
/// <remarks>
///     <para>
///         This code fix provides a default ActivitySourceName based on the containing type's
///         fully qualified name. For example, a class named <c>MyApp.Services.OrderService</c>
///         would get the ActivitySourceName <c>"MyApp.Services.OrderService"</c>.
///     </para>
/// </remarks>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(Al0073TracedCodeFixProvider))]
[Shared]
public sealed partial class Al0073TracedCodeFixProvider : CodeFixProvider {
    /// <summary>Gets the diagnostic IDs this provider can fix.</summary>
    public override ImmutableArray<string> FixableDiagnosticIds => [DiagnosticIds.TracedActivitySourceNameEmpty];

    /// <summary>Gets the FixAll provider for batch fixing.</summary>
    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    /// <summary>Registers code fixes for the given context.</summary>
    public override async Task RegisterCodeFixesAsync(CodeFixContext context) {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null) {
            return;
        }

        var diagnostic = context.Diagnostics[0];
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        // Find the attribute syntax
        var node = root.FindNode(diagnosticSpan);
        var attributeSyntax = node.AncestorsAndSelf().OfType<AttributeSyntax>().FirstOrDefault();

        if (attributeSyntax is null) {
            return;
        }

        // Get semantic model to determine the containing type's name
        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (semanticModel is null) {
            return;
        }

        // Determine the suggested source name based on context
        var suggestedName = GetSuggestedActivitySourceName(attributeSyntax, semanticModel, context.CancellationToken);

        context.RegisterCodeFix(
            CodeAction.Create(
                string.Format(CodeFixResources.AL0073CodeFixTitle, suggestedName),
                c => AddActivitySourceNameAsync(context.Document, attributeSyntax, suggestedName, root, c),
                nameof(Al0073TracedCodeFixProvider)),
            diagnostic);
    }

    private static string GetSuggestedActivitySourceName(
        SyntaxNode attribute,
        SemanticModel semanticModel,
        CancellationToken cancellationToken) {
        // Find the containing type
        var containingType = attribute.Ancestors()
            .OfType<TypeDeclarationSyntax>()
            .FirstOrDefault();

        if (containingType is null) {
            return "MyApp";
        }

        var typeSymbol = semanticModel.GetDeclaredSymbol(containingType, cancellationToken);
        if (typeSymbol is null) {
            return containingType.Identifier.Text;
        }

        // Use the fully qualified name without global:: prefix
        var fullName = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        return fullName.Replace("global::", "", StringComparison.Ordinal);
    }

    private static Task<Document> AddActivitySourceNameAsync(
        Document document,
        AttributeSyntax attribute,
        string sourceName,
        SyntaxNode root,
        CancellationToken _) {
        AttributeSyntax newAttribute;

        // Check if attribute has argument list
        if (attribute.ArgumentList is null || attribute.ArgumentList.Arguments.Count is 0) {
            // Create new argument list with the source name
            var argument = SyntaxFactory.AttributeArgument(
                SyntaxFactory.LiteralExpression(
                    SyntaxKind.StringLiteralExpression,
                    SyntaxFactory.Literal(sourceName)));

            var argumentList = SyntaxFactory.AttributeArgumentList(
                SyntaxFactory.SingletonSeparatedList(argument));

            newAttribute = attribute.WithArgumentList(argumentList);
        } else {
            // Check if first argument is empty string - replace it
            var firstArg = attribute.ArgumentList.Arguments[0];
            if (firstArg.Expression is LiteralExpressionSyntax { Token.ValueText: "" or " " or "  " }) {
                var newArg = SyntaxFactory.AttributeArgument(
                    SyntaxFactory.LiteralExpression(
                        SyntaxKind.StringLiteralExpression,
                        SyntaxFactory.Literal(sourceName)));

                var newArguments = attribute.ArgumentList.Arguments.Replace(firstArg, newArg);
                var newArgumentList = attribute.ArgumentList.WithArguments(newArguments);
                newAttribute = attribute.WithArgumentList(newArgumentList);
            } else {
                // Has non-empty arguments but missing ActivitySourceName - add as named argument
                var namedArg = SyntaxFactory.AttributeArgument(
                    SyntaxFactory.NameEquals("ActivitySourceName"),
                    null,
                    SyntaxFactory.LiteralExpression(
                        SyntaxKind.StringLiteralExpression,
                        SyntaxFactory.Literal(sourceName)));

                // Insert at the beginning
                var newArguments = attribute.ArgumentList.Arguments.Insert(0, namedArg);
                var newArgumentList = attribute.ArgumentList.WithArguments(newArguments);
                newAttribute = attribute.WithArgumentList(newArgumentList);
            }
        }

        var newRoot = root.ReplaceNode(attribute, newAttribute);
        return Task.FromResult(document.WithSyntaxRoot(newRoot));
    }
}
