namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0132: Detects deprecated OpenTelemetry semantic convention values.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0132DeprecatedSemconvValueAnalyzer : AlAnalyzer {
    /// <summary>The diagnostic identifier for AL0132.</summary>
    private const string DiagnosticId = "AL0132";

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
            || attributeName is null) {
            return;
        }

        var valueOperation = invocation.Arguments[1].Value.UnwrapAllConversions();
        if (!valueOperation.TryGetConstantValue(out string? attributeValue)
            || attributeValue is null
            || !OpenTelemetryDeprecatedSemconvCatalog.TryGetDeprecatedAttributeValue(attributeName, attributeValue, out var guidance)) {
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
