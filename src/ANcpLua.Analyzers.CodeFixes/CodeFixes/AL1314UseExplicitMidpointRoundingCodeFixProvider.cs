using ANcpLua.Analyzers.Analyzers;

namespace ANcpLua.Analyzers.CodeFixes.CodeFixes;

/// <summary>
///     Code fix for AL1314: Appends <c>MidpointRounding.ToEven</c> to <c>Math.Round</c> /
///     <c>MathF.Round</c> calls that lack an explicit rounding mode.
/// </summary>
/// <remarks>
///     The fix adds <c>using System;</c> when missing — <c>MidpointRounding</c> lives in the
///     <c>System</c> namespace.
/// </remarks>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(Al1314UseExplicitMidpointRoundingCodeFixProvider))]
[Shared]
public sealed partial class Al1314UseExplicitMidpointRoundingCodeFixProvider
    : AlCodeFixProvider<InvocationExpressionSyntax> {
    private const string SystemNamespace = "System";

    /// <summary>Gets the diagnostic IDs this code fix can fix.</summary>
    public override ImmutableArray<string> FixableDiagnosticIds =>
        [Al1314UseExplicitMidpointRoundingAnalyzer.DiagnosticId];

    /// <summary>Creates the code action for this fix.</summary>
    protected override CodeAction CreateCodeAction(
        Document document,
        InvocationExpressionSyntax invocation,
        SyntaxNode root,
        Diagnostic diagnostic) =>
        CodeAction.Create(
            CodeFixResources.AL1314CodeFixTitle,
            _ => AppendMidpointRounding(document, invocation, root),
            Al1314UseExplicitMidpointRoundingAnalyzer.DiagnosticId);

    private static Task<Document> AppendMidpointRounding(
        Document document,
        InvocationExpressionSyntax invocation,
        SyntaxNode root) {
        var midpointRoundingArg = SyntaxFactory.Argument(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName("MidpointRounding"),
                SyntaxFactory.IdentifierName("ToEven")));

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

        var newUsing = SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(namespaceName))
            .WithTrailingTrivia(DetectEndOfLine(compilationUnit));

        return compilationUnit.AddUsings(newUsing);
    }

    private static SyntaxTrivia DetectEndOfLine(CompilationUnitSyntax compilationUnit) {
        // Walk the actual file trivia first so the inserted using preserves the file's CRLF/LF
        // convention. Falling back to LineFeed corrupts Windows-CRLF files; falling back to
        // CarriageReturnLineFeed corrupts LF-normalized files. Pick what the file already uses.
        foreach (var trivia in compilationUnit.DescendantTrivia()) {
            if (trivia.IsKind(SyntaxKind.EndOfLineTrivia)) {
                return trivia;
            }
        }
        return SyntaxFactory.LineFeed;
    }
}
