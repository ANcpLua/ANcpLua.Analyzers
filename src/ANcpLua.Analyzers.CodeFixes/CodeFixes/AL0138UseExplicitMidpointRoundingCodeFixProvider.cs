using ANcpLua.Analyzers.Analyzers;

namespace ANcpLua.Analyzers.CodeFixes.CodeFixes;

/// <summary>
///     Code fix for AL0138: Appends <c>MidpointRounding.AwayFromZero</c> to <c>Math.Round</c> /
///     <c>MathF.Round</c> calls that lack an explicit rounding mode.
/// </summary>
/// <remarks>
///     The fix adds <c>using System;</c> when missing — <c>MidpointRounding</c> lives in the
///     <c>System</c> namespace.
/// </remarks>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(Al0138UseExplicitMidpointRoundingCodeFixProvider))]
[Shared]
public sealed partial class Al0138UseExplicitMidpointRoundingCodeFixProvider
    : AlCodeFixProvider<InvocationExpressionSyntax> {
    private const string SystemNamespace = "System";

    /// <summary>Gets the diagnostic IDs this code fix can fix.</summary>
    public override ImmutableArray<string> FixableDiagnosticIds =>
        [Al0138UseExplicitMidpointRoundingAnalyzer.DiagnosticId];

    /// <summary>Creates the code action for this fix.</summary>
    protected override CodeAction CreateCodeAction(
        Document document,
        InvocationExpressionSyntax invocation,
        SyntaxNode root,
        Diagnostic diagnostic) =>
        CodeAction.Create(
            CodeFixResources.AL0138CodeFixTitle,
            _ => AppendMidpointRounding(document, invocation, root),
            nameof(Al0138UseExplicitMidpointRoundingCodeFixProvider));

    private static Task<Document> AppendMidpointRounding(
        Document document,
        InvocationExpressionSyntax invocation,
        SyntaxNode root) {
        var midpointRoundingArg = SyntaxFactory.Argument(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName("MidpointRounding"),
                SyntaxFactory.IdentifierName("AwayFromZero")));

        var newArgumentList = invocation.ArgumentList.AddArguments(midpointRoundingArg);
        var newInvocation = invocation.WithArgumentList(newArgumentList);

        var newRoot = root.ReplaceNode(invocation, newInvocation);
        newRoot = AddUsingIfMissing(newRoot, SystemNamespace);

        return Task.FromResult(document.WithSyntaxRoot(newRoot));
    }

    private static SyntaxNode AddUsingIfMissing(SyntaxNode root, string namespaceName) {
        if (root is not CompilationUnitSyntax compilationUnit) {
            return root;
        }

        if (compilationUnit.Usings.Any(u => u.Name?.ToString() == namespaceName)) {
            return root;
        }

        var endOfLine = compilationUnit.Usings.LastOrDefault()?.GetTrailingTrivia()
            .FirstOrDefault(t => t.IsKind(SyntaxKind.EndOfLineTrivia)) ?? SyntaxFactory.LineFeed;

        var newUsing = SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(namespaceName))
            .WithTrailingTrivia(endOfLine);

        return compilationUnit.AddUsings(newUsing);
    }
}
