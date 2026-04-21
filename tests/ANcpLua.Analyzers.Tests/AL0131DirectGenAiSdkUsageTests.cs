using AnalyzerTestBase = ANcpLua.Roslyn.Utilities.Testing.AnalyzerTest<ANcpLua.Analyzers.Analyzers.Al0131DirectGenAiSdkUsageAnalyzer>;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL0131: Direct GenAI SDK usage bypassing <c>IChatClient</c>.
/// </summary>
/// <remarks>
///     The rule MUST fire on direct provider SDK invocations (OpenAI, Anthropic, Ollama, etc.)
///     and MUST NOT fire on calls to the Microsoft.Agents.AI abstractions (<c>AIAgent</c>,
///     <c>ChatClientAgent</c>, <c>DelegatingAIAgent</c>). Those are the abstraction, not the bypass.
/// </remarks>
public sealed partial class Al0131DirectGenAiSdkUsageTests : AnalyzerTestBase
{
    [Fact]
    public Task ShouldNotReportAIAgentRunAsync() =>
        VerifyAsync("""
                    namespace Microsoft.Agents.AI {
                        public abstract class AIAgent {
                            public virtual System.Threading.Tasks.Task RunAsync() => System.Threading.Tasks.Task.CompletedTask;
                        }
                    }

                    public class C {
                        public async System.Threading.Tasks.Task M(Microsoft.Agents.AI.AIAgent agent) {
                            await agent.RunAsync();
                        }
                    }
                    """);

    [Fact]
    public Task ShouldNotReportChatClientAgentRunAsync() =>
        VerifyAsync("""
                    namespace Microsoft.Agents.AI {
                        public class ChatClientAgent {
                            public virtual System.Threading.Tasks.Task RunAsync() => System.Threading.Tasks.Task.CompletedTask;
                        }
                    }

                    public class C {
                        public async System.Threading.Tasks.Task M(Microsoft.Agents.AI.ChatClientAgent agent) {
                            await agent.RunAsync();
                        }
                    }
                    """);

    [Fact]
    public Task ShouldNotReportDelegatingAIAgentRunAsync() =>
        VerifyAsync("""
                    namespace Microsoft.Agents.AI {
                        public class DelegatingAIAgent {
                            public virtual System.Threading.Tasks.Task RunAsync() => System.Threading.Tasks.Task.CompletedTask;
                        }
                    }

                    public class C {
                        public async System.Threading.Tasks.Task M(Microsoft.Agents.AI.DelegatingAIAgent agent) {
                            await agent.RunAsync();
                        }
                    }
                    """);

    [Fact]
    public Task ShouldReportDirectOpenAIChatClientCall() =>
        VerifyAsync("""
                    namespace OpenAI.Chat {
                        public class ChatClient {
                            public System.Threading.Tasks.Task CompleteChatAsync() => System.Threading.Tasks.Task.CompletedTask;
                        }
                    }

                    public class C {
                        public async System.Threading.Tasks.Task M(OpenAI.Chat.ChatClient client) {
                            await {|AL0131:client.CompleteChatAsync()|};
                        }
                    }
                    """);

    [Fact]
    public Task ShouldReportDirectAnthropicCall() =>
        VerifyAsync("""
                    namespace Anthropic {
                        public class AnthropicClient {
                            public System.Threading.Tasks.Task CreateMessageAsync() => System.Threading.Tasks.Task.CompletedTask;
                        }
                    }

                    public class C {
                        public async System.Threading.Tasks.Task M(Anthropic.AnthropicClient client) {
                            await {|AL0131:client.CreateMessageAsync()|};
                        }
                    }
                    """);

    [Fact]
    public Task ShouldNotReportIChatClientCall() =>
        VerifyAsync("""
                    namespace Microsoft.Extensions.AI {
                        public interface IChatClient {
                            System.Threading.Tasks.Task GetResponseAsync();
                        }
                    }

                    public class C {
                        public async System.Threading.Tasks.Task M(Microsoft.Extensions.AI.IChatClient client) {
                            await client.GetResponseAsync();
                        }
                    }
                    """);
}
