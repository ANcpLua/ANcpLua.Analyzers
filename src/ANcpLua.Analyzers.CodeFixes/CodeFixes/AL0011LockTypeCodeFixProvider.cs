using ANcpLua.Analyzers.Core;
using Microsoft.CodeAnalysis.Editing;

namespace ANcpLua.Analyzers.CodeFixes.CodeFixes;

/// <summary>
///     Code fix for AL0011: Changes field type from System.Object to System.Threading.Lock.
///     Only offered when Lock type is available (.NET 9+).
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(Al0011LockTypeCodeFixProvider))]
[Shared]
public sealed partial class Al0011LockTypeCodeFixProvider : CodeFixProvider {
    private const string LockTypeMetadataName = "System.Threading.Lock";

    public override ImmutableArray<string> FixableDiagnosticIds => [DiagnosticIds.AvoidLockKeywordOnNonLockTypes];

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context) {
        if (await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false) is not { } root) {
            return;
        }

        if (await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false) is not { } semanticModel) {
            return;
        }

        // Only offer fix if Lock type is available
        if (semanticModel.Compilation.GetTypeByMetadataName(LockTypeMetadataName) is null) {
            return;
        }

        foreach (var diagnostic in context.Diagnostics) {
            if (root.FindNode(diagnostic.Location.SourceSpan).FirstAncestorOrSelf<LockStatementSyntax>() is not { } lockStatement) {
                continue;
            }

            // Find the field being used for synchronization
            if (FindSyncField(lockStatement.Expression, semanticModel, context.CancellationToken) is not { } fieldInfo) {
                continue;
            }

            context.RegisterCodeFix(
                CodeAction.Create(
                    CodeFixResources.AL0011CodeFixTitle,
                    ct => ChangeFieldTypeToLockAsync(context.Document, fieldInfo.declaration, ct),
                    nameof(CodeFixResources.AL0011CodeFixTitle)),
                diagnostic);
        }
    }

    private static (VariableDeclaratorSyntax declaration, IFieldSymbol field)? FindSyncField(
        ExpressionSyntax syncExpression,
        SemanticModel semanticModel,
        CancellationToken cancellationToken) {
        if (semanticModel.GetSymbolInfo(syncExpression, cancellationToken).Symbol is not IFieldSymbol field) {
            return null;
        }

        // Only fix private fields of type System.Object
        if (field.DeclaredAccessibility != Accessibility.Private ||
            field.Type.SpecialType != SpecialType.System_Object) {
            return null;
        }

        // Find the declaration syntax
        if (field.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax(cancellationToken)
            is not VariableDeclaratorSyntax declarator) {
            return null;
        }

        return (declarator, field);
    }

    private static async Task<Document> ChangeFieldTypeToLockAsync(
        Document document,
        VariableDeclaratorSyntax declarator,
        CancellationToken cancellationToken) {
        if (await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false) is not { }) {
            return document;
        }

        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

        // Get the variable declaration
        if (declarator.Parent is not VariableDeclarationSyntax variableDeclaration) {
            return document;
        }

        // Create Lock type syntax (fully qualified)
        var lockTypeSyntax = SyntaxFactory.QualifiedName(
                SyntaxFactory.QualifiedName(
                    SyntaxFactory.IdentifierName("System"),
                    SyntaxFactory.IdentifierName("Threading")),
                SyntaxFactory.IdentifierName("Lock"))
            .WithTrailingTrivia(SyntaxFactory.Space);

        // Create new variable declaration with Lock type
        var newVariableDeclaration = variableDeclaration.WithType(lockTypeSyntax);

        // Update initializer to use new() if present
        if (declarator.Initializer?.Value is ObjectCreationExpressionSyntax or ImplicitObjectCreationExpressionSyntax) {
            var newInitializer = SyntaxFactory.ImplicitObjectCreationExpression()
                .WithArgumentList(SyntaxFactory.ArgumentList())
                .WithLeadingTrivia(declarator.Initializer.Value.GetLeadingTrivia())
                .WithTrailingTrivia(declarator.Initializer.Value.GetTrailingTrivia());

            var newDeclarator = declarator.WithInitializer(
                declarator.Initializer.WithValue(newInitializer));

            newVariableDeclaration = newVariableDeclaration.ReplaceNode(
                newVariableDeclaration.Variables.First(v => v.Identifier.Text == declarator.Identifier.Text),
                newDeclarator);
        }

        editor.ReplaceNode(variableDeclaration, newVariableDeclaration);

        return editor.GetChangedDocument();
    }
}
