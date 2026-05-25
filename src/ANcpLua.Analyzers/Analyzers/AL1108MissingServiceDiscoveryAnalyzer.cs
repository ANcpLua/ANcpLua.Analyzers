
namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL1108: Detects direct URLs used instead of service discovery in HttpClient configuration.
/// </summary>
/// <remarks>
///     <para>
///         When using .NET Aspire or service discovery libraries, hardcoded URLs should be
///         replaced with service names that resolve at runtime:
///         <list type="bullet">
///             <item>Instead of: <c>new Uri("http://localhost:5001")</c></item>
///             <item>Use: <c>new Uri("http+https://apiservice")</c></item>
///         </list>
///     </para>
///     <para>
///         This enables:
///         <list type="bullet">
///             <item>Automatic endpoint discovery</item>
///             <item>Load balancing</item>
///             <item>Environment-specific configuration</item>
///         </list>
///     </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al1108MissingServiceDiscoveryAnalyzer : AlAnalyzer {
    /// <summary>The diagnostic identifier for AL1108.</summary>
    private const string DiagnosticId = "AL1108";

    private static readonly DiagnosticDescriptor s_rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.AspNetCore,
        DiagnosticSeverities.Suggestion);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_rule];

    /// <summary>Registers compilation start actions to analyze service discovery configuration.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterCompilationStartAction(compilationContext => {
            var compilation = compilationContext.Compilation;

            if (IsTestProject(compilation)) {
                return;
            }

            var httpClientType = compilation.GetTypeByMetadataName("System.Net.Http.HttpClient");
            var uriType = compilation.GetTypeByMetadataName("System.Uri");

            if (httpClientType is null || uriType is null) {
                return;
            }

            compilationContext.RegisterOperationAction(
                ctx => AnalyzeAssignment(ctx, httpClientType),
                OperationKind.SimpleAssignment);

            compilationContext.RegisterOperationAction(
                ctx => AnalyzeObjectCreation(ctx, uriType),
                OperationKind.ObjectCreation);
        });

    private static void AnalyzeAssignment(OperationAnalysisContext context, ISymbol httpClientType) {
        var assignment = (ISimpleAssignmentOperation)context.Operation;

        if (assignment.Target is not IPropertyReferenceOperation { Property.Name: "BaseAddress" } propRef) {
            return;
        }

        if (!propRef.Property.ContainingType.IsEqualTo(httpClientType)) {
            return;
        }

        if (GetUrlFromOperation(assignment.Value) is not { } url) {
            return;
        }

        if (IsHardcodedUrl(url) && !IsServiceDiscoveryUrl(url)) {
            context.ReportDiagnostic(Diagnostic.Create(s_rule, assignment.Syntax.GetLocation(), url.OriginalString));
        }
    }

    private static void AnalyzeObjectCreation(OperationAnalysisContext context, ISymbol uriType) {
        var creation = (IObjectCreationOperation)context.Operation;

        if (!creation.Type.IsEqualTo(uriType)) {
            return;
        }

        if (!IsHttpClientRelated(context.Operation, out var isDirectBaseAddressAssignment)) {
            return;
        }

        // AnalyzeAssignment handles direct BaseAddress assignments
        if (isDirectBaseAddressAssignment) {
            return;
        }

        if (creation.Arguments is not [{ Value: var firstArg }, ..]) {
            return;
        }

        if (GetUrlFromOperation(firstArg) is not { } url) {
            return;
        }

        if (IsHardcodedUrl(url) && !IsServiceDiscoveryUrl(url)) {
            context.ReportDiagnostic(Diagnostic.Create(s_rule, creation.Syntax.GetLocation(), url.OriginalString));
        }
    }

    private static Uri? GetUrlFromOperation(IOperation? operation) {
        if (operation is null) {
            return null;
        }

        var unwrapped = operation.UnwrapAllConversions();

        if (unwrapped.ConstantValue is { HasValue: true, Value: string urlString }) {
            return TryParseUri(urlString);
        }

        if (unwrapped is IObjectCreationOperation { Arguments: [{ Value: var innerArg }, ..] }) {
            return GetUrlFromOperation(innerArg);
        }

        return null;
    }

    /// <summary>RFC 6761 reserved TLDs — these never resolve in production.</summary>
    private static readonly ImmutableArray<string> s_reservedTlds =
        [".test", ".invalid", ".example", ".localhost"];

    /// <summary>RFC 2606 / 6761 reserved second-level domains.</summary>
    private static readonly ImmutableArray<string> s_reservedDomains =
        ["example.com", "example.net", "example.org"];

    // Well-known third-party API endpoints. Aspire service discovery only resolves services
    // registered in the local service registry — external SaaS APIs have no registry entry
    // and MUST be addressed by their public hostname. Flagging them as "hardcoded URLs" is a
    // false positive.
    private static readonly ImmutableArray<string> s_wellKnownExternalApis =
    [
        "api.github.com",
        "api.openai.com",
        "api.anthropic.com",
        "api.mistral.ai",
        "api.cohere.ai",
        "generativelanguage.googleapis.com",
        "login.microsoftonline.com",
        "graph.microsoft.com",
        "accounts.google.com",
        "oauth2.googleapis.com",
        "www.googleapis.com"
    ];

    private static bool IsHardcodedUrl(Uri uri) {
        if (!uri.Scheme.EqualsIgnoreCase("http") && !uri.Scheme.EqualsIgnoreCase("https")) {
            return false;
        }

        var host = uri.Host;

        if (IsReservedDomain(host)) {
            return false;
        }

        if (IsWellKnownExternalApi(host)) {
            return false;
        }

        if (IsLocalhost(host)) {
            return true;
        }

        var hostParts = host.Split('.');
        if (hostParts.Length is 4 && hostParts.All(static p => byte.TryParse(p, out _))) {
            return true;
        }

        return !uri.IsDefaultPort || host.ContainsOrdinal(".");
    }

    private static bool IsWellKnownExternalApi(string host) =>
        s_wellKnownExternalApis.Any(host.EqualsIgnoreCase);

    private static bool IsServiceDiscoveryUrl(Uri uri) {
        var scheme = uri.Scheme;

        if (scheme.EqualsIgnoreCase("http+https") || scheme.EqualsIgnoreCase("https+http")) {
            return true;
        }

        return !uri.Host.ContainsOrdinal(".") &&
               !IsLocalhost(uri.Host) &&
               uri.IsDefaultPort;
    }

    private static bool IsReservedDomain(string host) =>
        s_reservedTlds.Any(host.EndsWithIgnoreCase) ||
        s_reservedDomains.Any(d => host.EqualsIgnoreCase(d) || host.EndsWithIgnoreCase("." + d));

    private static bool IsLocalhost(string host) =>
        host.EqualsIgnoreCase("localhost") ||
        host.EqualsOrdinal("127.0.0.1");

    private static Uri? TryParseUri(string value) {
        try {
            return new Uri(value);
        } catch {
            return null;
        }
    }

    private static bool IsTestProject(Compilation compilation) =>
        compilation.ReferencedAssemblyNames.Any(static a =>
            a.Name is "xunit.core" or "xunit.v3.core"
                   or "nunit.framework"
                   or "Microsoft.VisualStudio.TestPlatform.TestFramework"
                   or "Microsoft.Testing.Framework");

    private static bool IsHttpClientRelated(IOperation operation, out bool isDirectBaseAddressAssignment) {
        isDirectBaseAddressAssignment = false;

        var parent = operation.Parent;
        while (parent is not null) {
            switch (parent) {
                case ISimpleAssignmentOperation { Target: IPropertyReferenceOperation { Property.Name: "BaseAddress" } }:
                    isDirectBaseAddressAssignment = true;
                    return true;
                case IArgumentOperation { Parameter.Name: "baseAddress" or "requestUri" or "uri" }:
                    return true;
                case IInvocationOperation invocation
                    when invocation.TargetMethod.ContainingType.Name.ContainsIgnoreCase("HttpClient"):
                    return true;
            }

            parent = parent.Parent;
        }

        return false;
    }
}
