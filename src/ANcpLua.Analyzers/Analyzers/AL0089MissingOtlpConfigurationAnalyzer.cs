using ANcpLua.Analyzers.Core;
using ANcpLua.Roslyn.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

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
    public const string DiagnosticId = "AL0089";

    private static readonly string[] OtlpExporterMethods = [
        "UseOtlpExporter",
        "AddOtlpExporter"
    ];

    private static readonly DiagnosticDescriptor Rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.OpenTelemetry,
        DiagnosticSeverities.Suggestion);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    /// <summary>Registers syntax tree actions to analyze OTLP exporter configuration.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context) {
        var invocation = (InvocationExpressionSyntax)context.Node;

        // Look for OTLP exporter calls
        var methodName = GetMethodName(invocation);
        if (!IsOtlpExporterMethod(methodName)) {
            return;
        }

        // Must be called as extension method (builder.UseOtlpExporter())
        if (invocation.Expression is not MemberAccessExpressionSyntax) {
            return;
        }

        // Check if endpoint is explicitly configured
        if (HasExplicitEndpointConfiguration(invocation)) {
            return;
        }

        // Check if OTEL_EXPORTER_OTLP_ENDPOINT is set in the same method
        if (HasEnvironmentVariableSet(invocation)) {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            Rule,
            invocation.GetLocation()));
    }

    private static bool IsOtlpExporterMethod(string? methodName) =>
        methodName is not null && OtlpExporterMethods.Contains(methodName);

    private static string? GetMethodName(InvocationExpressionSyntax invocation) =>
        invocation.Expression switch {
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.Text,
            IdentifierNameSyntax identifier => identifier.Identifier.Text,
            _ => null
        };

    private static bool HasExplicitEndpointConfiguration(InvocationExpressionSyntax invocation) {
        var arguments = invocation.ArgumentList.Arguments;

        // Check for URI parameter (first positional argument that is a Uri or string containing endpoint)
        foreach (var arg in arguments) {
            // Check for lambda/delegate configuring options
            if (arg.Expression is SimpleLambdaExpressionSyntax or ParenthesizedLambdaExpressionSyntax or
                AnonymousMethodExpressionSyntax) {
                // Check if the lambda sets Endpoint property
                if (LambdaSetsEndpoint(arg.Expression)) {
                    return true;
                }
            }

            // Check for named argument "endpoint" or "configure" with endpoint configuration
            if (arg.NameColon?.Name.Identifier.Text.EqualsIgnoreCase("endpoint") == true) {
                return true;
            }

            // Check for OtlpExportProtocol enum followed by Uri parameter pattern
            // UseOtlpExporter(OtlpExportProtocol.Grpc, new Uri("http://localhost:4317"))
            if (arguments.Count >= 2) {
                var secondArg = arguments.Count > 1 ? arguments[1] : null;
                if (secondArg?.Expression is ObjectCreationExpressionSyntax objCreation &&
                    objCreation.Type.ToString().ContainsOrdinal("Uri")) {
                    return true;
                }
            }

            // Check for inline object creation with endpoint
            if (arg.Expression is ObjectCreationExpressionSyntax creation &&
                creation.Initializer?.Expressions.Any(static e =>
                    e is AssignmentExpressionSyntax assignment &&
                    assignment.Left.ToString().EqualsIgnoreCase("Endpoint")) == true) {
                return true;
            }
        }

        return false;
    }

    private static bool LambdaSetsEndpoint(ExpressionSyntax lambda) {
        // Look for "options.Endpoint = " or "o.Endpoint = " patterns in the lambda body
        if (lambda switch {
            SimpleLambdaExpressionSyntax simple => simple.Body,
            ParenthesizedLambdaExpressionSyntax paren => paren.Body,
            AnonymousMethodExpressionSyntax anon => anon.Body,
            _ => null
        } is not { } body) {
            return false;
        }

        // Check for Endpoint assignment in the lambda body
        var assignments = body.DescendantNodesAndSelf()
            .OfType<AssignmentExpressionSyntax>();

        foreach (var assignment in assignments) {
            var leftText = assignment.Left.ToString();
            if (leftText.EndsWithIgnoreCase(".Endpoint") ||
                leftText.EqualsOrdinal("Endpoint")) {
                return true;
            }
        }

        return false;
    }

    private static bool HasEnvironmentVariableSet(InvocationExpressionSyntax invocation) {
        // Check if OTEL_EXPORTER_OTLP_ENDPOINT is set earlier in the same method
        if (invocation.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault() is not { } containingMethod) {
            return false;
        }

        // Look for Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", ...)
        var invocations = containingMethod.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .TakeWhile(inv => inv != invocation); // Only look at invocations before this one

        foreach (var inv in invocations) {
            var name = GetMethodName(inv);
            if (name is "SetEnvironmentVariable" or "Add") {
                var args = inv.ArgumentList.Arguments;
                if (args.Count >= 1 &&
                    args[0].Expression is LiteralExpressionSyntax literal &&
                    literal.Token.ValueText.ContainsIgnoreCase("OTEL_EXPORTER_OTLP_ENDPOINT")) {
                    return true;
                }
            }
        }

        return false;
    }
}
