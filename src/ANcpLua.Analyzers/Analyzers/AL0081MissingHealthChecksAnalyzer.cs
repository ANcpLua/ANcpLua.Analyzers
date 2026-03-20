
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

        if (GetMethodName(invocation) is not ("CreateBuilder" or "CreateDefaultBuilder")
            || GetReceiverName(invocation) is not ("WebApplication" or "Host")) {
            return;
        }

        // Test code creates WebApplication for endpoint registration verification, not production hosting
        if (IsInsideTestMethod(invocation)) {
            return;
        }

        if (invocation.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault() is not { } containingMethod) {
            if (invocation.Ancestors().OfType<CompilationUnitSyntax>().FirstOrDefault() is { } compilationUnit
                && !HasHealthChecksConfigured(compilationUnit)) {
                context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.GetLocation()));
            }

            return;
        }

        if (!HasHealthChecksConfigured(containingMethod)) {
            context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.GetLocation()));
        }
    }

    private static bool HasHealthChecksConfigured(SyntaxNode scope) {
        foreach (var inv in scope.DescendantNodes().OfType<InvocationExpressionSyntax>()) {
            if (GetMethodName(inv) is "AddHealthChecks") {
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

    private static string? GetReceiverName(InvocationExpressionSyntax invocation) =>
        invocation.Expression switch {
            MemberAccessExpressionSyntax memberAccess => memberAccess.Expression switch {
                IdentifierNameSyntax identifier => identifier.Identifier.Text,
                _ => null
            },
            _ => null
        };

    private static bool IsInsideTestMethod(SyntaxNode node) {
        foreach (var ancestor in node.Ancestors()) {
            if (ancestor is not MethodDeclarationSyntax method) {
                continue;
            }

            foreach (var attrList in method.AttributeLists) {
                foreach (var attr in attrList.Attributes) {
                    var name = attr.Name switch {
                        IdentifierNameSyntax id => id.Identifier.Text,
                        QualifiedNameSyntax qualified => qualified.Right.Identifier.Text,
                        _ => null
                    };

                    if (name is "Test" or "TestMethod" or "Fact" or "Theory"
                        or "TestAttribute" or "TestMethodAttribute" or "FactAttribute" or "TheoryAttribute") {
                        return true;
                    }
                }
            }
        }

        return false;
    }
}
