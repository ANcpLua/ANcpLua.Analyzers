
namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0070: Detects collector endpoint configurations that don't use OTLP protocol.
/// </summary>
/// <remarks>
///     <para>
///         Collector endpoints should use the OTLP protocol for standardized telemetry export:
///         <list type="bullet">
///             <item>gRPC: grpc://host:4317</item>
///             <item>HTTP: http://host:4318/v1/traces (or /v1/metrics, /v1/logs)</item>
///         </list>
///     </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0070NonOtlpCollectorEndpointAnalyzer : AlAnalyzer {
    private static readonly string[] OtlpPatterns = [
        "4317", // gRPC default port
        "4318", // HTTP default port
        "/v1/traces",
        "/v1/metrics",
        "/v1/logs",
        "otlp"
    ];

    private static readonly string[] EndpointPropertyNames = [
        "Endpoint", "CollectorEndpoint", "OtlpEndpoint", "ExporterEndpoint"
    ];

    /// <summary>The diagnostic identifier for AL0070.</summary>
    public const string DiagnosticId = "AL0070";

    private static readonly DiagnosticDescriptor Rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.Configuration,
        DiagnosticSeverities.Suggestion);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    /// <summary>Registers operation actions to analyze endpoint assignments.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterOperationAction(AnalyzeAssignment, OperationKind.SimpleAssignment);

    private static void AnalyzeAssignment(OperationAnalysisContext context) {
        var assignment = (ISimpleAssignmentOperation)context.Operation;

        if (GetPropertyName(assignment.Target) is not { } propertyName ||
            !EndpointPropertyNames.Contains(propertyName, StringComparer.OrdinalIgnoreCase) ||
            assignment.Value.ConstantValue is not { HasValue: true, Value: string endpoint } ||
            IsOtlpEndpoint(endpoint)) {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(Rule, assignment.Syntax.GetLocation(), endpoint));
    }

    private static string? GetPropertyName(IOperation target) =>
        target switch {
            IPropertyReferenceOperation propRef => propRef.Property.Name,
            IMemberReferenceOperation memberRef => memberRef.Member.Name,
            _ => null
        };

    private static bool IsOtlpEndpoint(string endpoint) =>
        OtlpPatterns.Any(endpoint.ContainsIgnoreCase);
}
