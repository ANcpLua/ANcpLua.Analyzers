using ANcpLua.Analyzers.Core;

namespace ANcpLua.Analyzers.CodeFixes.CodeFixes;

/// <summary>
///     Code fix for AL0037: Converts TryParse ternary patterns to extension methods.
/// </summary>
/// <remarks>
///     <list type="bullet">
///         <item><c>int.TryParse(s, out var v) ? v : null</c> → <c>s.TryParseInt32()</c></item>
///         <item><c>Guid.TryParse(s, out var v) ? v : default</c> → <c>s.TryParseGuid()</c></item>
///     </list>
/// </remarks>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(Al0037UseTryParseExtensionsCodeFixProvider))]
[Shared]
public sealed partial class Al0037UseTryParseExtensionsCodeFixProvider
    : AlCodeFixProvider<ConditionalExpressionSyntax> {
    // Mapping from type name to extension method name
    private static readonly Dictionary<string, string> TypeToExtension = new(StringComparer.Ordinal) {
        ["int"] = "TryParseInt32",
        ["Int32"] = "TryParseInt32",
        ["long"] = "TryParseInt64",
        ["Int64"] = "TryParseInt64",
        ["double"] = "TryParseDouble",
        ["Double"] = "TryParseDouble",
        ["decimal"] = "TryParseDecimal",
        ["Decimal"] = "TryParseDecimal",
        ["bool"] = "TryParseBool",
        ["Boolean"] = "TryParseBool",
        ["Guid"] = "TryParseGuid",
        ["DateTime"] = "TryParseDateTime",
        ["DateTimeOffset"] = "TryParseDateTimeOffset",
        ["TimeSpan"] = "TryParseTimeSpan",
        ["byte"] = "TryParseByte",
        ["Byte"] = "TryParseByte",
        ["short"] = "TryParseInt16",
        ["Int16"] = "TryParseInt16",
        ["float"] = "TryParseSingle",
        ["Single"] = "TryParseSingle"
    };

    /// <summary>Gets the diagnostic IDs this code fix can fix.</summary>
    public override ImmutableArray<string> FixableDiagnosticIds => [DiagnosticIds.UseTryParseExtensions];

    /// <summary>Creates the code action for this fix.</summary>
    protected override CodeAction CreateCodeAction(
        Document document,
        ConditionalExpressionSyntax conditional,
        SyntaxNode root,
        Diagnostic diagnostic) =>
        CodeAction.Create(
            CodeFixResources.AL0037CodeFixTitle,
            _ => ConvertToExtension(document, conditional, root),
            nameof(Al0037UseTryParseExtensionsCodeFixProvider));

    private static Task<Document> ConvertToExtension(
        Document document,
        ConditionalExpressionSyntax conditional,
        SyntaxNode root) {
        // Extract the TryParse invocation from condition
        var condition = conditional.Condition;
        while (condition is ParenthesizedExpressionSyntax paren) {
            condition = paren.Expression;
        }

        if (condition is not InvocationExpressionSyntax tryParseInvocation) {
            return Task.FromResult(document);
        }

        // Get the type and extension method name
        var (stringArg, extensionName) = ExtractInfo(tryParseInvocation);
        if (stringArg is null || extensionName is null) {
            return Task.FromResult(document);
        }

        // Create: stringArg.ExtensionMethod()
        var newExpression = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    stringArg.WithoutTrivia(),
                    SyntaxFactory.IdentifierName(extensionName)))
            .WithTriviaFrom(conditional);

        var newRoot = root.ReplaceNode(conditional, newExpression);
        return Task.FromResult(document.WithSyntaxRoot(newRoot));
    }

    private static (ExpressionSyntax? stringArg, string? extensionName) ExtractInfo(
        InvocationExpressionSyntax invocation) {
        // Pattern: Type.TryParse(stringArg, out var result)
        if (invocation.Expression is not MemberAccessExpressionSyntax {
            Name.Identifier.Text: "TryParse"
        } memberAccess) {
            return (null, null);
        }

        // Get the type name
        var typeName = memberAccess.Expression switch {
            IdentifierNameSyntax id => id.Identifier.Text,
            PredefinedTypeSyntax predefined => predefined.Keyword.Text,
            _ => null
        };

        if (typeName is null || !TypeToExtension.TryGetValue(typeName, out var extensionName)) {
            return (null, null);
        }

        // Get the first argument (the string to parse)
        if (invocation.ArgumentList.Arguments.Count < 1) {
            return (null, null);
        }

        var stringArg = invocation.ArgumentList.Arguments[0].Expression;
        return (stringArg, extensionName);
    }
}
