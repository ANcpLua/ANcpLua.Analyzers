namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0132: Detects deprecated OpenTelemetry semantic convention values.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0132DeprecatedSemconvValueAnalyzer : AlAnalyzer {
    /// <summary>The diagnostic identifier for AL0132.</summary>
    private const string DiagnosticId = "AL0132";

    private static readonly Dictionary<string, Dictionary<string, string>> DeprecatedValues =
        new(StringComparer.OrdinalIgnoreCase) {
            ["cloud.platform"] = new(StringComparer.OrdinalIgnoreCase) {
                ["azure_aks"] = "Use 'azure.aks' instead.",
                ["azure_app_service"] = "Use 'azure.app_service' instead.",
                ["azure_container_apps"] = "Use 'azure.container_apps' instead.",
                ["azure_container_instances"] = "Use 'azure.container_instances' instead.",
                ["azure_functions"] = "Use 'azure.functions' instead.",
                ["azure_openshift"] = "Use 'azure.openshift' instead.",
                ["azure_vm"] = "Use 'azure.vm' instead."
            },
            ["db.system"] = new(StringComparer.OrdinalIgnoreCase) {
                ["cache"] = "Use 'intersystems_cache' instead.",
                ["cloudscape"] = "Use 'other_sql' instead.",
                ["coldfusion"] = "No replacement exists at this time.",
                ["firstsql"] = "Use 'other_sql' instead.",
                ["mssqlcompact"] = "Use 'other_sql' instead."
            },
            ["gen_ai.system"] = new(StringComparer.OrdinalIgnoreCase) {
                ["az.ai.inference"] = "Use 'azure.ai.inference' instead.",
                ["az.ai.openai"] = "Use 'azure.ai.openai' instead.",
                ["gemini"] = "Use 'gcp.gemini' instead.",
                ["vertex_ai"] = "Use 'gcp.vertex_ai' instead."
            },
            ["messaging.operation.type"] = new(StringComparer.OrdinalIgnoreCase) {
                ["deliver"] = "Use 'process' instead.",
                ["publish"] = "Use 'send' instead."
            },
            ["os.type"] = new(StringComparer.OrdinalIgnoreCase) {
                ["z_os"] = "Use 'zos' instead."
            },
            ["system.memory.state"] = new(StringComparer.OrdinalIgnoreCase) {
                ["shared"] = "Report shared memory usage with 'system.memory.linux.shared' instead."
            },
            ["vcs.provider.name"] = new(StringComparer.OrdinalIgnoreCase) {
                ["gittea"] = "Use 'gitea' instead."
            }
        };

    private static readonly DiagnosticDescriptor Rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.OpenTelemetry,
        DiagnosticSeverity.Warning);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    /// <summary>Registers operation actions to analyze telemetry attribute setter invocations.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);

    private static void AnalyzeInvocation(OperationAnalysisContext context) {
        var invocation = (IInvocationOperation)context.Operation;

        if (!IsAttributeSetterMethod(invocation.TargetMethod)
            || invocation.Arguments.Length < 2
            || !invocation.Arguments[0].Value.TryGetConstantValue(out string? attributeName)
            || attributeName is null
            || !DeprecatedValues.TryGetValue(attributeName, out var deprecatedValues)) {
            return;
        }

        var valueOperation = invocation.Arguments[1].Value.UnwrapAllConversions();
        if (!valueOperation.TryGetConstantValue(out string? attributeValue)
            || attributeValue is null
            || !deprecatedValues.TryGetValue(attributeValue, out var guidance)) {
            return;
        }

        context.ReportDiagnostic(Rule, invocation.Arguments[1].Syntax.GetLocation(), attributeName, attributeValue, guidance);
    }

    private static bool IsAttributeSetterMethod(IMethodSymbol method) =>
        method.Name switch {
            "SetTag" or "SetAttribute" or "AddTag" or "AddAttribute" or "SetCustomProperty" => true,
            "Add" => method.ContainingType?.Name is { } name
                && (name.ContainsOrdinal("Tag") || name.ContainsOrdinal("Attribute")),
            _ => false
        };
}
