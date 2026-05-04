
namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0085: Detects attribute values that violate OTel semantic convention specifications.
/// </summary>
/// <remarks>
///     <para>
///         Validates that known semantic convention attributes have correct value formats:
///         <list type="bullet">
///             <item>http.response.status_code - must be an integer (100-599)</item>
///             <item>gen_ai.system - must be a known provider (openai, anthropic, etc.)</item>
///             <item>error.type - should be an exception type or error code</item>
///         </list>
///     </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0085InvalidAttributeValueAnalyzer : AlAnalyzer {
    /// <summary>Attribute validators by attribute name.</summary>
    private static readonly Dictionary<string, AttributeValidator> s_validators = new(StringComparer.OrdinalIgnoreCase) {
        ["http.response.status_code"] = new AttributeValidator(
            ValidateHttpStatusCode,
            "integer between 100-599"),
        ["http.request.method"] = new AttributeValidator(
            ValidateHttpMethod,
            "valid HTTP method (GET, POST, PUT, DELETE, PATCH, HEAD, OPTIONS, TRACE, CONNECT, _OTHER)"),
        ["gen_ai.provider.name"] = new AttributeValidator(
            ValidateGenAiProvider,
            "one of the official provider names such as openai, anthropic, azure.ai.inference, azure.ai.openai, gcp.vertex_ai, gcp.gemini, cohere, aws.bedrock"),
        ["gen_ai.operation.name"] = new AttributeValidator(
            ValidateGenAiOperation,
            "one of: chat, generate_content, text_completion, embeddings, retrieval, create_agent, invoke_agent, execute_tool, invoke_workflow"),
        ["gen_ai.request.max_tokens"] = new AttributeValidator(
            ValidatePositiveInteger,
            "positive integer"),
        ["gen_ai.request.temperature"] = new AttributeValidator(
            ValidateTemperature,
            "number between 0.0 and 2.0"),
        ["gen_ai.request.top_p"] = new AttributeValidator(
            ValidateProbability,
            "number between 0.0 and 1.0"),
        ["gen_ai.response.finish_reasons"] = new AttributeValidator(
            ValidateFinishReason,
            "one of: stop, length, content_filter, tool_calls, error"),
        ["gen_ai.usage.input_tokens"] = new AttributeValidator(
            ValidateNonNegativeInteger,
            "non-negative integer"),
        ["gen_ai.usage.output_tokens"] = new AttributeValidator(
            ValidateNonNegativeInteger,
            "non-negative integer"),
        ["rpc.response.status_code"] = new AttributeValidator(
            ValidateRpcResponseStatusCode,
            "non-empty status code string such as OK, DEADLINE_EXCEEDED, or -32602"),
        ["url.scheme"] = new AttributeValidator(
            ValidateUrlScheme,
            "one of: http, https, ftp, ws, wss")
    };

    /// <summary>The diagnostic identifier for AL0085.</summary>
    private const string DiagnosticId = "AL0085";

    private static readonly DiagnosticDescriptor s_rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.OpenTelemetry,
        DiagnosticSeverities.RequiredFix);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_rule];

    /// <summary>Registers operation actions to analyze SetTag and SetAttribute calls.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);

    private static void AnalyzeInvocation(OperationAnalysisContext context) {
        var invocation = (IInvocationOperation)context.Operation;

        if (invocation.TargetMethod.Name is not ("SetTag" or "SetAttribute" or "Add")
            || invocation.Arguments.Length < 2
            || invocation.Arguments[0].Value.UnwrapAllConversions().ConstantValue is not { HasValue: true, Value: string attributeName }
            || !s_validators.TryGetValue(attributeName, out var validator)) {
            return;
        }

        // Unwrap implicit conversions (e.g. string -> object? when the Activity overload takes object).
        // Without this, callers that pass a string literal to a `object?` parameter lose ConstantValue
        // and the validator never runs.
        var valueArg = invocation.Arguments[1].Value.UnwrapAllConversions();

        if (valueArg.ConstantValue.HasValue) {
            var valueString = valueArg.ConstantValue.Value?.ToString() ?? string.Empty;

            if (!validator.Validate(valueString)) {
                context.ReportDiagnostic(Diagnostic.Create(
                    s_rule,
                    invocation.Arguments[1].Syntax.GetLocation(),
                    attributeName,
                    valueString,
                    validator.ExpectedFormat));
            }
        }
    }

    private static bool ValidateHttpStatusCode(string value) =>
        int.TryParse(value, out var code) && code is >= 100 and <= 599;

    private static bool ValidateHttpMethod(string value) =>
        value is "GET" or "POST" or "PUT" or "DELETE" or "PATCH"
            or "HEAD" or "OPTIONS" or "TRACE" or "CONNECT" or "_OTHER";

    private static bool ValidateGenAiProvider(string value) =>
        OpenTelemetryGenAiSemconvFacts.IsValidProviderName(value);

    private static bool ValidateGenAiOperation(string value) =>
        OpenTelemetryGenAiSemconvFacts.IsValidOperationName(value);

    private static bool ValidatePositiveInteger(string value) =>
        int.TryParse(value, out var num) && num > 0;

    private static bool ValidateNonNegativeInteger(string value) =>
        int.TryParse(value, out var num) && num >= 0;

    private static bool ValidateTemperature(string value) =>
        double.TryParse(value, out var temp) && temp is >= 0.0 and <= 2.0;

    private static bool ValidateProbability(string value) =>
        double.TryParse(value, out var prob) && prob is >= 0.0 and <= 1.0;

    private static bool ValidateFinishReason(string value) =>
        value.EqualsIgnoreCase("stop")
        || value.EqualsIgnoreCase("length")
        || value.EqualsIgnoreCase("content_filter")
        || value.EqualsIgnoreCase("tool_calls")
        || value.EqualsIgnoreCase("error");

    private static bool ValidateRpcResponseStatusCode(string value) =>
        !string.IsNullOrWhiteSpace(value);

    private static bool ValidateUrlScheme(string value) =>
        value.EqualsIgnoreCase("http")
        || value.EqualsIgnoreCase("https")
        || value.EqualsIgnoreCase("ftp")
        || value.EqualsIgnoreCase("ws")
        || value.EqualsIgnoreCase("wss");

    /// <summary>Encapsulates validation logic and expected format message.</summary>
    private sealed partial class AttributeValidator(Func<string, bool> validate, string expectedFormat) {
        public string ExpectedFormat { get; } = expectedFormat;

        public bool Validate(string value) => validate(value);
    }
}
