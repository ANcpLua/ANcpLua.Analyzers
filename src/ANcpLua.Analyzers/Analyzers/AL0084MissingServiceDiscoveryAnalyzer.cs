using System.Diagnostics.CodeAnalysis;
using ANcpLua.Analyzers.Core;

namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0084: Detects direct URLs used instead of service discovery in HttpClient configuration.
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
public sealed partial class Al0084MissingServiceDiscoveryAnalyzer : AlAnalyzer {
    private static readonly DiagnosticDescriptor Rule = CreateRule(
        DiagnosticIds.MissingServiceDiscovery,
        DiagnosticCategories.AspNetCore,
        DiagnosticSeverities.Suggestion);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    /// <summary>Registers operation actions to analyze URL assignments.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterCompilationStartAction(compilationContext => {
            var httpClientType = compilationContext.Compilation.GetTypeByMetadataName("System.Net.Http.HttpClient");
            var uriType = compilationContext.Compilation.GetTypeByMetadataName("System.Uri");

            // Skip if HttpClient is not referenced (not a web project)
            if (httpClientType is null || uriType is null) {
                return;
            }

            // Check if service discovery is configured (AddServiceDiscovery called)
            var hasServiceDiscovery = HasServiceDiscoveryConfigured(compilationContext.Compilation);

            compilationContext.RegisterOperationAction(ctx =>
                AnalyzeAssignment(ctx, httpClientType, hasServiceDiscovery),
                OperationKind.SimpleAssignment);

            compilationContext.RegisterOperationAction(ctx =>
                AnalyzeObjectCreation(ctx, uriType, hasServiceDiscovery),
                OperationKind.ObjectCreation);
        });

    private static bool HasServiceDiscoveryConfigured(Compilation compilation) {
        // Check for Microsoft.Extensions.ServiceDiscovery reference
        var serviceDiscoveryType = compilation.GetTypeByMetadataName(
            "Microsoft.Extensions.ServiceDiscovery.ServiceEndpointResolver");
        if (serviceDiscoveryType is not null) {
            return true;
        }

        // Check for Aspire reference
        var aspireType = compilation.GetTypeByMetadataName(
            "Aspire.Hosting.ApplicationModel.IResource");

        return aspireType is not null;
    }

    private static void AnalyzeAssignment(
        OperationAnalysisContext context,
        ISymbol httpClientType,
        bool hasServiceDiscovery) {
        var assignment = (ISimpleAssignmentOperation)context.Operation;

        // Check if assigning to HttpClient.BaseAddress
        if (assignment.Target is not IPropertyReferenceOperation { Property.Name: "BaseAddress" } propRef) {
            return;
        }

        if (!propRef.Property.ContainingType.IsEqualTo(httpClientType)) {
            return;
        }

        // Get the URL being assigned
        if (GetUrlFromOperation(assignment.Value) is not { } urlString) {
            return;
        }

        // Check if this looks like a hardcoded URL
        if (IsHardcodedUrl(urlString) && !IsServiceDiscoveryUrl(urlString)) {
            context.ReportDiagnostic(Diagnostic.Create(Rule, assignment.Syntax.GetLocation(), urlString));
        }
    }

    private static void AnalyzeObjectCreation(
        OperationAnalysisContext context,
        ISymbol uriType,
        bool hasServiceDiscovery) {
        var creation = (IObjectCreationOperation)context.Operation;

        if (!creation.Type.IsEqualTo(uriType)) {
            return;
        }

        // Check if this Uri is used for HttpClient configuration
        if (!IsHttpClientRelated(context.Operation, out var isDirectBaseAddressAssignment)) {
            return;
        }

        // Skip if this is a direct BaseAddress assignment - AnalyzeAssignment handles that
        if (isDirectBaseAddressAssignment) {
            return;
        }

        // Get the URL from constructor argument
        if (creation.Arguments.Length is 0) {
            return;
        }

        if (GetUrlFromOperation(creation.Arguments[0].Value) is not { } urlString) {
            return;
        }

        // Check if this looks like a hardcoded URL
        if (IsHardcodedUrl(urlString) && !IsServiceDiscoveryUrl(urlString)) {
            context.ReportDiagnostic(Diagnostic.Create(Rule, creation.Syntax.GetLocation(), urlString));
        }
    }

    [SuppressMessage("Design", "CA1055:URI-like return values should not be strings")]
    private static string? GetUrlFromOperation(IOperation? operation) {
        if (operation is null) {
            return null;
        }

        // Unwrap conversions
        var unwrapped = operation.UnwrapAllConversions();

        // Check for constant string
        if (unwrapped.ConstantValue is { HasValue: true, Value: string urlString }) {
            return urlString;
        }

        // Check for Uri constructor with string
        if (unwrapped is IObjectCreationOperation { Arguments.Length: > 0 } objCreation) {
            return GetUrlFromOperation(objCreation.Arguments[0].Value);
        }

        return null;
    }

    [SuppressMessage("Design", "CA1054:URI-like parameters should not be strings")]
    private static bool IsHardcodedUrl(string urlString) {
        // Check for URL patterns that indicate hardcoded addresses
        if (!urlString.StartsWithIgnoreCase("http://") &&
            !urlString.StartsWithIgnoreCase("https://")) {
            return false;
        }

        // URLs with port numbers or IP addresses are likely hardcoded
        if (urlString.ContainsIgnoreCase(":50") || // Common dev ports like :5000, :5001
            urlString.ContainsIgnoreCase(":80") ||
            urlString.ContainsIgnoreCase(":443") ||
            urlString.ContainsIgnoreCase("localhost") ||
            urlString.ContainsOrdinal("127.0.0.1") ||
            IsIpAddress(urlString)) {
            return true;
        }

        // URLs with specific hostnames (not service names) are hardcoded
        // Service discovery URLs typically use simple names without dots
        if (TryParseUri(urlString) is { } uri && uri.Host.ContainsOrdinal(".")) {
            return true;
        }

        return false;
    }

    [SuppressMessage("Design", "CA1054:URI-like parameters should not be strings")]
    private static bool IsServiceDiscoveryUrl(string urlString) {
        // Service discovery URLs use the http+https:// or https+http:// scheme
        if (urlString.StartsWithIgnoreCase("http+https://") ||
            urlString.StartsWithIgnoreCase("https+http://")) {
            return true;
        }

        // URLs without dots in hostname might be service names
        // But if they have an explicit non-default port, they're likely hardcoded
        if (TryParseUri(urlString) is { } uri &&
            !uri.Host.ContainsOrdinal(".") &&
            !IsLocalhost(uri.Host) &&
            !HasExplicitPort(uri)) {
            return true;
        }

        return false;
    }

    private static bool HasExplicitPort(Uri uri) {
        // Check if the URL has an explicit port that's not the default for the scheme
        if (uri.IsDefaultPort) {
            return false;
        }

        // Any non-default port means the URL is hardcoded
        return true;
    }

    private static bool IsLocalhost(string host) =>
        host.EqualsIgnoreCase("localhost") ||
        host.EqualsOrdinal("127.0.0.1");

    [SuppressMessage("Design", "CA1054:URI-like parameters should not be strings")]
    private static bool IsIpAddress(string urlString) {
        if (TryParseUri(urlString) is not { } uri) {
            return false;
        }

        // Simple check for IP address pattern in host
        var host = uri.Host;
        var parts = host.Split('.');
        if (parts.Length is 4 && parts.All(static p => byte.TryParse(p, out _))) {
            return true;
        }

        return false;
    }

    [SuppressMessage("Design", "CA1054:URI-like parameters should not be strings")]
    [SuppressMessage("Design", "CA1055:URI-like return values should not be strings")]
    private static Uri? TryParseUri(string urlString) {
        try {
            return new Uri(urlString);
        } catch {
            return null;
        }
    }

    private static bool IsHttpClientRelated(IOperation operation, out bool isDirectBaseAddressAssignment) {
        isDirectBaseAddressAssignment = false;

        // Walk up the tree to find HttpClient-related context
        var parent = operation.Parent;
        while (parent is not null) {
            switch (parent) {
                case ISimpleAssignmentOperation { Target: IPropertyReferenceOperation { Property.Name: "BaseAddress" } }:
                    // This will be caught by AnalyzeAssignment, don't double-report
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
