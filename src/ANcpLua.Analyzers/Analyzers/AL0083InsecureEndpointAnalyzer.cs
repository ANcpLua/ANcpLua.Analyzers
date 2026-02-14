using ANcpLua.Analyzers.Core;

namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0083: Detects HTTP endpoints used where HTTPS is expected.
/// </summary>
/// <remarks>
///     <para>
///         HTTP endpoints expose sensitive data in transit to interception and
///         man-in-the-middle attacks. This analyzer flags:
///         <list type="bullet">
///             <item>String literals starting with "http://" (not "https://")</item>
///             <item>Used in HttpClient, OTLP exporter, API client configurations</item>
///         </list>
///     </para>
///     <para>
///         The following are excluded (development use):
///         <list type="bullet">
///             <item>localhost (e.g., http://localhost:5000)</item>
///             <item>127.0.0.1 (e.g., http://127.0.0.1:5000)</item>
///         </list>
///     </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0083InsecureEndpointAnalyzer : AlAnalyzer {
    private const string HttpPrefix = "http://";
    private const string HttpsPrefix = "https://";

    private static readonly string[] LocalhostPatterns = [
        "localhost",
        "127.0.0.1",
        "[::1]"
    ];

    private static readonly string[] EndpointPropertyNames = [
        "Endpoint",
        "BaseAddress",
        "Url",
        "Uri",
        "Address",
        "Host",
        "CollectorEndpoint",
        "OtlpEndpoint",
        "ExporterEndpoint",
        "ServiceUrl",
        "ApiUrl",
        "ServerUrl"
    ];

    /// <summary>The diagnostic identifier for AL0083.</summary>
    public const string DiagnosticId = "AL0083";

    private static readonly DiagnosticDescriptor Rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.Configuration,
        DiagnosticSeverities.Suggestion);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    /// <summary>Registers operation actions to analyze endpoint assignments and constructor arguments.</summary>
    protected override void RegisterActions(AnalysisContext context) {
        context.RegisterOperationAction(AnalyzeAssignment, OperationKind.SimpleAssignment);
        context.RegisterOperationAction(AnalyzeArgument, OperationKind.Argument);
        context.RegisterOperationAction(AnalyzeObjectCreation, OperationKind.ObjectCreation);
    }

    private static void AnalyzeAssignment(OperationAnalysisContext context) {
        var assignment = (ISimpleAssignmentOperation)context.Operation;

        // Check if this is an endpoint property assignment
        var propertyName = GetPropertyName(assignment.Target);
        if (propertyName is null || !IsEndpointProperty(propertyName)) {
            return;
        }

        CheckForInsecureEndpoint(context, assignment.Value);
    }

    private static void AnalyzeArgument(OperationAnalysisContext context) {
        var argument = (IArgumentOperation)context.Operation;

        // Skip if parent is Uri/HttpClient object creation (handled by AnalyzeObjectCreation)
        if (argument.Parent is IObjectCreationOperation creation &&
            creation.Type?.Name is "Uri" or "HttpClient") {
            return;
        }

        // Check if parameter name suggests endpoint usage
        var parameterName = argument.Parameter?.Name;
        if (parameterName is null || !IsEndpointProperty(parameterName)) {
            return;
        }

        CheckForInsecureEndpoint(context, argument.Value);
    }

    private static void AnalyzeObjectCreation(OperationAnalysisContext context) {
        var creation = (IObjectCreationOperation)context.Operation;

        // Check for Uri/HttpClient creation with http:// string
        var typeName = creation.Type?.Name;
        if (typeName is not ("Uri" or "HttpClient")) {
            return;
        }

        foreach (var argument in creation.Arguments) {
            CheckForInsecureEndpoint(context, argument.Value);
        }
    }

    private static void CheckForInsecureEndpoint(OperationAnalysisContext context, IOperation operation) {
        // Unwrap conversions to get the actual value
        var value = operation.UnwrapAllConversions();

        if (value.ConstantValue is not { HasValue: true, Value: string endpoint }) {
            return;
        }

        if (IsInsecureEndpoint(endpoint)) {
            context.ReportDiagnostic(Diagnostic.Create(
                Rule,
                operation.Syntax.GetLocation(),
                endpoint));
        }
    }

    private static string? GetPropertyName(IOperation target) =>
        target switch {
            IPropertyReferenceOperation propRef => propRef.Property.Name,
            IMemberReferenceOperation memberRef => memberRef.Member.Name,
            _ => null
        };

    private static bool IsEndpointProperty(string name) {
        foreach (var pattern in EndpointPropertyNames) {
            if (name.ContainsIgnoreCase(pattern)) {
                return true;
            }
        }

        return false;
    }

    private static bool IsInsecureEndpoint(string endpoint) {
        // Must start with http:// (not https://)
        if (!endpoint.StartsWithOrdinal(HttpPrefix)) {
            return false;
        }

        // Exclude https:// (defensive, but shouldn't match above)
        if (endpoint.StartsWithOrdinal(HttpsPrefix)) {
            return false;
        }

        // Exclude localhost patterns (development use)
        var hostPart = endpoint.Substring(HttpPrefix.Length);
        foreach (var localhost in LocalhostPatterns) {
            if (hostPart.StartsWithIgnoreCase(localhost)) {
                // Check it's not just a prefix (e.g., "localhost" not "localhost-prod")
                if (hostPart.Length == localhost.Length ||
                    hostPart[localhost.Length] is ':' or '/' or '?') {
                    return false;
                }
            }
        }

        return true;
    }
}
