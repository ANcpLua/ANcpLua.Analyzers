using ANcpLua.Analyzers.Core;

namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0011: Avoid lock keyword on non-Lock types.
///     In .NET 9+, lock(Lock) is valid and preferred - only warn on lock(object).
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0011LockKeywordAnalyzer : AlAnalyzer {
    /// <summary>Metadata name for the System.Threading.Lock type (.NET 9+).</summary>
    private const string LockTypeMetadataName = "System.Threading.Lock";

    private static readonly DiagnosticDescriptor Rule = CreateRule(
        DiagnosticIds.AvoidLockKeywordOnNonLockTypes,
        DiagnosticCategories.Threading,
        DiagnosticSeverity.Warning);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    /// <summary>Registers compilation start action to analyze lock statements.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterCompilationStartAction(OnCompilationStart);

    private static void OnCompilationStart(CompilationStartAnalysisContext context) {
        var lockType = context.Compilation.GetTypeByMetadataName(LockTypeMetadataName);

        context.RegisterSyntaxNodeAction(
            ctx => AnalyzeLockStatement(ctx, lockType),
            SyntaxKind.LockStatement);
    }

    private static void AnalyzeLockStatement(SyntaxNodeAnalysisContext context, INamedTypeSymbol? lockType) {
        // If Lock type doesn't exist (.NET < 9), don't report - user can't act on it
        if (lockType is null) {
            return;
        }

        var lockStatement = (LockStatementSyntax)context.Node;

        var lockExpressionType =
            context.SemanticModel.GetTypeInfo(lockStatement.Expression, context.CancellationToken).Type;

        // Already using Lock type - no diagnostic needed
        if (lockExpressionType.IsEqualTo(lockType)) {
            return;
        }

        context.ReportDiagnostic(Rule, lockStatement.LockKeyword.GetLocation());
    }
}
