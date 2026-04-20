namespace ANcpLua.Analyzers.Analyzers;

internal static partial class OpenTelemetryGenAiSemconvFacts {
    internal static readonly string[] RequiredAttributeKeys = [
        "gen_ai.provider.name",
        "gen_ai.request.model",
        "gen_ai.operation.name"
    ];

    private static readonly HashSet<string> ValidProviderNames = new(StringComparer.OrdinalIgnoreCase) {
        "openai",
        "anthropic",
        "cohere",
        "azure.ai.inference",
        "azure.ai.openai",
        "gcp.gen_ai",
        "gcp.vertex_ai",
        "gcp.gemini",
        "ibm.watsonx.ai",
        "aws.bedrock",
        "perplexity",
        "x_ai",
        "deepseek",
        "groq",
        "mistral_ai"
    };

    private static readonly HashSet<string> ValidOperationNames = new(StringComparer.OrdinalIgnoreCase) {
        "chat",
        "generate_content",
        "text_completion",
        "embeddings",
        "retrieval",
        "create_agent",
        "invoke_agent",
        "execute_tool",
        "invoke_workflow"
    };

    internal static bool IsValidProviderName(string value) => ValidProviderNames.Contains(value);

    internal static bool IsValidOperationName(string value) => ValidOperationNames.Contains(value);
}
