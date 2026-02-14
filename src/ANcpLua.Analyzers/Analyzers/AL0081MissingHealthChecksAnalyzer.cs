using ANcpLua.Analyzers.Core;

namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0081: Detects web applications that don't configure health checks.
/// </summary>
/// <remarks>
///     <para>
///         Health checks are essential for container orchestration platforms to determine
///         service availability. This analyzer detects when:
///         <list type="bullet">
///             <item>WebApplication.CreateBuilder is called without AddHealthChecks()</item>
///             <item>Host.CreateDefaultBuilder is called without AddHealthChecks()</item>
///         </list>
///     </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0081MissingHealthChecksAnalyzer : AlAnalyzer {
    /// <summary>The diagnostic identifier for AL0081.</summary>
    public const string DiagnosticId = "AL0081";

    private static readonly DiagnosticDescriptor Rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.AspNetCore,
        DiagnosticSeverities.Suggestion);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    /// <summary>Registers syntax tree actions to analyze web application configuration.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context) {
        var invocation = (InvocationExpressionSyntax)context.Node;

        // Look for WebApplication.CreateBuilder or Host.CreateDefaultBuilder calls
        var methodName = GetMethodName(invocation);
        if (methodName is not ("CreateBuilder" or "CreateDefaultBuilder")) {
            return;
        }

        // Check if this looks like a web application builder pattern
        var receiverName = GetReceiverName(invocation);
        if (receiverName is not ("WebApplication" or "Host")) {
            return;
        }

        // Find the containing method (likely Main or configuration method)
        if (invocation.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault() is not { } containingMethod) {
            // Also check for top-level statements (no method, but compilation unit)
            if (invocation.Ancestors().OfType<CompilationUnitSyntax>().FirstOrDefault() is not { } compilationUnit) {
                return;
            }

            // Search in top-level statements
            if (!HasHealthChecksConfigured(compilationUnit, context.SemanticModel)) {
                context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.GetLocation()));
            }

            return;
        }

        // Collect all method invocations in the same method
        if (!HasHealthChecksConfigured(containingMethod, context.SemanticModel)) {
            context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.GetLocation()));
        }
    }

    private static bool HasHealthChecksConfigured(SyntaxNode scope, SemanticModel semanticModel) {
        var allInvocations = new HashSet<string>();

        foreach (var inv in scope.DescendantNodes().OfType<InvocationExpressionSyntax>()) {
            var name = GetMethodName(inv);
            if (name is not null) {
                allInvocations.Add(name);
            }
        }

        // Check for AddHealthChecks call
        return allInvocations.Contains("AddHealthChecks");
    }

    private static string? GetMethodName(InvocationExpressionSyntax invocation) =>
        invocation.Expression switch {
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.Text,
            IdentifierNameSyntax identifier => identifier.Identifier.Text,
            _ => null
        };

    private static string? GetReceiverName(InvocationExpressionSyntax invocation) =>
        invocation.Expression switch {
            MemberAccessExpressionSyntax memberAccess => memberAccess.Expression switch {
                IdentifierNameSyntax identifier => identifier.Identifier.Text,
                _ => null
            },
            _ => null
        };
}
