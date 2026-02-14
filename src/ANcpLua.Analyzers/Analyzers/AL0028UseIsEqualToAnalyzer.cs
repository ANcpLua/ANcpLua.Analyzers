
namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0028: Suggests using IsEqualTo extension instead of SymbolEqualityComparer.Equals.
/// </summary>
/// <remarks>
///     <c>SymbolEqualityComparer.Default.Equals(a, b)</c> → <c>a.IsEqualTo(b)</c>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0028UseIsEqualToAnalyzer : AlAnalyzer {
    /// <summary>The diagnostic identifier for AL0028.</summary>
    public const string DiagnosticId = "AL0028";

    /// <summary>Metadata name for SymbolEqualityComparer.</summary>
    private const string SymbolEqualityComparerTypeName = "Microsoft.CodeAnalysis.SymbolEqualityComparer";
    /// <summary>Metadata name for ISymbol.</summary>
    private const string ISymbolTypeName = "Microsoft.CodeAnalysis.ISymbol";

    private static readonly DiagnosticDescriptor Rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.RoslynUtilities,
        DiagnosticSeverity.Info);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    /// <summary>Registers compilation start action to analyze SymbolEqualityComparer usage.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterCompilationStartAction(OnCompilationStart);

    private static void OnCompilationStart(CompilationStartAnalysisContext context) {
        if (context.Compilation.GetTypeByMetadataName(ISymbolTypeName) is null) {
            return;
        }

        if (context.Compilation.GetTypeByMetadataName(SymbolEqualityComparerTypeName) is not { } symbolEqualityComparerType) {
            return;
        }

        context.RegisterOperationAction(
            ctx => AnalyzeInvocation(ctx, symbolEqualityComparerType),
            OperationKind.Invocation);
    }

    private static void AnalyzeInvocation(OperationAnalysisContext context, INamedTypeSymbol symbolEqualityComparerType) {
        if (context.Operation is not IInvocationOperation invocation) {
            return;
        }

        var method = invocation.TargetMethod;
        if (method.Name != "Equals" || method.Parameters.Length != 2) {
            return;
        }

        if (!method.ContainingType.IsEqualTo(symbolEqualityComparerType)) {
            return;
        }

        var arg0 = GetArgumentDisplayName(invocation, 0);
        var arg1 = GetArgumentDisplayName(invocation, 1);
        context.ReportDiagnostic(Rule, invocation.Syntax.GetLocation(),
            $"{arg0}.IsEqualTo({arg1})", "SymbolEqualityComparer.Default.Equals()");
    }

    private static string GetArgumentDisplayName(IInvocationOperation invocation, int index) {
        if (index >= invocation.Arguments.Length) {
            return "symbol";
        }

        var arg = invocation.Arguments[index];
        return arg.Value switch {
            ILocalReferenceOperation local => local.Local.Name,
            IParameterReferenceOperation param => param.Parameter.Name,
            IPropertyReferenceOperation prop => prop.Property.Name,
            IFieldReferenceOperation field => field.Field.Name,
            _ => "symbol"
        };
    }
}
