using ANcpLua.Analyzers.Analyzers;

namespace ANcpLua.Analyzers.CodeFixes.CodeFixes;

/// <summary>
///     Code fix for AL1209: Converts TryParse ternary patterns to extension methods.
/// </summary>
/// <remarks>
///     <list type="bullet">
///         <item><c>int.TryParse(s, out var v) ? v : null</c> → <c>s.TryParseInt32()</c></item>
///         <item><c>Guid.TryParse(s, out var v) ? v : default</c> → <c>s.TryParseGuid()</c></item>
///     </list>
/// </remarks>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(Al1209UseTryParseExtensionsCodeFixProvider))]
[Shared]
public sealed partial class Al1209UseTryParseExtensionsCodeFixProvider
    : AlCodeFixProvider<ConditionalExpressionSyntax> {
    /// <summary>Gets the diagnostic IDs this code fix can fix.</summary>
    public override ImmutableArray<string> FixableDiagnosticIds => [Al1209UseTryParseExtensionsAnalyzer.DiagnosticId];

    /// <summary>Creates the code action for this fix.</summary>
    protected override CodeAction CreateCodeAction(
        Document document,
        ConditionalExpressionSyntax conditional,
        SyntaxNode root,
        Diagnostic diagnostic) =>
        CodeAction.Create(
            CodeFixResources.AL1209CodeFixTitle,
            _ => ConvertToExtension(document, conditional, root),
            Al1209UseTryParseExtensionsAnalyzer.DiagnosticId);

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
        var (stringArg, extensionName) = ExtractInfo(conditional, tryParseInvocation);
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
        ConditionalExpressionSyntax conditional,
        InvocationExpressionSyntax invocation) {
        // Pattern: Type.TryParse(stringArg, out var result)
        if (invocation.Expression is not MemberAccessExpressionSyntax {
            Name.Identifier.Text: "TryParse"
        } memberAccess) {
            return (null, null);
        }

        if (memberAccess.Expression is not { } receiverSyntax) {
            return (null, null);
        }

        if (GetTypeName(receiverSyntax) is not { } receiverTypeName ||
            GetTryParseExtension(receiverTypeName) is not { } extensionName) {
            return (null, null);
        }

        // Get the first argument (the string to parse)
        if (invocation.ArgumentList.Arguments.Count < 1) {
            return (null, null);
        }

        if (invocation.ArgumentList.Arguments.Count != 2) {
            return (null, null);
        }

        if (invocation.ArgumentList.Arguments[0].Expression is null or AssignmentExpressionSyntax) {
            return (null, null);
        }

        if (invocation.ArgumentList.Arguments[1].RefKindKeyword is not { RawKind: (int)SyntaxKind.OutKeyword }) {
            return (null, null);
        }

        if (!TryGetOutVariableName(invocation.ArgumentList.Arguments[1].Expression, out var outVarName)) {
            return (null, null);
        }

        var whenTrue = conditional.WhenTrue;
        while (whenTrue is ParenthesizedExpressionSyntax trueParen) {
            whenTrue = trueParen.Expression;
        }

        if (whenTrue is not IdentifierNameSyntax whenTrueVar ||
            whenTrueVar.Identifier.Text != outVarName) {
            return (null, null);
        }

        var stringArg = invocation.ArgumentList.Arguments[0].Expression;
        return (stringArg, extensionName);
    }

    private static string? GetTypeName(ExpressionSyntax expression) =>
        expression switch {
            IdentifierNameSyntax id => id.Identifier.Text,
            PredefinedTypeSyntax predefined => predefined.Keyword.Text,
            QualifiedNameSyntax qualified => qualified.ToString(),
            AliasQualifiedNameSyntax aliasQualified => aliasQualified.Name.ToString(),
            MemberAccessExpressionSyntax memberAccess => memberAccess.ToString(),
            _ => null
        };

    private static string? GetTryParseExtension(string typeName) =>
        typeName switch {
            "System.Int32" or "int" => "TryParseInt32",
            "System.Int64" or "long" => "TryParseInt64",
            "System.Double" or "double" => "TryParseDouble",
            "System.Decimal" or "decimal" => "TryParseDecimal",
            "System.Boolean" or "bool" => "TryParseBool",
            "System.Guid" or "Guid" => "TryParseGuid",
            "System.DateTime" or "DateTime" => "TryParseDateTime",
            "System.DateTimeOffset" or "DateTimeOffset" => "TryParseDateTimeOffset",
            "System.TimeSpan" or "TimeSpan" => "TryParseTimeSpan",
            "System.Byte" or "byte" => "TryParseByte",
            "System.Int16" or "short" => "TryParseInt16",
            "System.Single" or "float" => "TryParseSingle",
            _ => null
        };

    private static bool TryGetOutVariableName(ExpressionSyntax expression, out string variableName) {
        switch (expression) {
            case DeclarationExpressionSyntax { Designation: SingleVariableDesignationSyntax { Identifier.Text: var variable } }:
                variableName = variable;
                return true;
            case IdentifierNameSyntax { Identifier.Text: var variable }:
                variableName = variable;
                return true;
            default:
                variableName = "";
                return false;
        }
    }
}
