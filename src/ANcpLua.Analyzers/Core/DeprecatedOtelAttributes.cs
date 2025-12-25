namespace ANcpLua.Analyzers.Core;

/// <summary>
///     Contains mappings of deprecated OpenTelemetry semantic convention attributes
///     to their modern replacements.
/// </summary>
public static class DeprecatedOtelAttributes
{
    /// <summary>
    ///     Deprecated attribute names mapped to replacements.
    ///     Based on OTel schema 1.38.0
    /// </summary>
    public static readonly IReadOnlyDictionary<string, (string Replacement, string Version)> Renames =
        new Dictionary<string, (string, string)>
        {
            // GenAI
            ["gen_ai.system"] = ("gen_ai.provider.name", "1.37.0"),
            ["gen_ai.usage.prompt_tokens"] = ("gen_ai.usage.input_tokens", "1.27.0"),
            ["gen_ai.usage.completion_tokens"] = ("gen_ai.usage.output_tokens", "1.27.0"),

            // HTTP
            ["http.method"] = ("http.request.method", "1.21.0"),
            ["http.status_code"] = ("http.response.status_code", "1.21.0"),
            ["http.url"] = ("url.full", "1.21.0"),
            ["http.scheme"] = ("url.scheme", "1.21.0"),
            ["http.request_content_length"] = ("http.request.body.size", "1.21.0"),
            ["http.response_content_length"] = ("http.response.body.size", "1.21.0"),
            ["http.client_ip"] = ("client.address", "1.21.0"),
            ["http.user_agent"] = ("user_agent.original", "1.19.0"),

            // Network
            ["net.host.name"] = ("server.address", "1.21.0"),
            ["net.host.port"] = ("server.port", "1.21.0"),
            ["net.sock.host.addr"] = ("server.socket.address", "1.21.0"),
            ["net.sock.host.port"] = ("server.socket.port", "1.21.0"),
            ["net.protocol.name"] = ("network.protocol.name", "1.21.0"),
            ["net.protocol.version"] = ("network.protocol.version", "1.21.0"),

            // Database
            ["db.statement"] = ("db.query.text", "1.25.0"),
            ["db.operation"] = ("db.operation.name", "1.25.0"),
            ["db.name"] = ("db.namespace", "1.25.0"),

            // Code
            ["code.filepath"] = ("code.file.path", "1.30.0"),
            ["code.function"] = ("code.function.name", "1.30.0"),
            ["code.lineno"] = ("code.line.number", "1.30.0"),
            ["code.column"] = ("code.column.number", "1.30.0"),

            // FaaS
            ["faas.execution"] = ("faas.invocation_id", "1.19.0"),
            ["faas.id"] = ("cloud.resource_id", "1.19.0"),

            // Messaging
            ["messaging.kafka.client_id"] = ("messaging.client_id", "1.21.0"),
            ["messaging.rocketmq.client_id"] = ("messaging.client_id", "1.21.0")
        };

    /// <summary>
    ///     Known attribute key patterns used in OpenTelemetry APIs.
    /// </summary>
    public static readonly HashSet<string> AttributeKeyPatterns = new(StringComparer.OrdinalIgnoreCase)
    {
        "SetAttribute",
        "AddTag",
        "SetTag",
        "attributes",
        "Tags"
    };
}