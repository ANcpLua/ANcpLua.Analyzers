
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
///         The analyzer flags catch blocks whose body contains zero statements.
///         Catch blocks that contain any statement — throw, return, invocation,
///         assignment, or anything else — are considered handled and not reported.
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

        context.ReportDiagnostic(Rule, catchClause.CatchKeyword.GetLocation());
    }
}
