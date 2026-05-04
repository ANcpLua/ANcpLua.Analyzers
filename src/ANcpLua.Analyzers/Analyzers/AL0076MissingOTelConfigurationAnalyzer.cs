
namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0076: Detects when AddServiceDefaults() or similar setup is called but AddOpenTelemetry() is missing.
/// </summary>
/// <remarks>
///     <para>
///         This analyzer detects incomplete telemetry configuration where service defaults
///         are configured but OpenTelemetry is not set up, meaning telemetry will not be exported.
///     </para>
///     <para>
///         Methods that trigger this diagnostic:
///         <list type="bullet">
///             <item>AddServiceDefaults</item>
///             <item>AddQylServiceDefaults</item>
///             <item>ConfigureOpenTelemetry</item>
///         </list>
///     </para>
///     <para>
///         Methods that satisfy the OpenTelemetry requirement:
///         <list type="bullet">
///             <item>AddOpenTelemetry</item>
///             <item>WithTracing</item>
///             <item>UseOpenTelemetry</item>
///         </list>
///     </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0076MissingOTelConfigurationAnalyzer : AlAnalyzer {
    private static readonly string[] s_serviceDefaultsMethods = [
        "AddServiceDefaults",
        "AddQylServiceDefaults",
        "ConfigureOpenTelemetry"
    ];

    private static readonly string[] s_oTelConfigurationMethods = [
        "AddOpenTelemetry",
        "WithTracing",
        "UseOpenTelemetry"
    ];

    /// <summary>The diagnostic identifier for AL0076.</summary>
    private const string DiagnosticId = "AL0076";

    private static readonly DiagnosticDescriptor s_rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.OpenTelemetry,
        DiagnosticSeverities.Suggestion);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_rule];

    /// <summary>Registers syntax tree actions to analyze OpenTelemetry configuration.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context) {
        var invocation = (InvocationExpressionSyntax)context.Node;

        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess
            || !s_serviceDefaultsMethods.Contains(memberAccess.Name.Identifier.Text)) {
            return;
        }

        if (invocation.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault() is not { } containingMethod) {
            return;
        }

        var allInvocations = new HashSet<string>();
        foreach (var inv in containingMethod.DescendantNodes().OfType<InvocationExpressionSyntax>()) {
            if (GetMethodName(inv) is { } name) {
                allInvocations.Add(name);
            }
        }

        if (!s_oTelConfigurationMethods.Any(allInvocations.Contains)) {
            context.ReportDiagnostic(Diagnostic.Create(s_rule, invocation.GetLocation()));
        }
    }

    // Only match member access to avoid matching local methods with same name
    private static string? GetMethodName(InvocationExpressionSyntax invocation) =>
        invocation.Expression switch {
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.Text,
            _ => null
        };
}
