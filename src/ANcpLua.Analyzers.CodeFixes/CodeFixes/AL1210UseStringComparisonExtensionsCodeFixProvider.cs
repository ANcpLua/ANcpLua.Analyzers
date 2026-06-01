using ANcpLua.Analyzers.Analyzers;

namespace ANcpLua.Analyzers.CodeFixes.CodeFixes;

/// <summary>
///     Code fix for AL1210: Converts StringComparison method calls to extension methods.
/// </summary>
/// <remarks>
///     <list type="bullet">
///         <item><c>str.Equals(other, StringComparison.Ordinal)</c> → <c>str.EqualsOrdinal(other)</c></item>
///         <item><c>str.Equals(other, StringComparison.OrdinalIgnoreCase)</c> → <c>str.EqualsIgnoreCase(other)</c></item>
///         <item><c>str.StartsWith(prefix, StringComparison.Ordinal)</c> → <c>str.StartsWithOrdinal(prefix)</c></item>
///         <item><c>str.Contains(sub, StringComparison.OrdinalIgnoreCase)</c> → <c>str.ContainsIgnoreCase(sub)</c></item>
///     </list>
///     A <c>using ANcpLua.Roslyn.Utilities;</c> directive is added to the compilation unit when missing,
///     so the rewritten call resolves to <c>StringComparisonExtensions</c>.
/// </remarks>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(Al1210UseStringComparisonExtensionsCodeFixProvider))]
[Shared]
public sealed partial class Al1210UseStringComparisonExtensionsCodeFixProvider
    : AlCodeFixProvider<InvocationExpressionSyntax> {
    private const string ExtensionsNamespace = "ANcpLua.Roslyn.Utilities";

    // Mapping from StringComparison value to extension suffix. Only Ordinal/OrdinalIgnoreCase have
    // StringComparisonExtensions equivalents; culture-aware comparisons have no extension method.
    // Kept in sync with MappingRegistry.s_stringComparisonSuffixes — the analyzer/code-fix assembly
    // boundary (no InternalsVisibleTo) prevents sharing the analyzer's internal registry directly.
    private static readonly Dictionary<string, string> s_comparisonToSuffix = new(StringComparer.Ordinal) {
        ["Ordinal"] = "Ordinal",
        ["OrdinalIgnoreCase"] = "IgnoreCase"
    };

    /// <summary>Gets the diagnostic IDs this code fix can fix.</summary>
    public override ImmutableArray<string> FixableDiagnosticIds => [Al1210UseStringComparisonExtensionsAnalyzer.DiagnosticId];

    /// <summary>Creates the code action for this fix.</summary>
    protected override CodeAction CreateCodeAction(
        Document document,
        InvocationExpressionSyntax invocation,
        SyntaxNode root,
        Diagnostic diagnostic) =>
        CodeAction.Create(
            CodeFixResources.AL1210CodeFixTitle,
            _ => ConvertToExtensionMethod(document, invocation, root),
            Al1210UseStringComparisonExtensionsAnalyzer.DiagnosticId);

    private static Task<Document> ConvertToExtensionMethod(
        Document document,
        InvocationExpressionSyntax invocation,
        SyntaxNode root) {
        // Get the method name from member access (e.g., str.Equals -> "Equals")
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess) {
            return Task.FromResult(document);
        }

        var methodName = memberAccess.Name.Identifier.Text;
        var receiver = memberAccess.Expression;

        // Find StringComparison argument and its value, also collect non-comparison args
        string? comparisonValue = null;
        var nonComparisonArgs = new List<ArgumentSyntax>();

        foreach (var arg in invocation.ArgumentList.Arguments) {
            if (IsStringComparisonArgument(arg, out var value)) {
                comparisonValue = value;
            } else {
                nonComparisonArgs.Add(arg);
            }
        }

        // If we couldn't find a valid comparison, bail out
        if (comparisonValue is null || !s_comparisonToSuffix.TryGetValue(comparisonValue, out var suffix)) {
            return Task.FromResult(document);
        }

        // Build the new extension method name (e.g., "EqualsOrdinal")
        var extensionName = $"{methodName}{suffix}";

        // Create new argument list without the StringComparison argument
        var newArgumentList = SyntaxFactory.ArgumentList(
            SyntaxFactory.SeparatedList(
                nonComparisonArgs.Select(static a => a.WithoutTrivia())));

        // Create: receiver.ExtensionMethod(args)
        var newExpression = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    receiver.WithoutTrivia(),
                    SyntaxFactory.IdentifierName(extensionName)),
                newArgumentList)
            .WithTriviaFrom(invocation);

        var newRoot = root.ReplaceNode(invocation, newExpression);
        newRoot = AddUsingIfMissing(newRoot, ExtensionsNamespace);
        return Task.FromResult(document.WithSyntaxRoot(newRoot));
    }

    private static SyntaxNode AddUsingIfMissing(SyntaxNode root, string namespaceName) {
        if (root is not CompilationUnitSyntax compilationUnit) {
            return root;
        }

        if (compilationUnit.Usings.Any(u => u.Name?.ToString() == namespaceName)) {
            return root;
        }

        var newUsing = SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(namespaceName))
            .WithTrailingTrivia(DetectEndOfLine(compilationUnit));

        return compilationUnit.AddUsings(newUsing);
    }

    private static SyntaxTrivia DetectEndOfLine(CompilationUnitSyntax compilationUnit) {
        // Preserve the file's CRLF/LF convention so the inserted using does not corrupt
        // line endings; fall back to LineFeed only when the file has no end-of-line trivia.
        foreach (var trivia in compilationUnit.DescendantTrivia()) {
            if (trivia.IsKind(SyntaxKind.EndOfLineTrivia)) {
                return trivia;
            }
        }

        return SyntaxFactory.LineFeed;
    }

    private static bool IsStringComparisonArgument(ArgumentSyntax argument, out string? value) {
        value = null;

        // Look for StringComparison.XYZ pattern
        if (argument.Expression is MemberAccessExpressionSyntax { Name: IdentifierNameSyntax memberName } memberAccess) {
            var expressionText = memberAccess.Expression.ToString();
            if (expressionText is "StringComparison" or "System.StringComparison") {
                value = memberName.Identifier.Text;
                return true;
            }
        }

        return false;
    }
}
