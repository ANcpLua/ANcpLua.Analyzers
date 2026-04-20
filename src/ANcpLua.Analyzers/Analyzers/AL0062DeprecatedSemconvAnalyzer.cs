
namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0062: Detects usage of deprecated OpenTelemetry semantic convention attributes.
/// </summary>
/// <remarks>
///     <para>
///         Some semantic convention attribute names have been deprecated and replaced
///         with newer names. This analyzer helps migrate to the current conventions.
///     </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0062DeprecatedSemconvAnalyzer : AlAnalyzer {
    /// <summary>The diagnostic identifier for AL0062.</summary>
    private const string DiagnosticId = "AL0062";

    // Map of deprecated attribute names to their replacements and deprecation version
    private static readonly Dictionary<string, (string Replacement, string Version)> DeprecatedAttributes =
        new(StringComparer.OrdinalIgnoreCase) {
            // HTTP semantic conventions (1.21.0 -> 1.23.0)
            ["http.method"] = ("http.request.method", "1.21.0"),
            ["http.url"] = ("url.full", "1.21.0"),
            ["http.target"] = ("url.path and url.query", "1.21.0"),
            ["http.host"] = ("server.address and server.port", "1.21.0"),
            ["http.scheme"] = ("url.scheme", "1.21.0"),
            ["http.server_name"] = ("server.address", "1.21.0"),
            ["http.status_code"] = ("http.response.status_code", "1.21.0"),
            ["http.request_content_length"] = ("http.request.header.content-length", "1.21.0"),
            ["http.request_content_length_uncompressed"] = ("http.request.body.size", "1.21.0"),
            ["http.response_content_length"] = ("http.response.header.content-length", "1.21.0"),
            ["http.response_content_length_uncompressed"] = ("http.response.body.size", "1.21.0"),
            ["http.flavor"] = ("network.protocol.version", "1.21.0"),
            ["http.user_agent"] = ("user_agent.original", "1.21.0"),
            ["http.client_ip"] = ("client.address", "1.21.0"),
            // Network semantic conventions
            ["net.peer.name"] = ("server.address", "1.21.0"),
            ["net.peer.port"] = ("server.port", "1.21.0"),
            ["net.host.name"] = ("server.address", "1.21.0"),
            ["net.host.port"] = ("server.port", "1.21.0"),
            ["net.protocol.name"] = ("network.protocol.name", "1.21.0"),
            ["net.protocol.version"] = ("network.protocol.version", "1.21.0"),
            ["net.transport"] = ("network.transport", "1.21.0"),

            // Database semantic conventions
            ["db.name"] = ("db.namespace", "1.25.0"),
            ["db.statement"] = ("db.query.text", "1.25.0"),
            ["db.operation"] = ("db.operation.name", "1.25.0"),
            ["db.system"] = ("db.system.name", "1.30.0"),
            ["db.sql.table"] = ("db.collection.name", "1.25.0"),
            ["db.mongodb.collection"] = ("db.collection.name", "1.25.0"),
            ["db.cosmosdb.container"] = ("db.collection.name", "1.25.0"),
            ["db.cassandra.table"] = ("db.collection.name", "1.25.0"),
            ["db.client.connections.pool.name"] = ("db.client.connection.pool.name", "1.27.0"),
            ["db.client.connections.state"] = ("db.client.connection.state", "1.27.0"),

            // Messaging semantic conventions
            ["messaging.client_id"] = ("messaging.client.id", "1.26.0"),
            ["messaging.kafka.message.offset"] = ("messaging.kafka.offset", "1.27.0"),
            ["messaging.kafka.destination.partition"] = ("messaging.destination.partition.id", "1.25.0"),
            ["messaging.operation"] = ("messaging.operation.type", "1.25.0"),

            // RPC semantic conventions
            ["rpc.connect_rpc.error_code"] = ("rpc.response.status_code", "1.39.0"),
            ["rpc.jsonrpc.request_id"] = ("jsonrpc.request.id", "1.39.0"),
            ["rpc.jsonrpc.version"] = ("jsonrpc.protocol.version", "1.39.0"),
            ["rpc.system"] = ("rpc.system.name", "1.39.0")
        };

    private static readonly DiagnosticDescriptor Rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.OpenTelemetry,
        DiagnosticSeverities.Suggestion);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    internal static bool TryGetDeprecatedAttribute(string attributeName, out (string Replacement, string Version) info) =>
        DeprecatedAttributes.TryGetValue(attributeName, out info);

    /// <summary>Registers operation actions to analyze SetTag calls.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);

    private static void AnalyzeInvocation(OperationAnalysisContext context) {
        var invocation = (IInvocationOperation)context.Operation;

        if (invocation.TargetMethod.Name is not ("SetTag" or "AddTag" or "SetAttribute" or "Add") ||
            invocation.Arguments.Length is 0 ||
            !invocation.Arguments[0].Value.TryGetConstantValue(out string? attributeName) ||
            attributeName is null ||
            !TryGetDeprecatedAttribute(attributeName, out var info)) {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            Rule,
            invocation.Arguments[0].Syntax.GetLocation(),
            attributeName,
            info.Version,
            info.Replacement));
    }
}
