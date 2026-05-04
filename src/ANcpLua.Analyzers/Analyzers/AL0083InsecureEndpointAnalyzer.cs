
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

    private static readonly string[] s_localhostPatterns = [
        "localhost",
        "127.0.0.1",
        "[::1]"
    ];

    private static readonly string[] s_endpointPropertyNames = [
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
    private const string DiagnosticId = "AL0083";

    private static readonly DiagnosticDescriptor s_rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.Configuration,
        DiagnosticSeverities.Suggestion);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_rule];

    /// <summary>Registers operation actions to analyze endpoint assignments and constructor arguments.</summary>
    protected override void RegisterActions(AnalysisContext context) {
        context.RegisterOperationAction(AnalyzeAssignment, OperationKind.SimpleAssignment);
        context.RegisterOperationAction(AnalyzeArgument, OperationKind.Argument);
        context.RegisterOperationAction(AnalyzeObjectCreation, OperationKind.ObjectCreation);
    }

    private static void AnalyzeAssignment(OperationAnalysisContext context) {
        var assignment = (ISimpleAssignmentOperation)context.Operation;

        if (GetPropertyName(assignment.Target) is not { } propertyName || !IsEndpointProperty(propertyName)) {
            return;
        }

        CheckForInsecureEndpoint(context, assignment.Value);
    }

    private static void AnalyzeArgument(OperationAnalysisContext context) {
        var argument = (IArgumentOperation)context.Operation;

        // AnalyzeObjectCreation handles Uri/HttpClient creation arguments
        if (argument.Parent is IObjectCreationOperation { Type.Name: "Uri" or "HttpClient" }) {
            return;
        }

        if (argument.Parameter?.Name is not { } parameterName || !IsEndpointProperty(parameterName)) {
            return;
        }

        CheckForInsecureEndpoint(context, argument.Value);
    }

    private static void AnalyzeObjectCreation(OperationAnalysisContext context) {
        var creation = (IObjectCreationOperation)context.Operation;

        if (creation.Type?.Name is not ("Uri" or "HttpClient")) {
            return;
        }

        foreach (var argument in creation.Arguments) {
            CheckForInsecureEndpoint(context, argument.Value);
        }
    }

    private static void CheckForInsecureEndpoint(OperationAnalysisContext context, IOperation operation) {
        var value = operation.UnwrapAllConversions();

        if (value.ConstantValue is { HasValue: true, Value: string endpoint } && IsInsecureEndpoint(endpoint)) {
            context.ReportDiagnostic(Diagnostic.Create(s_rule, operation.Syntax.GetLocation(), endpoint));
        }
    }

    private static string? GetPropertyName(IOperation target) =>
        target switch {
            IPropertyReferenceOperation propRef => propRef.Property.Name,
            IMemberReferenceOperation memberRef => memberRef.Member.Name,
            _ => null
        };

    private static bool IsEndpointProperty(string name) {
        foreach (var pattern in s_endpointPropertyNames) {
            if (name.ContainsIgnoreCase(pattern)) {
                return true;
            }
        }

        return false;
    }

    private static bool IsInsecureEndpoint(string endpoint) {
        if (!endpoint.StartsWithOrdinal(HttpPrefix) || endpoint.StartsWithOrdinal(HttpsPrefix)) {
            return false;
        }

        // Exclude localhost patterns (development use), but not "localhost-prod" style prefixes
        var hostPart = endpoint.Substring(HttpPrefix.Length);
        foreach (var localhost in s_localhostPatterns) {
            if (hostPart.StartsWithIgnoreCase(localhost)
                && (hostPart.Length == localhost.Length || hostPart[localhost.Length] is ':' or '/' or '?')) {
                return false;
            }
        }

        return true;
    }
}
