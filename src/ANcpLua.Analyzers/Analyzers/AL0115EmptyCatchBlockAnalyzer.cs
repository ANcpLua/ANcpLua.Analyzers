
namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0115: Detects empty catch blocks that silently swallow exceptions.
/// </summary>
/// <remarks>
///     <para>
///         Empty catch blocks hide failures by discarding exceptions without logging,
///         rethrowing, or any meaningful handling. This makes bugs invisible and
///         significantly complicates production debugging.
///     </para>
///     <para>
///         The analyzer flags catch blocks whose body contains zero statements AND no explanatory
///         comment. A catch with an explanatory comment is considered a deliberate swallow —
///         the author has documented the <em>why</em>, which is the whole point of the rule.
///         Catch blocks that contain any statement are likewise treated as handled.
///     </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0115EmptyCatchBlockAnalyzer : AlAnalyzer {
    /// <summary>The diagnostic identifier for AL0115.</summary>
    private const string DiagnosticId = "AL0115";

    private static readonly DiagnosticDescriptor Rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.Reliability,
        DiagnosticSeverity.Warning);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    /// <summary>Registers syntax node action to analyze catch clauses.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterSyntaxNodeAction(AnalyzeCatchClause, SyntaxKind.CatchClause);

    private static void AnalyzeCatchClause(SyntaxNodeAnalysisContext context) {
        var catchClause = (CatchClauseSyntax)context.Node;

        if (catchClause.Block.Statements.Count is not 0) {
            return;
        }

        // A comment inside the block is the canonical way to document a deliberate swallow.
        // The trailing trivia of the open brace and leading trivia of the close brace together
        // cover the interior of the block.
        if (HasExplanatoryComment(catchClause.Block)) {
            return;
        }

        context.ReportDiagnostic(Rule, catchClause.CatchKeyword.GetLocation());
    }

    private static bool HasExplanatoryComment(BlockSyntax block) {
        foreach (var trivia in block.OpenBraceToken.TrailingTrivia) {
            if (IsComment(trivia)) return true;
        }

        foreach (var trivia in block.CloseBraceToken.LeadingTrivia) {
            if (IsComment(trivia)) return true;
        }

        return false;
    }

    private static bool IsComment(SyntaxTrivia trivia) =>
        trivia.IsKind(SyntaxKind.SingleLineCommentTrivia)
        || trivia.IsKind(SyntaxKind.MultiLineCommentTrivia)
        || trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia)
        || trivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia);
}
