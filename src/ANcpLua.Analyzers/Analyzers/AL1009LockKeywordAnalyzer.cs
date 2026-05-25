
namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL1009: Avoid lock keyword on non-Lock types.
///     In .NET 9+, lock(Lock) is valid and preferred - only warn on lock(object).
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al1009LockKeywordAnalyzer : AlAnalyzer {
    /// <summary>The diagnostic identifier for AL1009.</summary>
    public const string DiagnosticId = "AL1009";

    /// <summary>Metadata name for the System.Threading.Lock type (.NET 9+).</summary>
    private const string LockTypeMetadataName = "System.Threading.Lock";

    private static readonly DiagnosticDescriptor s_rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.Threading,
        DiagnosticSeverity.Warning);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_rule];

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
        if (lockType is null) {
            return;
        }

        var lockStatement = (LockStatementSyntax)context.Node;

        var lockExpressionType =
            ModelExtensions.GetTypeInfo(context.SemanticModel, lockStatement.Expression, context.CancellationToken).Type;

        if (lockExpressionType.IsEqualTo(lockType)) {
            return;
        }

        context.ReportDiagnostic(s_rule, lockStatement.LockKeyword.GetLocation());
    }
}
