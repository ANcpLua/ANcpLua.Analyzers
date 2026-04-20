namespace ANcpLua.Analyzers.Analyzers;

internal static class OpenTelemetryDeprecatedSemconvCatalog {
    private static readonly Dictionary<string, (string Replacement, string Version)> DeprecatedAttributes =
        new(StringComparer.OrdinalIgnoreCase) {
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
            ["net.peer.name"] = ("server.address", "1.21.0"),
            ["net.peer.port"] = ("server.port", "1.21.0"),
            ["net.host.name"] = ("server.address", "1.21.0"),
            ["net.host.port"] = ("server.port", "1.21.0"),
            ["net.protocol.name"] = ("network.protocol.name", "1.21.0"),
            ["net.protocol.version"] = ("network.protocol.version", "1.21.0"),
            ["net.transport"] = ("network.transport", "1.21.0"),
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
            ["messaging.client_id"] = ("messaging.client.id", "1.26.0"),
            ["messaging.kafka.message.offset"] = ("messaging.kafka.offset", "1.27.0"),
            ["messaging.kafka.destination.partition"] = ("messaging.destination.partition.id", "1.25.0"),
            ["messaging.operation"] = ("messaging.operation.type", "1.25.0"),
            ["rpc.connect_rpc.error_code"] = ("rpc.response.status_code", "1.39.0"),
            ["rpc.jsonrpc.request_id"] = ("jsonrpc.request.id", "1.39.0"),
            ["rpc.jsonrpc.version"] = ("jsonrpc.protocol.version", "1.39.0"),
            ["rpc.system"] = ("rpc.system.name", "1.39.0")
        };

    private static readonly Dictionary<string, string> DeprecatedGenAiAttributes = new(StringComparer.OrdinalIgnoreCase) {
        ["gen_ai.system"] = "gen_ai.provider.name",
        ["gen_ai.usage.prompt_tokens"] = "gen_ai.usage.input_tokens",
        ["gen_ai.usage.completion_tokens"] = "gen_ai.usage.output_tokens",
        ["gen_ai.openai.request.seed"] = "gen_ai.request.seed",
        ["gen_ai.openai.request.response_format"] = "gen_ai.output.type",
        ["gen_ai.openai.request.service_tier"] = "openai.request.service_tier",
        ["gen_ai.openai.response.service_tier"] = "openai.response.service_tier",
        ["gen_ai.openai.response.system_fingerprint"] = "openai.response.system_fingerprint",
        ["gen_ai.prompt.tokens"] = "gen_ai.usage.input_tokens",
        ["gen_ai.completion.tokens"] = "gen_ai.usage.output_tokens",
        ["gen_ai.response.tokens"] = "gen_ai.usage.output_tokens",
        ["prompt_tokens"] = "gen_ai.usage.input_tokens",
        ["completion_tokens"] = "gen_ai.usage.output_tokens",
        ["total_tokens"] = "gen_ai.usage.input_tokens + gen_ai.usage.output_tokens",
        ["gen_ai.model"] = "gen_ai.request.model",
        ["model"] = "gen_ai.request.model",
        ["gen_ai.operation"] = "gen_ai.operation.name",
        ["operation"] = "gen_ai.operation.name",
        ["gen_ai.request.prompt"] = "gen_ai.prompt",
        ["gen_ai.response.completion"] = "gen_ai.completion"
    };

    private static readonly Dictionary<string, Dictionary<string, string>> DeprecatedAttributeValues =
        new(StringComparer.OrdinalIgnoreCase) {
            ["cloud.platform"] = new(StringComparer.OrdinalIgnoreCase) {
                ["azure_aks"] = "Use 'azure.aks' instead.",
                ["azure_app_service"] = "Use 'azure.app_service' instead.",
                ["azure_container_apps"] = "Use 'azure.container_apps' instead.",
                ["azure_container_instances"] = "Use 'azure.container_instances' instead.",
                ["azure_functions"] = "Use 'azure.functions' instead.",
                ["azure_openshift"] = "Use 'azure.openshift' instead.",
                ["azure_vm"] = "Use 'azure.vm' instead."
            },
            ["db.system"] = new(StringComparer.OrdinalIgnoreCase) {
                ["cache"] = "Use 'intersystems_cache' instead.",
                ["cloudscape"] = "Use 'other_sql' instead.",
                ["coldfusion"] = "No replacement exists at this time.",
                ["firstsql"] = "Use 'other_sql' instead.",
                ["mssqlcompact"] = "Use 'other_sql' instead."
            },
            ["gen_ai.system"] = new(StringComparer.OrdinalIgnoreCase) {
                ["az.ai.inference"] = "Use 'azure.ai.inference' instead.",
                ["az.ai.openai"] = "Use 'azure.ai.openai' instead.",
                ["gemini"] = "Use 'gcp.gemini' instead.",
                ["vertex_ai"] = "Use 'gcp.vertex_ai' instead."
            },
            ["messaging.operation.type"] = new(StringComparer.OrdinalIgnoreCase) {
                ["deliver"] = "Use 'process' instead.",
                ["publish"] = "Use 'send' instead."
            },
            ["os.type"] = new(StringComparer.OrdinalIgnoreCase) {
                ["z_os"] = "Use 'zos' instead."
            },
            ["system.memory.state"] = new(StringComparer.OrdinalIgnoreCase) {
                ["shared"] = "Report shared memory usage with 'system.memory.linux.shared' instead."
            },
            ["vcs.provider.name"] = new(StringComparer.OrdinalIgnoreCase) {
                ["gittea"] = "Use 'gitea' instead."
            }
        };

    private static readonly Dictionary<string, string> ContextSensitiveDeprecatedAttributes =
        new(StringComparer.OrdinalIgnoreCase) {
            ["code.function"] = "Include the value in 'code.function.name' as a fully-qualified name.",
            ["code.namespace"] = "Include the namespace in 'code.function.name' as a fully-qualified name.",
            ["db.connection_string"] = "Use 'server.address' and 'server.port' instead.",
            ["db.cosmosdb.operation_type"] = "No replacement exists at this time.",
            ["db.cosmosdb.status_code"] = "Use 'db.response.status_code' instead.",
            ["db.instance.id"] = "No general replacement exists; for Elasticsearch use 'db.elasticsearch.node.name' instead.",
            ["db.jdbc.driver_classname"] = "No replacement exists at this time.",
            ["db.mssql.instance_name"] = "No replacement exists at this time.",
            ["db.redis.database_index"] = "Use 'db.namespace' instead.",
            ["db.user"] = "No replacement exists at this time.",
            ["enduser.scope"] = "No replacement exists at this time.",
            ["error.message"] = "Use a domain-specific error message attribute instead.",
            ["exception.escaped"] = "Avoid recording handled exceptions that do not escape the span scope.",
            ["gen_ai.completion"] = "Use 'gen_ai.output.messages' or 'gen_ai.client.inference.operation.details' instead.",
            ["gen_ai.prompt"] = "Use 'gen_ai.input.messages', 'gen_ai.system_instructions', or 'gen_ai.client.inference.operation.details' instead.",
            ["http.host"] = "Use 'server.address', 'client.address', or 'http.request.header.host' depending on the usage.",
            ["http.target"] = "Split the value into 'url.path' and 'url.query'.",
            ["messaging.destination_publish.anonymous"] = "No replacement exists at this time.",
            ["messaging.destination_publish.name"] = "No replacement exists at this time.",
            ["messaging.rocketmq.client_group"] = "Use 'messaging.consumer.group.name' on consumer spans; there is no producer replacement.",
            ["net.sock.family"] = "Split the value into 'network.transport' and 'network.type'.",
            ["net.sock.peer.name"] = "No replacement exists at this time.",
            ["rpc.grpc.status_code"] = "Use the string representation on 'rpc.response.status_code' instead.",
            ["rpc.jsonrpc.error_code"] = "Use the string representation on 'rpc.response.status_code' instead.",
            ["rpc.jsonrpc.error_message"] = "Use the span status description instead.",
            ["rpc.service"] = "Include the service in a fully-qualified 'rpc.method' instead."
        };

    internal static bool TryGetDeprecatedAttribute(
        string attributeName,
        out (string Replacement, string Version) info) =>
        DeprecatedAttributes.TryGetValue(attributeName, out info);

    internal static bool TryGetDeprecatedGenAiAttribute(string attributeName, out string replacement) =>
        DeprecatedGenAiAttributes.TryGetValue(attributeName, out replacement);

    internal static bool TryGetDeprecatedAttributeValue(
        string attributeName,
        string attributeValue,
        out string guidance) {
        if (DeprecatedAttributeValues.TryGetValue(attributeName, out var values)
            && values.TryGetValue(attributeValue, out guidance)) {
            return true;
        }

        guidance = string.Empty;
        return false;
    }

    internal static bool TryGetContextSensitiveDeprecatedAttribute(string attributeName, out string guidance) =>
        ContextSensitiveDeprecatedAttributes.TryGetValue(attributeName, out guidance);
}
