using ANcpLua.Analyzers.Analyzers;

namespace ANcpLua.Analyzers.CodeFixes.CodeFixes;

/// <summary>
///     Code fix for AL1220: Rewrites BCL throw helpers to <c>Guard.*</c> from <c>ANcpLua.Roslyn.Utilities</c>.
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
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(Al1220UseGuardForThrowIfCodeFixProvider))]
[Shared]
public sealed partial class Al1220UseGuardForThrowIfCodeFixProvider
    : AlCodeFixProvider<InvocationExpressionSyntax> {
    /// <summary>Gets the diagnostic IDs this code fix can fix.</summary>
    public override ImmutableArray<string> FixableDiagnosticIds => [Al1220UseGuardForThrowIfAnalyzer.DiagnosticId];

    /// <summary>Creates the code action for this fix.</summary>
    protected override CodeAction CreateCodeAction(
        Document document,
        InvocationExpressionSyntax invocation,
        SyntaxNode root,
        Diagnostic diagnostic) =>
        CodeAction.Create(
            CodeFixResources.AL1220CodeFixTitle,
            _ => ConvertToGuard(document, invocation, root, diagnostic),
            Al1220UseGuardForThrowIfAnalyzer.DiagnosticId);

    private static Task<Document> ConvertToGuard(
        Document document,
        InvocationExpressionSyntax invocation,
        SyntaxNode root,
        Diagnostic diagnostic) {
        if (!diagnostic.Properties.TryGetValue(
                Al1220UseGuardForThrowIfAnalyzer.PropertyGuardMethod, out var guardMethod) ||
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
        newRoot = GuardCodeFixes.AddGuardUsingIfMissing(newRoot);

        return Task.FromResult(document.WithSyntaxRoot(newRoot));
    }
}
