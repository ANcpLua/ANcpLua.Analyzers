
namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0131: Warns when application code calls GenAI SDK APIs directly, bypassing the
///     <c>IChatClient</c> abstraction and the automatic OpenTelemetry instrumentation pipeline.
/// </summary>
/// <remarks>
///     Register the SDK client through DI and layer instrumentation on the resulting
///     <c>IChatClient</c>:
///     <code>
///         builder.Services.AddChatClient(inner =&gt; new OpenAIChatClient(model, apiKey))
///                         .AsBuilder()
///                         .UseFunctionInvocation()
///                         .UseOpenTelemetry(sourceName)
///                         .Build();
///     </code>
///     Direct SDK calls — <c>OpenAI.Chat.ChatClient.CompleteChatAsync</c>, <c>Anthropic.AnthropicClient.CreateMessageAsync</c>, etc.
///     — bypass this pipeline and emit no telemetry.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0131DirectGenAiSdkUsageAnalyzer : AlAnalyzer {
    /// <summary>The diagnostic identifier for AL0131.</summary>
    private const string DiagnosticId = "AL0131";

    private const string IChatClientMetadataName = "Microsoft.Extensions.AI.IChatClient";

    /// <summary>
    ///     Direct-SDK type metadata names paired with a short display label. <c>IChatClient</c> is
    ///     intentionally absent — it is the instrumented abstraction. Likewise <c>AIAgent</c>,
    ///     <c>ChatClientAgent</c>, and <c>DelegatingAIAgent</c> are NOT bypasses — they are the
    ///     Microsoft.Agents.AI abstractions the framework expects consumers to call through.
    /// </summary>
    private static readonly (string MetadataName, string Label)[] s_sdkTypes = [
        ("OpenAI.Chat.ChatClient", "OpenAI Chat"),
        ("OpenAI.Embeddings.EmbeddingClient", "OpenAI Embeddings"),
        ("OpenAI.Images.ImageClient", "OpenAI Images"),
        ("OpenAI.Audio.AudioClient", "OpenAI Audio"),
        ("Anthropic.AnthropicClient", "Anthropic"),
        ("Anthropic.Messaging.MessageClient", "Anthropic Messaging"),
        ("OllamaSharp.OllamaApiClient", "Ollama"),
        ("Azure.AI.OpenAI.OpenAIClient", "Azure.AI.OpenAI"),
        ("Cohere.CohereClient", "Cohere")
    ];

    private static readonly DiagnosticDescriptor s_rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.GenAI,
        DiagnosticSeverities.Suggestion);

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_rule];

    /// <inheritdoc />
    protected override void RegisterActions(AnalysisContext context) {
        context.RegisterCompilationStartAction(compilationContext => {
            var iChatClientType = compilationContext.Compilation.GetTypeByMetadataName(IChatClientMetadataName);

            var knownSdkTypes = s_sdkTypes
                .Select(entry => (
                    Type: compilationContext.Compilation.GetTypeByMetadataName(entry.MetadataName),
                    entry.Label))
                .Where(entry => entry.Type is not null)
                .ToImmutableArray();

            if (knownSdkTypes.IsEmpty) {
                return;
            }

            compilationContext.RegisterOperationAction(
                ctx => AnalyzeInvocation(ctx, iChatClientType, knownSdkTypes),
                OperationKind.Invocation);
        });
    }

    private static void AnalyzeInvocation(
        OperationAnalysisContext context,
        INamedTypeSymbol? iChatClientType,
        ImmutableArray<(INamedTypeSymbol? Type, string Label)> knownSdkTypes) {
        var invocation = (IInvocationOperation)context.Operation;
        if (invocation.TargetMethod.ContainingType is not { } containingType) {
            return;
        }

        if (iChatClientType is not null && containingType.IsEqualTo(iChatClientType)) {
            return;
        }

        foreach (var (sdkType, label) in knownSdkTypes) {
            if (sdkType is not null && containingType.IsEqualTo(sdkType)) {
                context.ReportDiagnostic(Diagnostic.Create(
                    s_rule,
                    invocation.Syntax.GetLocation(),
                    label));
                return;
            }
        }
    }
}
