using ANcpLua.Analyzers.Core;

namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0028: Suggests using IsEqualTo extension instead of SymbolEqualityComparer.Equals.
/// </summary>
/// <remarks>
///     <c>SymbolEqualityComparer.Default.Equals(a, b)</c> → <c>a.IsEqualTo(b)</c>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0028UseIsEqualToAnalyzer : AlAnalyzer {
    private const string SymbolEqualityComparerTypeName = "Microsoft.CodeAnalysis.SymbolEqualityComparer";
    private const string ISymbolTypeName = "Microsoft.CodeAnalysis.ISymbol";

    private static readonly LocalizableResourceString Title = new(
        nameof(Resources.AL0028AnalyzerTitle), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableResourceString MessageFormat = new(
        nameof(Resources.AL0028AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableResourceString Description = new(
        nameof(Resources.AL0028AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.UseIsEqualTo,
        Title, MessageFormat, DiagnosticCategories.RoslynUtilities,
        DiagnosticSeverity.Info, true, Description,
        HelpLinkBase);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterCompilationStartAction(OnCompilationStart);

    private static void OnCompilationStart(CompilationStartAnalysisContext context) {
        if (context.Compilation.GetTypeByMetadataName(ISymbolTypeName) is not { }) {
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
