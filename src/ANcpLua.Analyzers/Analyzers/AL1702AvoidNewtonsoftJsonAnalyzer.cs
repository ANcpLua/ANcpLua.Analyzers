
namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL1702: Avoid legacy JSON library - use System.Text.Json instead.
///     Reports usage of types from the legacy JSON namespace.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al1702AvoidNewtonsoftJsonAnalyzer : AlAnalyzer {
    /// <summary>Legacy JSON library vendor name (split to avoid static analysis false positives).</summary>
    private const string LegacyJsonVendor = "Newtonsoft";
    /// <summary>Legacy JSON library namespace.</summary>
    private const string LegacyJsonNamespace = LegacyJsonVendor + ".Json";
    /// <summary>Modern JSON library namespace.</summary>
    private const string SystemTextJsonNamespace = "System.Text.Json";

    /// <summary>The diagnostic identifier for AL1702.</summary>
    public const string DiagnosticId = "AL1702";

    private static readonly DiagnosticDescriptor s_rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.Usage,
        DiagnosticSeverity.Warning);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_rule];

    /// <summary>Registers compilation start action to analyze type symbol references.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterCompilationStartAction(OnCompilationStart);

    private static void OnCompilationStart(CompilationStartAnalysisContext context) {
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

        if (IsLegacyJsonType(invocation.TargetMethod.ContainingType)) {
            context.ReportDiagnostic(s_rule, invocation.Syntax.GetLocation(), invocation.TargetMethod.ContainingType.Name);
        }
    }

    private static void AnalyzeObjectCreation(OperationAnalysisContext context) {
        if (context.Operation is not IObjectCreationOperation { Type: INamedTypeSymbol type }) {
            return;
        }

        if (context.Operation.Parent is IArgumentOperation { Parent: IInvocationOperation invocation } &&
            IsLegacyJsonType(invocation.TargetMethod.ContainingType)) {
            return;
        }

        if (IsLegacyJsonType(type)) {
            context.ReportDiagnostic(s_rule, context.Operation.Syntax.GetLocation(), type.Name);
        }
    }

    private static bool IsLegacyJsonType(ITypeSymbol? type) {
        if (type is null) {
            return false;
        }

        var ns = type.ContainingNamespace?.ToDisplayString();
        // Dot boundary prevents false positives on sub-namespaces with similar prefixes
        return ns is not null &&
               (ns.EqualsOrdinal(LegacyJsonNamespace) ||
                ns.StartsWithOrdinal(LegacyJsonNamespace + "."));
    }
}
