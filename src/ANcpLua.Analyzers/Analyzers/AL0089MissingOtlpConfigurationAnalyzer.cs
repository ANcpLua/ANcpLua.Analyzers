
namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0089: Detects OTLP exporter calls without explicit endpoint configuration.
/// </summary>
/// <remarks>
///     <para>
///         When using UseOtlpExporter() or AddOtlpExporter() without an explicit endpoint,
///         telemetry will be sent to the default localhost endpoint or fail to export
///         if no collector is running locally.
///     </para>
///     <para>
///         Methods that trigger this diagnostic (when called without endpoint configuration):
///         <list type="bullet">
///             <item>UseOtlpExporter</item>
///             <item>AddOtlpExporter</item>
///         </list>
///     </para>
///     <para>
///         Ways to satisfy the endpoint configuration requirement:
///         <list type="bullet">
///             <item>Pass options delegate with Endpoint property set</item>
///             <item>Pass explicit endpoint URI parameter</item>
///             <item>Call after Environment.SetEnvironmentVariable for OTEL_EXPORTER_OTLP_ENDPOINT</item>
///         </list>
///     </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0089MissingOtlpConfigurationAnalyzer : AlAnalyzer {
    /// <summary>The diagnostic identifier for AL0089.</summary>
    private const string DiagnosticId = "AL0089";

    private static readonly string[] s_otlpExporterMethods = [
        "UseOtlpExporter",
        "AddOtlpExporter"
    ];

    private static readonly DiagnosticDescriptor s_rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.OpenTelemetry,
        DiagnosticSeverities.Suggestion);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_rule];

    /// <summary>Registers syntax tree actions to analyze OTLP exporter configuration.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context) {
        var invocation = (InvocationExpressionSyntax)context.Node;

        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess
            || !s_otlpExporterMethods.Contains(memberAccess.Name.Identifier.Text)
            || HasExplicitEndpointConfiguration(invocation)
            || HasEnvironmentVariableSet(invocation)) {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(s_rule, invocation.GetLocation()));
    }

    private static bool HasExplicitEndpointConfiguration(InvocationExpressionSyntax invocation) {
        var arguments = invocation.ArgumentList.Arguments;

        // UseOtlpExporter(OtlpExportProtocol.Grpc, new Uri("...")) pattern
        if (arguments is [_, { Expression: ObjectCreationExpressionSyntax objCreation }, ..]
            && objCreation.Type.ToString().ContainsOrdinal("Uri")) {
            return true;
        }

        foreach (var arg in arguments) {
            if (arg.Expression is SimpleLambdaExpressionSyntax or ParenthesizedLambdaExpressionSyntax or
                AnonymousMethodExpressionSyntax
                && LambdaSetsEndpoint(arg.Expression)) {
                return true;
            }

            if (arg.NameColon?.Name.Identifier.Text.EqualsIgnoreCase("endpoint") == true) {
                return true;
            }

            if (arg.Expression is ObjectCreationExpressionSyntax creation
                && creation.Initializer?.Expressions.Any(static e =>
                    e is AssignmentExpressionSyntax assignment
                    && assignment.Left.ToString().EqualsIgnoreCase("Endpoint")) == true) {
                return true;
            }
        }

        return false;
    }

    private static bool LambdaSetsEndpoint(ExpressionSyntax lambda) {
        if (lambda switch {
            SimpleLambdaExpressionSyntax simple => simple.Body,
            ParenthesizedLambdaExpressionSyntax paren => paren.Body,
            AnonymousMethodExpressionSyntax anon => anon.Body,
            _ => null
        } is not { } body) {
            return false;
        }

        foreach (var assignment in body.DescendantNodesAndSelf().OfType<AssignmentExpressionSyntax>()) {
            var leftText = assignment.Left.ToString();
            if (leftText.EndsWithIgnoreCase(".Endpoint") || leftText.EqualsOrdinal("Endpoint")) {
                return true;
            }
        }

        return false;
    }

    private static string? GetMethodName(InvocationExpressionSyntax invocation) =>
        invocation.Expression switch {
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.Text,
            IdentifierNameSyntax identifier => identifier.Identifier.Text,
            _ => null
        };

    private static bool HasEnvironmentVariableSet(InvocationExpressionSyntax invocation) {
        if (invocation.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault() is not { } containingMethod) {
            return false;
        }

        // Only check invocations that appear before this one in the method
        foreach (var inv in containingMethod.DescendantNodes().OfType<InvocationExpressionSyntax>().TakeWhile(inv => inv != invocation)) {
            if (GetMethodName(inv) is "SetEnvironmentVariable" or "Add"
                && inv.ArgumentList.Arguments is [{ Expression: LiteralExpressionSyntax literal }, ..]
                && literal.Token.ValueText.ContainsIgnoreCase("OTEL_EXPORTER_OTLP_ENDPOINT")) {
                return true;
            }
        }

        return false;
    }
}
