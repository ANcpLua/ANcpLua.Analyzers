namespace ANcpLua.Analyzers.CodeFixes.CodeFixes;

/// <summary>
///     Shared rewriting for the AL12xx <c>Guard.*</c> code fixes (AL1208, AL1212–AL1218, AL1220).
/// </summary>
internal static partial class GuardCodeFixes {
    private const string GuardNamespace = "ANcpLua.Roslyn.Utilities";

    /// <summary>
    ///     Adds <c>using ANcpLua.Roslyn.Utilities;</c> to the compilation unit when it is missing.
    ///     Returns <paramref name="root" /> unchanged when it is not a compilation unit.
    /// </summary>
    internal static SyntaxNode AddGuardUsingIfMissing(SyntaxNode root) {
        if (root is not CompilationUnitSyntax compilationUnit) {
            return root;
        }

        if (compilationUnit.Usings.Any(static u => u.Name?.ToString() == GuardNamespace)) {
            return root;
        }

        var newUsing = SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(GuardNamespace))
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
