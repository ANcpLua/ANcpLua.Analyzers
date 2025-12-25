using ANcpLua.Analyzers.Core;

namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0011: Avoid lock keyword on non-Lock types.
///     In .NET 9+, lock(Lock) is valid and preferred - only warn on lock(object).
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AL0011LockKeywordAnalyzer : ALAnalyzer
{
    private const string LockTypeMetadataName = "System.Threading.Lock";

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
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    private static void OnCompilationStart(CompilationStartAnalysisContext context)
    {
        var lockType = context.Compilation.GetTypeByMetadataName(LockTypeMetadataName);

        if (lockType is null)
            return;

        context.RegisterSyntaxNodeAction(
            ctx => AnalyzeLockStatement(ctx, lockType),
            SyntaxKind.LockStatement);
    }

    private static void AnalyzeLockStatement(SyntaxNodeAnalysisContext context, INamedTypeSymbol lockType)
    {
        var lockStatement = (LockStatementSyntax)context.Node;

        var lockExpressionType =
            context.SemanticModel.GetTypeInfo(lockStatement.Expression, context.CancellationToken).Type;

        if (SymbolEqualityComparer.Default.Equals(lockExpressionType, lockType))
            return;

        context.ReportDiagnostic(Rule, lockStatement.LockKeyword.GetLocation());
    }
}
