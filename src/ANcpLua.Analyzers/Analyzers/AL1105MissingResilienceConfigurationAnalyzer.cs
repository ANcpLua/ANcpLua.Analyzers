
namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL1105: Detects HTTP clients registered without resilience policies.
///     Reports when AddHttpClient is called without corresponding AddStandardResilienceHandler,
///     AddResilienceHandler, or Polly configuration.
/// </summary>
/// <remarks>
///     <para>
///         HTTP clients in production should be configured with resilience policies to handle
///         transient failures gracefully. This analyzer detects when AddHttpClient is called
///         without a corresponding resilience configuration in the same method body.
///     </para>
///     <para>
///         The following patterns are considered valid resilience configurations:
///         <list type="bullet">
///             <item><c>AddStandardResilienceHandler()</c> - Microsoft.Extensions.Http.Resilience standard handler</item>
///             <item><c>AddResilienceHandler()</c> - Custom resilience handler</item>
///             <item><c>AddTransientHttpErrorPolicy()</c> - Polly transient error policy</item>
///             <item><c>AddPolicyHandler()</c> - Polly policy handler</item>
///         </list>
///     </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al1105MissingResilienceConfigurationAnalyzer : AlAnalyzer {
    /// <summary>The diagnostic identifier for AL1105.</summary>
    private const string DiagnosticId = "AL1105";

    private static readonly DiagnosticDescriptor s_rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.AspNetCore,
        DiagnosticSeverity.Warning);

    /// <summary>Gets the diagnostic descriptor for AL1105.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_rule];

    /// <summary>Registers operation actions to analyze HTTP client registrations.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);

    private static void AnalyzeInvocation(OperationAnalysisContext context) {
        if (context.Operation is not IInvocationOperation invocation
            || !IsAddHttpClientCall(invocation)
            || GetContainingMethodBody(invocation) is not { } containingMethod
            || HasResilienceConfiguration(containingMethod)) {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            s_rule,
            invocation.Syntax.GetLocation(),
            GetHttpClientName(invocation)));
    }

    private static bool IsAddHttpClientCall(IInvocationOperation invocation) =>
        invocation.TargetMethod is { Name: "AddHttpClient", ContainingType: { } containingType }
        && containingType.ToDisplayString() is "Microsoft.Extensions.DependencyInjection.HttpClientFactoryServiceCollectionExtensions"
            or "Microsoft.Extensions.DependencyInjection.HttpClientBuilderExtensions";

    private static IOperation? GetContainingMethodBody(IOperation operation) {
        var current = operation.Parent;
        while (current is not null) {
            if (current is IMethodBodyOperation or IBlockOperation { Parent: IMethodBodyOperation or null }) {
                return current;
            }

            current = current.Parent;
        }

        return null;
    }

    private static bool HasResilienceConfiguration(IOperation methodBody) {
        foreach (var descendant in Microsoft.CodeAnalysis.Operations.OperationExtensions.Descendants(methodBody)) {
            if (descendant is not IInvocationOperation invocation) {
                continue;
            }

            var methodName = invocation.TargetMethod.Name;
            if (methodName is "AddStandardResilienceHandler"
                or "AddResilienceHandler"
                or "AddTransientHttpErrorPolicy"
                or "AddPolicyHandler"
                or "AddStandardHedgingHandler") {
                return true;
            }
        }

        return false;
    }

    private static string GetHttpClientName(IInvocationOperation invocation) {
        if (invocation.TargetMethod.TypeArguments is [{ } typeArg]) {
            return typeArg.Name;
        }

        foreach (var arg in invocation.Arguments) {
            if (arg.Value is ILiteralOperation { ConstantValue: { HasValue: true, Value: string clientName } }) {
                return clientName;
            }
        }

        return "unnamed";
    }
}
