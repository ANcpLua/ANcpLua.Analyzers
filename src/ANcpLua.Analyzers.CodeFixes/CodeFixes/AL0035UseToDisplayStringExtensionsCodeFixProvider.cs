using ANcpLua.Analyzers.Analyzers;

namespace ANcpLua.Analyzers.CodeFixes.CodeFixes;

/// <summary>
///     Code fix for AL0035: Converts ToDisplayString with format to extension methods.
/// </summary>
/// <remarks>
///     <list type="bullet">
///         <item>
///             <c>type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)</c> →
///             <c>type.GetFullyQualifiedName()</c>
///         </item>
///         <item>
///             <c>type.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)</c> →
///             <c>type.GetMetadataName()</c>
///         </item>
///     </list>
/// </remarks>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(Al0035UseToDisplayStringExtensionsCodeFixProvider))]
[Shared]
public sealed partial class Al0035UseToDisplayStringExtensionsCodeFixProvider
    : AlCodeFixProvider<InvocationExpressionSyntax> {
    // Mapping from format name to extension method name
    private static readonly Dictionary<string, string> s_formatToExtension = new(StringComparer.Ordinal) {
        ["FullyQualifiedFormat"] = "GetFullyQualifiedName",
        ["CSharpErrorMessageFormat"] = "GetMetadataName"
    };

    /// <summary>Gets the diagnostic IDs this code fix can fix.</summary>
    public override ImmutableArray<string> FixableDiagnosticIds => [Al0035UseToDisplayStringExtensionsAnalyzer.DiagnosticId];

    /// <summary>Creates the code action for this fix.</summary>
    protected override CodeAction CreateCodeAction(
        Document document,
        InvocationExpressionSyntax invocation,
        SyntaxNode root,
        Diagnostic diagnostic) =>
        CodeAction.Create(
            CodeFixResources.AL0035CodeFixTitle,
            _ => ConvertToExtension(document, invocation, root),
            nameof(Al0035UseToDisplayStringExtensionsCodeFixProvider));

    private static Task<Document> ConvertToExtension(
        Document document,
        InvocationExpressionSyntax invocation,
        SyntaxNode root) {
        // Get the receiver from member access: type.ToDisplayString(format)
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess) {
            return Task.FromResult(document);
        }

        var receiver = memberAccess.Expression;

        // Find the format argument and determine the extension method
        if (GetExtensionMethodName(invocation) is not { } extensionName) {
            return Task.FromResult(document);
        }

        // Create: receiver.ExtensionMethod()
        var newExpression = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    receiver.WithoutTrivia(),
                    SyntaxFactory.IdentifierName(extensionName)))
            .WithTriviaFrom(invocation);

        var newRoot = root.ReplaceNode(invocation, newExpression);
        return Task.FromResult(document.WithSyntaxRoot(newRoot));
    }

    private static string? GetExtensionMethodName(InvocationExpressionSyntax invocation) {
        foreach (var arg in invocation.ArgumentList.Arguments) {
            // Look for SymbolDisplayFormat.XYZ pattern
            if (arg.Expression is MemberAccessExpressionSyntax { Name: IdentifierNameSyntax formatName } formatAccess) {
                var expressionText = formatAccess.Expression.ToString();
                if (expressionText is "SymbolDisplayFormat" or "Microsoft.CodeAnalysis.SymbolDisplayFormat") {
                    if (s_formatToExtension.TryGetValue(formatName.Identifier.Text, out var extension)) {
                        return extension;
                    }
                }
            }
        }

        return null;
    }
}
