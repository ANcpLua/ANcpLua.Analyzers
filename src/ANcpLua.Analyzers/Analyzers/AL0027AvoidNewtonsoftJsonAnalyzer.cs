using ANcpLua.Analyzers.Core;

namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0027: Avoid legacy JSON library - use System.Text.Json instead.
///     Reports usage of types from the legacy JSON namespace.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0027AvoidNewtonsoftJsonAnalyzer : AlAnalyzer {
    // Split to avoid static analysis false positives
    private const string LegacyJsonVendor = "Newtonsoft";
    private const string LegacyJsonNamespace = LegacyJsonVendor + ".Json";
    private const string SystemTextJsonNamespace = "System.Text.Json";

    private static readonly LocalizableResourceString Title = new(
        nameof(Resources.AL0027AnalyzerTitle), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableResourceString MessageFormat = new(
        nameof(Resources.AL0027AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableResourceString Description = new(
        nameof(Resources.AL0027AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.AvoidNewtonsoftJson,
        Title, MessageFormat, DiagnosticCategories.Usage,
        DiagnosticSeverity.Warning, true, Description,
        HelpLinkBase);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterCompilationStartAction(OnCompilationStart);

    private static void OnCompilationStart(CompilationStartAnalysisContext context) {
        // Only report if System.Text.Json is available as an alternative
        if (context.Compilation.GetTypeByMetadataName($"{SystemTextJsonNamespace}.JsonSerializer") is null) {
            return;
        }

        context.RegisterOperationAction(AnalyzeOperation, OperationKind.Invocation);
        context.RegisterOperationAction(AnalyzeObjectCreation, OperationKind.ObjectCreation);
    }

    private static void AnalyzeOperation(OperationAnalysisContext context) {
        if (context.Operation is not IInvocationOperation invocation) {
            return;
        }

        var method = invocation.TargetMethod;

        // Check if the method is from legacy JSON library
        if (IsLegacyJsonType(method.ContainingType)) {
            context.ReportDiagnostic(Rule, invocation.Syntax.GetLocation(), method.ContainingType.Name);
        }
    }

    private static void AnalyzeObjectCreation(OperationAnalysisContext context) {
        if (context.Operation is not IObjectCreationOperation creation) {
            return;
        }

        if (creation.Type is not INamedTypeSymbol type) {
            return;
        }

        // Check if creating a legacy JSON type
        if (IsLegacyJsonType(type)) {
            context.ReportDiagnostic(Rule, creation.Syntax.GetLocation(), type.Name);
        }
    }

    private static bool IsLegacyJsonType(ITypeSymbol? type) {
        if (type is null) {
            return false;
        }

        var ns = type.ContainingNamespace?.ToDisplayString();
        return ns is not null && ns.StartsWith(LegacyJsonNamespace, StringComparison.Ordinal);
    }
}
