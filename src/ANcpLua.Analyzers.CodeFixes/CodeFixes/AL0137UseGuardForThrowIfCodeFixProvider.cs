using ANcpLua.Analyzers.Analyzers;

namespace ANcpLua.Analyzers.CodeFixes.CodeFixes;

/// <summary>
///     Code fix for AL0137: Rewrites BCL throw helpers to <c>Guard.*</c> from <c>ANcpLua.Roslyn.Utilities</c>.
/// </summary>
/// <remarks>
///     <list type="bullet">
///         <item><c>ArgumentNullException.ThrowIfNull(x)</c> → <c>Guard.NotNull(x)</c></item>
///         <item><c>ArgumentException.ThrowIfNullOrEmpty(s)</c> → <c>Guard.NotNullOrEmpty(s)</c></item>
///         <item><c>ArgumentException.ThrowIfNullOrWhiteSpace(s)</c> → <c>Guard.NotNullOrWhiteSpace(s)</c></item>
///     </list>
///     A <c>using ANcpLua.Roslyn.Utilities;</c> directive is added to the compilation unit when missing.
///     Argument list (including any explicit <c>paramName</c>) is preserved verbatim — Guard.* shares the
///     <c>[CallerArgumentExpression]</c> contract.
/// </remarks>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(Al0137UseGuardForThrowIfCodeFixProvider))]
[Shared]
public sealed partial class Al0137UseGuardForThrowIfCodeFixProvider
    : AlCodeFixProvider<InvocationExpressionSyntax> {
    private const string GuardNamespace = "ANcpLua.Roslyn.Utilities";

    /// <summary>Gets the diagnostic IDs this code fix can fix.</summary>
    public override ImmutableArray<string> FixableDiagnosticIds => [Al0137UseGuardForThrowIfAnalyzer.DiagnosticId];

    /// <summary>Creates the code action for this fix.</summary>
    protected override CodeAction CreateCodeAction(
        Document document,
        InvocationExpressionSyntax invocation,
        SyntaxNode root,
        Diagnostic diagnostic) =>
        CodeAction.Create(
            CodeFixResources.AL0137CodeFixTitle,
            _ => ConvertToGuard(document, invocation, root, diagnostic),
            nameof(Al0137UseGuardForThrowIfCodeFixProvider));

    private static Task<Document> ConvertToGuard(
        Document document,
        InvocationExpressionSyntax invocation,
        SyntaxNode root,
        Diagnostic diagnostic) {
        if (!diagnostic.Properties.TryGetValue(
                Al0137UseGuardForThrowIfAnalyzer.PropertyGuardMethod, out var guardMethod) ||
            guardMethod is not { Length: > 0 } guardName) {
            return Task.FromResult(document);
        }

        var newInvocation = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName("Guard"),
                    SyntaxFactory.IdentifierName(guardName)),
                invocation.ArgumentList)
            .WithTriviaFrom(invocation);

        var newRoot = root.ReplaceNode(invocation, newInvocation);
        newRoot = AddUsingIfMissing(newRoot, GuardNamespace);

        return Task.FromResult(document.WithSyntaxRoot(newRoot));
    }

    private static SyntaxNode AddUsingIfMissing(SyntaxNode root, string namespaceName) {
        if (root is not CompilationUnitSyntax compilationUnit) {
            return root;
        }

        if (compilationUnit.Usings.Any(u => u.Name?.ToString() == namespaceName)) {
            return root;
        }

        // Match the EOL of an existing using if present, otherwise default to LineFeed.
        // Hard-coding CarriageReturnLineFeed produces wrong output on LF-normalized files.
        var endOfLine = compilationUnit.Usings.LastOrDefault()?.GetTrailingTrivia()
            .FirstOrDefault(t => t.IsKind(SyntaxKind.EndOfLineTrivia)) ?? SyntaxFactory.LineFeed;

        var newUsing = SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(namespaceName))
            .WithTrailingTrivia(endOfLine);

        return compilationUnit.AddUsings(newUsing);
    }
}
