using ANcpLua.Analyzers.Core;

namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0093: Detects when OpenTelemetry is configured without essential resource attributes.
/// </summary>
/// <remarks>
///     <para>
///         Resource attributes like 'service.name' and 'service.version' are critical for
///         identifying services in observability backends. Without them, traces and metrics
///         appear with generic or missing service identification, making it difficult to
///         correlate telemetry across services.
///     </para>
///     <para>
///         This analyzer detects calls to AddOpenTelemetry() or similar configuration methods
///         and checks if ConfigureResource() or AddService() is called to set the service identity.
///     </para>
///     <para>
///         The fix is to add ResourceBuilder.CreateDefault().AddService(serviceName, serviceVersion)
///         or use ConfigureResource() with appropriate service attributes.
///     </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0093MissingResourceAttributesAnalyzer : AlAnalyzer {
    private static readonly string[] OTelSetupMethods = [
        "AddOpenTelemetry",
        "UseOpenTelemetry",
        "ConfigureOpenTelemetry"
    ];

    private static readonly string[] ResourceConfigMethods = [
        "ConfigureResource",
        "AddResource",
        "AddService",
        "SetResourceBuilder"
    ];

    private static readonly DiagnosticDescriptor Rule = CreateRule(
        DiagnosticIds.MissingResourceAttributes,
        DiagnosticCategories.OpenTelemetry,
        DiagnosticSeverities.Suggestion);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    /// <summary>Registers syntax node actions to analyze OpenTelemetry configuration.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context) {
        var invocation = (InvocationExpressionSyntax)context.Node;

        // Look for OTel setup calls
        var methodName = GetMethodName(invocation);
        if (!IsOTelSetupMethod(methodName)) {
            return;
        }

        // Check if this is in a method body (likely configuration code)
        if (invocation.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault() is not { } containingMethod) {
            return;
        }

        // Collect all method invocations in the same method
        var allInvocations = new HashSet<string>();
        foreach (var inv in containingMethod.DescendantNodes().OfType<InvocationExpressionSyntax>()) {
            var name = GetMethodName(inv);
            if (name is not null) {
                allInvocations.Add(name);
            }
        }

        // Check if any resource configuration method is present
        var hasResourceConfig = ResourceConfigMethods.Any(allInvocations.Contains);

        if (!hasResourceConfig) {
            var location = GetMethodNameLocation(invocation);
            context.ReportDiagnostic(Diagnostic.Create(
                Rule,
                location,
                "service.name/service.version"));
        }
    }

    private static bool IsOTelSetupMethod(string? methodName) =>
        methodName is not null && OTelSetupMethods.Contains(methodName);

    private static string? GetMethodName(InvocationExpressionSyntax invocation) =>
        invocation.Expression switch {
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.Text,
            IdentifierNameSyntax identifier => identifier.Identifier.Text,
            _ => null
        };

    private static Location GetMethodNameLocation(InvocationExpressionSyntax invocation) =>
        invocation.Expression switch {
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name.GetLocation(),
            IdentifierNameSyntax identifier => identifier.GetLocation(),
            _ => invocation.GetLocation()
        };
}
