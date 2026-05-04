
namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0069: Detects incomplete ServiceDefaults configuration.
/// </summary>
/// <remarks>
///     <para>
///         Complete ServiceDefaults configuration should include:
///         <list type="bullet">
///             <item>Tracing configuration (AddTracing/WithTracing)</item>
///             <item>Metrics configuration (AddMetrics/WithMetrics)</item>
///             <item>Logging configuration (AddLogging/WithLogging)</item>
///         </list>
///     </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0069IncompleteServiceDefaultsAnalyzer : AlAnalyzer {
    /// <summary>The diagnostic identifier for AL0069.</summary>
    private const string DiagnosticId = "AL0069";

    private static readonly string[] s_tracingMethods = ["AddOpenTelemetry", "WithTracing", "AddTracing"];
    private static readonly string[] s_metricsMethods = ["AddOpenTelemetry", "WithMetrics", "AddMetrics"];

    private static readonly DiagnosticDescriptor s_rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.Configuration,
        DiagnosticSeverities.Suggestion);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_rule];

    /// <summary>Registers syntax tree actions to analyze ServiceDefaults configuration.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context) {
        var invocation = (InvocationExpressionSyntax)context.Node;

        // Look for ConfigureOpenTelemetry or AddServiceDefaults calls
        var methodName = GetMethodName(invocation);
        if (methodName is not ("ConfigureOpenTelemetry" or "AddServiceDefaults" or "AddOpenTelemetry")) {
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

        // Check for tracing configuration
        var hasTracing = s_tracingMethods.Any(allInvocations.Contains);
        var hasMetrics = s_metricsMethods.Any(allInvocations.Contains);

        // Report missing components
        if (!hasTracing) {
            context.ReportDiagnostic(Diagnostic.Create(
                s_rule,
                invocation.GetLocation(),
                "tracing",
                "WithTracing() or AddTracing()"));
        }

        if (!hasMetrics) {
            context.ReportDiagnostic(Diagnostic.Create(
                s_rule,
                invocation.GetLocation(),
                "metrics",
                "WithMetrics() or AddMetrics()"));
        }

        // Note: Logging is optional, so we don't report it as missing
    }

    private static string? GetMethodName(InvocationExpressionSyntax invocation) =>
        invocation.Expression switch {
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.Text,
            IdentifierNameSyntax identifier => identifier.Identifier.Text,
            _ => null
        };
}
