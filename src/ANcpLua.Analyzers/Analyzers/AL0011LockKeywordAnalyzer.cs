using ANcpLua.Analyzers.Core;

namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0011: Avoid lock keyword on non-Lock types.
///     In .NET 9+, lock(Lock) is valid and preferred - only warn on lock(object).
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AL0011LockKeywordAnalyzer : ALAnalyzer
{
    private const string LockTypeName = "System.Threading.Lock";

    private static readonly LocalizableResourceString Title = new(
        nameof(Resources.AL0011AnalyzerTitle), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableResourceString MessageFormat = new(
        nameof(Resources.AL0011AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableResourceString Description = new(
        nameof(Resources.AL0011AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.AvoidLockKeywordOnNonLockTypes,
        Title, MessageFormat, DiagnosticCategories.Threading,
        DiagnosticSeverity.Warning, isEnabledByDefault: true, Description,
        HelpLinkBase + "AL0011.md");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    protected override void RegisterActions(AnalysisContext context)
    {
        context.RegisterSyntaxNodeAction(AnalyzeLockStatement, SyntaxKind.LockStatement);
    }

    private static void AnalyzeLockStatement(SyntaxNodeAnalysisContext context)
    {
        var lockStatement = (LockStatementSyntax)context.Node;

        var lockExpressionType =
            context.SemanticModel.GetTypeInfo(lockStatement.Expression, context.CancellationToken).Type;

        // If locking on System.Threading.Lock, this is the correct modern pattern - don't warn
        if (lockExpressionType is not null &&
            string.Equals(lockExpressionType.ToDisplayString(), LockTypeName, StringComparison.Ordinal))
            return;

        context.ReportDiagnostic(Rule, lockStatement.LockKeyword.GetLocation());
    }
}