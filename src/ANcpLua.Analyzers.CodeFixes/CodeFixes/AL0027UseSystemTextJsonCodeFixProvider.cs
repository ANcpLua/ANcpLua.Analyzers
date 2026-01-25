using ANcpLua.Analyzers.Core;

namespace ANcpLua.Analyzers.CodeFixes.CodeFixes;

/// <summary>
///     Code fix for AL0027: Converts common Newtonsoft.Json patterns to System.Text.Json.
/// </summary>
/// <remarks>
///     <c>JsonConvert.SerializeObject(obj)</c> → <c>JsonSerializer.Serialize(obj)</c>
///     <c>JsonConvert.DeserializeObject&lt;T&gt;(json)</c> → <c>JsonSerializer.Deserialize&lt;T&gt;(json)</c>
/// </remarks>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(Al0027UseSystemTextJsonCodeFixProvider))]
[Shared]
public sealed partial class Al0027UseSystemTextJsonCodeFixProvider : CodeFixProvider {
    public override ImmutableArray<string> FixableDiagnosticIds => [DiagnosticIds.AvoidNewtonsoftJson];

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context) {
        if (await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false) is not { } root) {
            return;
        }

        var diagnostic = context.Diagnostics.First();
        var diagnosticSpan = diagnostic.Location.SourceSpan;
        var node = root.FindNode(diagnosticSpan);

        // Handle JsonConvert.SerializeObject/DeserializeObject
        if (node.FirstAncestorOrSelf<InvocationExpressionSyntax>() is { } invocation &&
            TryGetJsonConvertReplacement(invocation, out var methodName, out var typeArgs)) {
            context.RegisterCodeFix(
                CodeAction.Create(
                    CodeFixResources.AL0027CodeFixTitle,
                    _ => ConvertToSystemTextJson(context.Document, root, invocation, methodName, typeArgs),
                    nameof(Al0027UseSystemTextJsonCodeFixProvider)),
                diagnostic);
        }
    }

    private static bool TryGetJsonConvertReplacement(
        InvocationExpressionSyntax invocation,
        [NotNullWhen(true)] out string? replacementMethod,
        out TypeArgumentListSyntax? typeArgs) {
        replacementMethod = null;
        typeArgs = null;

        // Pattern: JsonConvert.SerializeObject(...) or JsonConvert.DeserializeObject<T>(...)
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess) {
            return false;
        }

        // Check for JsonConvert class
        if (memberAccess.Expression is not IdentifierNameSyntax { Identifier.Text: "JsonConvert" }) {
            return false;
        }

        var methodName = memberAccess.Name switch {
            GenericNameSyntax generic => generic.Identifier.Text,
            not null => memberAccess.Name.Identifier.Text,
            _ => null
        };

        replacementMethod = methodName switch {
            "SerializeObject" => "Serialize",
            "DeserializeObject" => "Deserialize",
            _ => null
        };

        if (replacementMethod is null) {
            return false;
        }

        // Get type arguments if present
        if (memberAccess.Name is GenericNameSyntax genericName) {
            typeArgs = genericName.TypeArgumentList;
        }

        return true;
    }

    private static Task<Document> ConvertToSystemTextJson(
        Document document,
        SyntaxNode root,
        InvocationExpressionSyntax invocation,
        string methodName,
        TypeArgumentListSyntax? typeArgs) {
        // Create: System.Text.Json.JsonSerializer.Serialize(...) or Deserialize<T>(...)
        // Use fully qualified name to avoid conflicts with Newtonsoft.Json.JsonSerializer
        SimpleNameSyntax methodNameSyntax = typeArgs is not null
            ? SyntaxFactory.GenericName(
                SyntaxFactory.Identifier(methodName),
                typeArgs)
            : SyntaxFactory.IdentifierName(methodName);

        // Build: System.Text.Json.JsonSerializer
        var fullyQualifiedJsonSerializer = SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName("System"),
                    SyntaxFactory.IdentifierName("Text")),
                SyntaxFactory.IdentifierName("Json")),
            SyntaxFactory.IdentifierName("JsonSerializer"));

        var newExpression = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    fullyQualifiedJsonSerializer,
                    methodNameSyntax),
                invocation.ArgumentList)
            .WithTriviaFrom(invocation);

        var newRoot = root.ReplaceNode(invocation, newExpression);
        return Task.FromResult(document.WithSyntaxRoot(newRoot));
    }
}
