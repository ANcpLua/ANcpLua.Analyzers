namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0133: Detects deprecated semantic convention attributes that require context-sensitive migration.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0133ContextSensitiveDeprecatedSemconvAnalyzer : AlAnalyzer {
    /// <summary>The diagnostic identifier for AL0133.</summary>
    private const string DiagnosticId = "AL0133";

    private static readonly Dictionary<string, string> DeprecatedAttributes = new(StringComparer.OrdinalIgnoreCase) {
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

    private static readonly DiagnosticDescriptor Rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.OpenTelemetry,
        DiagnosticSeverity.Warning);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    /// <summary>Registers syntax node actions to analyze string literals in telemetry contexts.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterSyntaxNodeAction(AnalyzeStringLiteral, SyntaxKind.StringLiteralExpression);

    private static void AnalyzeStringLiteral(SyntaxNodeAnalysisContext context) {
        var assemblyName = context.SemanticModel.Compilation.AssemblyName;
        if (assemblyName is not null && assemblyName.StartsWithOrdinal("ANcpLua.Analyzers")) {
            return;
        }

        var literal = (LiteralExpressionSyntax)context.Node;
        var value = literal.Token.ValueText;

        if (string.IsNullOrEmpty(value)
            || !DeprecatedAttributes.TryGetValue(value, out var guidance)
            || !IsInTelemetryContext(literal)) {
            return;
        }

        context.ReportDiagnostic(Rule, literal.GetLocation(), value, guidance);
    }

    private static bool IsInTelemetryContext(SyntaxNode node) {
        var current = node.Parent;

        while (current is not null) {
            if (IsTelemetryElementAccess(current) ||
                IsTelemetryInvocation(current) ||
                IsTelemetryInitializer(current) ||
                current is AssignmentExpressionSyntax { Parent: InitializerExpressionSyntax }) {
                return true;
            }

            current = current.Parent;
        }

        return false;
    }

    private static bool IsTelemetryElementAccess(SyntaxNode node) =>
        node is ElementAccessExpressionSyntax elementAccess &&
        GetIdentifierName(elementAccess.Expression) is { } identifier &&
        IsLikelyTelemetryContainer(identifier);

    private static bool IsTelemetryInvocation(SyntaxNode node) {
        if (node is not InvocationExpressionSyntax invocation || GetMethodName(invocation) is not { } methodName) {
            return false;
        }

        if (methodName.ContainsIgnoreCase("ATTRIBUTE")
            || methodName.ContainsIgnoreCase("TAG")) {
            return true;
        }

        return invocation.Expression is MemberAccessExpressionSyntax memberAccess
               && memberAccess.Name.Identifier.Text == "Add"
               && GetIdentifierName(memberAccess.Expression) is { } identifier
               && IsLikelyTelemetryContainer(identifier);
    }

    private static bool IsTelemetryInitializer(SyntaxNode node) =>
        node is InitializerExpressionSyntax { Parent: ObjectCreationExpressionSyntax creation } &&
        IsTelemetryTypeName(creation.Type.ToString());

    private static bool IsTelemetryTypeName(string typeName) =>
        typeName.ContainsOrdinal("Tag")
        || typeName.ContainsOrdinal("Attribute")
        || typeName.ContainsOrdinal("KeyValuePair");

    private static bool IsLikelyTelemetryContainer(string identifier) =>
        identifier.ContainsIgnoreCase("ATTRIBUTE")
        || identifier.ContainsIgnoreCase("TAG")
        || identifier.ContainsIgnoreCase("ATTR")
        || identifier.EqualsIgnoreCase("ATTRS");

    private static string? GetMethodName(InvocationExpressionSyntax invocation) =>
        invocation.Expression switch {
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.Text,
            IdentifierNameSyntax identifier => identifier.Identifier.Text,
            _ => null
        };

    private static string? GetIdentifierName(ExpressionSyntax expression) =>
        expression switch {
            IdentifierNameSyntax identifier => identifier.Identifier.Text,
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.Text,
            _ => null
        };
}
