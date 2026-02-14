using ANcpLua.Analyzers.Core;

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
    private static readonly string[] ServiceDefaultsMethods = [
        "AddServiceDefaults",
        "AddQylServiceDefaults",
        "ConfigureOpenTelemetry"
    ];

    private static readonly string[] OTelConfigurationMethods = [
        "AddOpenTelemetry",
        "WithTracing",
        "UseOpenTelemetry"
    ];

    /// <summary>The diagnostic identifier for AL0076.</summary>
    public const string DiagnosticId = "AL0076";

    private static readonly DiagnosticDescriptor Rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.OpenTelemetry,
        DiagnosticSeverities.Suggestion);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    /// <summary>Registers syntax tree actions to analyze OpenTelemetry configuration.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context) {
        var invocation = (InvocationExpressionSyntax)context.Node;

        // Look for ServiceDefaults-type calls
        var methodName = GetMethodName(invocation);
        if (!IsServiceDefaultsMethod(methodName)) {
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

        // Check if any OTel configuration method is present
        var hasOTelConfiguration = OTelConfigurationMethods.Any(allInvocations.Contains);

        if (!hasOTelConfiguration) {
            context.ReportDiagnostic(Diagnostic.Create(
                Rule,
                invocation.GetLocation()));
        }
    }

    private static bool IsServiceDefaultsMethod(string? methodName) =>
        methodName is not null && ServiceDefaultsMethods.Contains(methodName);

    private static string? GetMethodName(InvocationExpressionSyntax invocation) =>
        invocation.Expression switch {
            // Only match member access (e.g., services.AddServiceDefaults())
            // Skip IdentifierNameSyntax to avoid matching local methods with same name
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.Text,
            _ => null
        };
}
