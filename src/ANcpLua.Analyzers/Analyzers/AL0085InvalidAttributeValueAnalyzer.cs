
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
    /// <summary>Known gen_ai.system provider values from semantic conventions.</summary>
    private static readonly HashSet<string> ValidGenAiSystems = new(StringComparer.OrdinalIgnoreCase) {
        "openai",
        "anthropic",
        "azure_ai_inference",
        "vertex_ai",
        "cohere",
        "aws_bedrock",
        "watsonx",
        "az.ai.inference"
    };

    /// <summary>Known gen_ai.operation.name values from semantic conventions.</summary>
    private static readonly HashSet<string> ValidGenAiOperations = new(StringComparer.OrdinalIgnoreCase) {
        "chat",
        "text_completion",
        "embeddings",
        "create_agent",
        "invoke_agent",
        "execute_tool"
    };

    /// <summary>Attribute validators by attribute name.</summary>
    private static readonly Dictionary<string, AttributeValidator> Validators = new(StringComparer.OrdinalIgnoreCase) {
        ["http.response.status_code"] = new AttributeValidator(
            ValidateHttpStatusCode,
            "integer between 100-599"),
        ["http.request.method"] = new AttributeValidator(
            ValidateHttpMethod,
            "valid HTTP method (GET, POST, PUT, DELETE, PATCH, HEAD, OPTIONS, TRACE, CONNECT, _OTHER)"),
        ["gen_ai.system"] = new AttributeValidator(
            ValidateGenAiSystem,
            "one of: openai, anthropic, azure_ai_inference, vertex_ai, cohere, aws_bedrock, watsonx, az.ai.inference"),
        ["gen_ai.operation.name"] = new AttributeValidator(
            ValidateGenAiOperation,
            "one of: chat, text_completion, embeddings, create_agent, invoke_agent, execute_tool"),
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
        ["rpc.grpc.status_code"] = new AttributeValidator(
            ValidateGrpcStatusCode,
            "integer between 0-16"),
        ["url.scheme"] = new AttributeValidator(
            ValidateUrlScheme,
            "one of: http, https, ftp, ws, wss")
    };

    /// <summary>The diagnostic identifier for AL0085.</summary>
    private const string DiagnosticId = "AL0085";

    private static readonly DiagnosticDescriptor Rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.OpenTelemetry,
        DiagnosticSeverities.RequiredFix);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    /// <summary>Registers operation actions to analyze SetTag and SetAttribute calls.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);

    private static void AnalyzeInvocation(OperationAnalysisContext context) {
        var invocation = (IInvocationOperation)context.Operation;

        if (invocation.TargetMethod.Name is not ("SetTag" or "SetAttribute" or "Add")
            || invocation.Arguments.Length < 2
            || invocation.Arguments[0].Value.ConstantValue is not { HasValue: true, Value: string attributeName }
            || !Validators.TryGetValue(attributeName, out var validator)) {
            return;
        }

        var valueArg = invocation.Arguments[1].Value;

        if (valueArg.ConstantValue.HasValue) {
            var valueString = valueArg.ConstantValue.Value?.ToString() ?? string.Empty;

            if (!validator.Validate(valueString)) {
                context.ReportDiagnostic(Diagnostic.Create(
                    Rule,
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

    private static bool ValidateGenAiSystem(string value) =>
        ValidGenAiSystems.Contains(value);

    private static bool ValidateGenAiOperation(string value) =>
        ValidGenAiOperations.Contains(value);

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

    private static bool ValidateGrpcStatusCode(string value) =>
        int.TryParse(value, out var code) && code is >= 0 and <= 16;

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
