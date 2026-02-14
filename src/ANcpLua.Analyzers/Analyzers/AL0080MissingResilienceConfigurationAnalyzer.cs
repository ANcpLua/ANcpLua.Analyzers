using ANcpLua.Analyzers.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using System.Collections.Immutable;

namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0080: Detects HTTP clients registered without resilience policies.
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
public sealed partial class Al0080MissingResilienceConfigurationAnalyzer : AlAnalyzer {
    /// <summary>The diagnostic identifier for AL0080.</summary>
    public const string DiagnosticId = "AL0080";

    private static readonly DiagnosticDescriptor Rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.AspNetCore,
        DiagnosticSeverity.Warning);

    /// <summary>Gets the diagnostic descriptor for AL0080.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    /// <summary>Registers operation actions to analyze HTTP client registrations.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);

    private static void AnalyzeInvocation(OperationAnalysisContext context) {
        if (context.Operation is not IInvocationOperation invocation) {
            return;
        }

        // Check if this is an AddHttpClient call
        if (!IsAddHttpClientCall(invocation)) {
            return;
        }

        // Get the containing method body and check for resilience configuration
        if (GetContainingMethodBody(invocation) is not { } containingMethod
            || HasResilienceConfiguration(containingMethod)) {
            return;
        }

        // Extract HTTP client name for the diagnostic message
        var clientName = GetHttpClientName(invocation);

        context.ReportDiagnostic(Diagnostic.Create(
            Rule,
            invocation.Syntax.GetLocation(),
            clientName));
    }

    private static bool IsAddHttpClientCall(IInvocationOperation invocation) {
        var methodName = invocation.TargetMethod.Name;
        if (methodName is not "AddHttpClient") {
            return false;
        }

        // Check if it's an extension method on IServiceCollection
        var containingType = invocation.TargetMethod.ContainingType?.ToDisplayString();
        return containingType is "Microsoft.Extensions.DependencyInjection.HttpClientFactoryServiceCollectionExtensions"
            or "Microsoft.Extensions.DependencyInjection.HttpClientBuilderExtensions";
    }

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
        // Try to get the client name from the type argument
        if (invocation.TargetMethod.TypeArguments is [{ } typeArg]) {
            return typeArg.Name;
        }

        // Try to get the client name from the first string argument
        foreach (var arg in invocation.Arguments) {
            if (arg.Value is ILiteralOperation { ConstantValue: { HasValue: true, Value: string clientName } }) {
                return clientName;
            }
        }

        return "unnamed";
    }
}
