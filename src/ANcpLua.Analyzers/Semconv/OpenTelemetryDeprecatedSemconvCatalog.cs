namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     Central snapshot of deprecated OpenTelemetry semantic conventions sourced from
///     <c>semantic-conventions/model/**/*.yaml</c>.
/// </summary>
internal static class OpenTelemetryDeprecatedSemconvCatalog {
    private static readonly Dictionary<string, (string Replacement, string Version)> s_deprecatedAttributes =
        new(StringComparer.OrdinalIgnoreCase) {
            ["android.state"] = ("android.app.state", "1.31.0"),
            ["az.namespace"] = ("azure.resource_provider.namespace", "1.35.0"),
            ["az.service_request_id"] = ("azure.service.request.id", "1.35.0"),
            ["code.column"] = ("code.column.number", "1.30.0"),
            ["code.filepath"] = ("code.file.path", "1.30.0"),
            ["code.lineno"] = ("code.line.number", "1.30.0"),
            ["container.cpu.state"] = ("cpu.mode", "1.27.0"),
            ["container.labels"] = ("container.label", "1.25.0"),
            ["container.runtime"] = ("container.runtime.name", "1.37.0"),
            ["db.cassandra.consistency_level"] = ("cassandra.consistency.level", "1.30.0"),
            ["db.cassandra.coordinator.dc"] = ("cassandra.coordinator.dc", "1.30.0"),
            ["db.cassandra.coordinator.id"] = ("cassandra.coordinator.id", "1.30.0"),
            ["db.cassandra.idempotence"] = ("cassandra.query.idempotent", "1.30.0"),
            ["db.cassandra.page_size"] = ("cassandra.page.size", "1.30.0"),
            ["db.cassandra.speculative_execution_count"] = ("cassandra.speculative_execution.count", "1.30.0"),
            ["db.cassandra.table"] = ("db.collection.name", "1.25.0"),
            ["db.client.connections.pool.name"] = ("db.client.connection.pool.name", "1.27.0"),
            ["db.client.connections.state"] = ("db.client.connection.state", "1.27.0"),
            ["db.cosmosdb.client_id"] = ("azure.client.id", "1.30.0"),
            ["db.cosmosdb.connection_mode"] = ("azure.cosmosdb.connection.mode", "1.30.0"),
            ["db.cosmosdb.consistency_level"] = ("azure.cosmosdb.consistency.level", "1.30.0"),
            ["db.cosmosdb.container"] = ("db.collection.name", "1.25.0"),
            ["db.cosmosdb.regions_contacted"] = ("azure.cosmosdb.operation.contacted_regions", "1.30.0"),
            ["db.cosmosdb.request_charge"] = ("azure.cosmosdb.operation.request_charge", "1.30.0"),
            ["db.cosmosdb.request_content_length"] = ("azure.cosmosdb.request.body.size", "1.30.0"),
            ["db.cosmosdb.sub_status_code"] = ("azure.cosmosdb.response.sub_status_code", "1.30.0"),
            ["db.elasticsearch.cluster.name"] = ("db.namespace", "1.27.0"),
            ["db.elasticsearch.node.name"] = ("elasticsearch.node.name", "1.30.0"),
            ["db.mongodb.collection"] = ("db.collection.name", "1.25.0"),
            ["db.name"] = ("db.namespace", "1.25.0"),
            ["db.operation"] = ("db.operation.name", "1.25.0"),
            ["db.statement"] = ("db.query.text", "1.25.0"),
            ["db.system"] = ("db.system.name", "1.30.0"),
            ["deployment.environment"] = ("deployment.environment.name", "1.27.0"),
            ["feature_flag.evaluation.error.message"] = ("feature_flag.error.message", "1.33.0"),
            ["feature_flag.evaluation.reason"] = ("feature_flag.result.reason", "1.32.0"),
            ["feature_flag.provider_name"] = ("feature_flag.provider.name", "1.33.0"),
            ["feature_flag.variant"] = ("feature_flag.result.variant", "1.32.0"),
            ["http.client_ip"] = ("client.address", "1.21.0"),
            ["http.method"] = ("http.request.method", "1.21.0"),
            ["http.request_content_length_uncompressed"] = ("http.request.body.size", ""),
            ["http.response_content_length_uncompressed"] = ("http.response.body.size", ""),
            ["http.scheme"] = ("url.scheme", "1.21.0"),
            ["http.server_name"] = ("server.address", ""),
            ["http.status_code"] = ("http.response.status_code", "1.21.0"),
            ["http.url"] = ("url.full", "1.21.0"),
            ["http.user_agent"] = ("user_agent.original", "1.19.0"),
            ["ios.state"] = ("ios.app.state", "1.37.0"),
            ["k8s.pod.labels"] = ("k8s.pod.label", "1.25.0"),
            ["linux.memory.slab.state"] = ("system.memory.linux.slab.state", "1.39.0"),
            ["messaging.client_id"] = ("messaging.client.id", "1.26.0"),
            ["messaging.eventhubs.consumer.group"] = ("messaging.consumer.group.name", "1.27.0"),
            ["messaging.kafka.consumer.group"] = ("messaging.consumer.group.name", "1.27.0"),
            ["messaging.kafka.message.offset"] = ("messaging.kafka.offset", "1.27.0"),
            ["messaging.operation"] = ("messaging.operation.type", "1.25.0"),
            ["messaging.servicebus.destination.subscription_name"] = ("messaging.destination.subscription.name", "1.27.0"),
            ["net.host.ip"] = ("network.local.address", "1.13.0"),
            ["net.host.name"] = ("server.address", "1.21.0"),
            ["net.host.port"] = ("server.port", "1.21.0"),
            ["net.peer.ip"] = ("network.peer.address", "1.13.0"),
            ["net.protocol.name"] = ("network.protocol.name", "1.21.0"),
            ["net.protocol.version"] = ("network.protocol.version", "1.21.0"),
            ["net.sock.host.addr"] = ("network.local.address", "1.21.0"),
            ["net.sock.host.port"] = ("network.local.port", "1.21.0"),
            ["net.sock.peer.addr"] = ("network.peer.address", ""),
            ["net.sock.peer.port"] = ("network.peer.port", ""),
            ["net.transport"] = ("network.transport", ""),
            ["otel.library.name"] = ("otel.scope.name", ""),
            ["otel.library.version"] = ("otel.scope.version", ""),
            ["peer.service"] = ("service.peer.name", "1.39.0"),
            ["pool.name"] = ("db.client.connection.pool.name", "1.26.0"),
            ["process.context_switch_type"] = ("process.context_switch.type", "1.38.0"),
            ["process.cpu.state"] = ("cpu.mode", "1.27.0"),
            ["process.executable.build_id.profiling"] = ("process.executable.build_id.htlhash", "1.29.0"),
            ["process.paging.fault_type"] = ("system.paging.fault.type", "1.38.0"),
            ["rpc.connect_rpc.error_code"] = ("rpc.response.status_code", "1.39.0"),
            ["rpc.connect_rpc.request.metadata"] = ("rpc.request.metadata", "1.39.0"),
            ["rpc.connect_rpc.response.metadata"] = ("rpc.response.metadata", "1.39.0"),
            ["rpc.grpc.request.metadata"] = ("rpc.request.metadata", "1.39.0"),
            ["rpc.grpc.response.metadata"] = ("rpc.response.metadata", "1.39.0"),
            ["rpc.jsonrpc.request_id"] = ("jsonrpc.request.id", "1.39.0"),
            ["rpc.jsonrpc.version"] = ("jsonrpc.protocol.version", "1.39.0"),
            ["rpc.system"] = ("rpc.system.name", "1.39.0"),
            ["state"] = ("db.client.connection.state", "1.22.0"),
            ["system.cpu.logical_number"] = ("cpu.logical_number", "1.31.0"),
            ["system.cpu.state"] = ("cpu.mode", "1.27.0"),
            ["system.network.state"] = ("network.connection.state", "1.30.0"),
            ["system.paging.type"] = ("system.paging.fault.type", "1.38.0"),
            ["system.process.status"] = ("process.state", "1.38.0"),
            ["system.processes.status"] = ("process.state", "1.25.0"),
            ["tls.client.server_name"] = ("server.address", "1.27.0"),
            ["vcs.repository.change.id"] = ("vcs.change.id", "1.29.0"),
            ["vcs.repository.change.title"] = ("vcs.change.title", "1.29.0"),
            ["vcs.repository.ref.name"] = ("vcs.ref.head.name", "1.29.0"),
            ["vcs.repository.ref.revision"] = ("vcs.ref.head.revision", "1.29.0"),
            ["vcs.repository.ref.type"] = ("vcs.ref.head.type", "1.29.0"),
        };

    private static readonly Dictionary<string, (string ReplacementPrefix, string Version)> s_deprecatedAttributePrefixes =
        new(StringComparer.OrdinalIgnoreCase) {
            ["db.elasticsearch.path_parts."] = ("db.operation.parameter.", "1.30.0"),
        };

    private static readonly Dictionary<string, string> s_deprecatedGenAiAttributes = new(StringComparer.OrdinalIgnoreCase) {
        ["gen_ai.openai.request.response_format"] = "gen_ai.output.type",
        ["gen_ai.openai.request.seed"] = "gen_ai.request.seed",
        ["gen_ai.openai.request.service_tier"] = "openai.request.service_tier",
        ["gen_ai.openai.response.service_tier"] = "openai.response.service_tier",
        ["gen_ai.openai.response.system_fingerprint"] = "openai.response.system_fingerprint",
        ["gen_ai.system"] = "gen_ai.provider.name",
        ["gen_ai.usage.completion_tokens"] = "gen_ai.usage.output_tokens",
        ["gen_ai.usage.prompt_tokens"] = "gen_ai.usage.input_tokens",
    };

    private static readonly Dictionary<string, Dictionary<string, string>> s_deprecatedAttributeValues =
        new(StringComparer.OrdinalIgnoreCase) {
            ["cloud.platform"] = new(StringComparer.OrdinalIgnoreCase) {
                ["azure_aks"] = "Use 'azure.aks' instead.",
                ["azure_app_service"] = "Use 'azure.app_service' instead.",
                ["azure_container_apps"] = "Use 'azure.container_apps' instead.",
                ["azure_container_instances"] = "Use 'azure.container_instances' instead.",
                ["azure_functions"] = "Use 'azure.functions' instead.",
                ["azure_openshift"] = "Use 'azure.openshift' instead.",
                ["azure_vm"] = "Use 'azure.vm' instead.",
            },
            ["db.system"] = new(StringComparer.OrdinalIgnoreCase) {
                ["cache"] = "Use 'intersystems_cache' instead.",
                ["cloudscape"] = "Use 'other_sql' instead.",
                ["coldfusion"] = "No replacement exists at this time.",
                ["firstsql"] = "Use 'other_sql' instead.",
                ["mssqlcompact"] = "Use 'other_sql' instead.",
            },
            ["gen_ai.system"] = new(StringComparer.OrdinalIgnoreCase) {
                ["az.ai.inference"] = "Use 'azure.ai.inference' instead.",
                ["az.ai.openai"] = "Use 'azure.ai.openai' instead.",
                ["gemini"] = "Use 'gcp.gemini' instead.",
                ["vertex_ai"] = "Use 'gcp.vertex_ai' instead.",
            },
            ["messaging.operation.type"] = new(StringComparer.OrdinalIgnoreCase) {
                ["deliver"] = "Use 'process' instead.",
                ["publish"] = "Use 'send' instead.",
            },
            ["os.type"] = new(StringComparer.OrdinalIgnoreCase) {
                ["z_os"] = "Use 'zos' instead.",
            },
            ["system.memory.state"] = new(StringComparer.OrdinalIgnoreCase) {
                ["shared"] = "Removed, report shared memory usage with `metric.system.memory.linux.shared` metric",
            },
            ["vcs.provider.name"] = new(StringComparer.OrdinalIgnoreCase) {
                ["gittea"] = "Use 'gitea' instead.",
            },
        };

    private static readonly Dictionary<string, string> s_contextSensitiveDeprecatedNames = new(StringComparer.OrdinalIgnoreCase) {
        ["code.function"] = "Value should be included in `code.function.name` which is expected to be a fully-qualified name.",
        ["code.namespace"] = "Value should be included in `code.function.name` which is expected to be a fully-qualified name.",
        ["db.connection_string"] = "Replaced by `server.address` and `server.port`.",
        ["db.cosmosdb.operation_type"] = "Removed, no replacement at this time.",
        ["db.cosmosdb.status_code"] = "Use `db.response.status_code` instead.",
        ["db.instance.id"] = "Removed, no general replacement at this time. For Elasticsearch, use `db.elasticsearch.node.name` instead.",
        ["db.jdbc.driver_classname"] = "Removed, no replacement at this time.",
        ["db.mssql.instance_name"] = "Removed, no replacement at this time.",
        ["db.redis.database_index"] = "Use `db.namespace` instead.",
        ["db.sql.table"] = "Replaced by `db.collection.name`, but only if not extracting the value from `db.query.text`.",
        ["db.user"] = "Removed, no replacement at this time.",
        ["enduser.role"] = "Use `user.roles` instead.",
        ["enduser.scope"] = "Removed, no replacement at this time.",
        ["error.message"] = "Use domain-specific error message attribute. For example, use `feature_flag.error.message` for feature flag errors.",
        ["event.az.resource.log"] = "Use 'azure.resource.log' instead.",
        ["event.gen_ai.assistant.message"] = "Chat history is reported on `gen_ai.input.messages` attribute on spans or `gen_ai.client.inference.operation.details` event.",
        ["event.gen_ai.choice"] = "Chat history is reported on `gen_ai.output.messages` attribute on spans or `gen_ai.client.inference.operation.details` event.",
        ["event.gen_ai.system.message"] = "Chat history is reported on `gen_ai.system_instructions` attribute on spans or `gen_ai.client.inference.operation.details` event.",
        ["event.gen_ai.tool.message"] = "Chat history is reported on `gen_ai.input.messages` attribute on spans or `gen_ai.client.inference.operation.details` event.",
        ["event.gen_ai.user.message"] = "Chat history is reported on `gen_ai.input.messages` attribute on spans or `gen_ai.client.inference.operation.details` event.",
        ["event.name"] = "The value of this attribute MUST now be set as the value of the EventName field on the LogRecord to indicate that the LogRecord represents an Event.",
        ["event.rpc.message"] = "Deprecated, no replacement at this time.",
        ["exception.escaped"] = "It's no longer recommended to record exceptions that are handled and do not escape the scope of a span.",
        ["gen_ai.completion"] = "Removed, no replacement at this time.",
        ["gen_ai.prompt"] = "Removed, no replacement at this time.",
        ["http.flavor"] = "Split into `network.protocol.name` and `network.protocol.version`",
        ["http.host"] = "Replaced by one of `server.address`, `client.address` or `http.request.header.host`, depending on the usage.",
        ["http.request_content_length"] = "Replaced by `http.request.header.content-length`.",
        ["http.response_content_length"] = "Replaced by `http.response.header.content-length`.",
        ["http.target"] = "Split to `url.path` and `url.query`.",
        ["message.compressed_size"] = "Deprecated, no replacement at this time.",
        ["message.id"] = "Deprecated, no replacement at this time.",
        ["message.type"] = "Deprecated, no replacement at this time.",
        ["message.uncompressed_size"] = "Deprecated, no replacement at this time.",
        ["messaging.destination_publish.anonymous"] = "Removed. No replacement at this time.",
        ["messaging.destination_publish.name"] = "Removed. No replacement at this time.",
        ["messaging.kafka.destination.partition"] = "Record string representation of the partition id in `messaging.destination.partition.id` attribute.",
        ["messaging.rocketmq.client_group"] = "Replaced by `messaging.consumer.group.name` on the consumer spans. No replacement for producer spans.",
        ["net.peer.name"] = "Replaced by `server.address` on client spans and `client.address` on server spans.",
        ["net.peer.port"] = "Replaced by `server.port` on client spans and `client.port` on server spans.",
        ["net.sock.family"] = "Split to `network.transport` and `network.type`.",
        ["net.sock.peer.name"] = "Removed. No replacement at this time.",
        ["rpc.grpc.status_code"] = "Use string representation of the gRPC status code on the `rpc.response.status_code` attribute.",
        ["rpc.jsonrpc.error_code"] = "Use string representation of the error code on the `rpc.response.status_code` attribute.",
        ["rpc.jsonrpc.error_message"] = "Use the span status description when reporting JSON-RPC spans.",
        ["rpc.message.compressed_size"] = "Deprecated, no replacement at this time.",
        ["rpc.message.id"] = "Deprecated, no replacement at this time.",
        ["rpc.message.type"] = "Deprecated, no replacement at this time.",
        ["rpc.message.uncompressed_size"] = "Deprecated, no replacement at this time.",
        ["rpc.service"] = "Value should be included in `rpc.method` which is expected to be a fully-qualified name.",
    };

    internal static bool TryGetDeprecatedAttribute(
        string attributeName,
        out (string Replacement, string Version) info) {
        if (s_deprecatedAttributes.TryGetValue(attributeName, out info)) {
            return true;
        }

        foreach (var prefix in s_deprecatedAttributePrefixes) {
            if (attributeName.StartsWith(prefix.Key, StringComparison.OrdinalIgnoreCase)) {
                var suffix = attributeName[prefix.Key.Length..];
                info = ($"{prefix.Value.ReplacementPrefix}{suffix}", prefix.Value.Version);
                return true;
            }
        }

        info = default;
        return false;
    }

    internal static bool TryGetDeprecatedGenAiAttribute(string attributeName, [NotNullWhen(true)] out string? replacement) =>
        s_deprecatedGenAiAttributes.TryGetValue(attributeName, out replacement);

    internal static bool TryGetDeprecatedAttributeValue(
        string attributeName,
        string attributeValue,
        [NotNullWhen(true)] out string? guidance) {
        if (s_deprecatedAttributeValues.TryGetValue(attributeName, out var values)
            && values.TryGetValue(attributeValue, out guidance)) {
            return true;
        }

        guidance = null;
        return false;
    }

    internal static bool TryGetContextSensitiveDeprecatedName(string name, [NotNullWhen(true)] out string? guidance) =>
        s_contextSensitiveDeprecatedNames.TryGetValue(name, out guidance);
}
