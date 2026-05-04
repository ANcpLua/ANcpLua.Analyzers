
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
    /// <summary>The diagnostic identifier for AL0093.</summary>
    private const string DiagnosticId = "AL0093";

    private static readonly string[] s_oTelSetupMethods = [
        "AddOpenTelemetry",
        "UseOpenTelemetry",
        "ConfigureOpenTelemetry"
    ];

    private static readonly string[] s_resourceConfigMethods = [
        "ConfigureResource",
        "AddResource",
        "AddService",
        "SetResourceBuilder"
    ];

    private static readonly DiagnosticDescriptor s_rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.OpenTelemetry,
        DiagnosticSeverities.Suggestion);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_rule];

    /// <summary>Registers syntax node actions to analyze OpenTelemetry configuration.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context) {
        var invocation = (InvocationExpressionSyntax)context.Node;

        var methodName = GetMethodName(invocation);
        if (methodName is null || !s_oTelSetupMethods.Contains(methodName)) {
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

        if (!s_resourceConfigMethods.Any(allInvocations.Contains)) {
            context.ReportDiagnostic(Diagnostic.Create(s_rule, GetMethodNameLocation(invocation), "service.name/service.version"));
        }
    }

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
