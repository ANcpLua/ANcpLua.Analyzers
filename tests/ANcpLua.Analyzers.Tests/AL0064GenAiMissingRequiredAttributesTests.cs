using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL0064: Detects GenAI spans that are missing required semantic convention attributes.
/// </summary>
public sealed partial class Al0064GenAiMissingRequiredAttributesTests : AnalyzerTest<Al0064GenAiMissingRequiredAttributesAnalyzer> {
    private const string ActivityStubs = """
        namespace System.Diagnostics {
            public class Activity {
                public Activity SetTag(string key, string? value) => this;
                public Activity SetTag(string key, int value) => this;
                public Activity SetTag(string key, object? value) => this;
            }
            public class ActivitySource {
                public ActivitySource(string name) { }
                public Activity? StartActivity(string name) => new Activity();
            }
        }
        """;

    // ── Should report: missing required attributes ────────────────────────

    [Fact]
    public Task ShouldReport_WhenAllAttributesMissing() =>
        VerifyAsync($$"""
            {{ActivityStubs}}

            public class C {
                private static readonly System.Diagnostics.ActivitySource Source = new("test");

                void M() {
                    var activity = {|AL0064:{|AL0064:{|AL0064:Source.StartActivity("gen_ai.chat")|}|}|};
                }
            }
            """);

    [Fact]
    public Task ShouldReport_WhenSomeAttributesMissing() =>
        VerifyAsync($$"""
            {{ActivityStubs}}

            public class C {
                private static readonly System.Diagnostics.ActivitySource Source = new("test");

                void M() {
                    var activity = {|AL0064:{|AL0064:Source.StartActivity("gen_ai.chat")|}|};
                    activity?.SetTag("gen_ai.provider.name", "openai");
                }
            }
            """);

    // ── Should NOT report: all required attributes present ────────────────

    [Fact]
    public Task ShouldNotReport_WhenAllRequiredAttributesPresent() =>
        VerifyAsync($$"""
            {{ActivityStubs}}

            public class C {
                private static readonly System.Diagnostics.ActivitySource Source = new("test");

                void M() {
                    var activity = Source.StartActivity("gen_ai.chat");
                    activity?.SetTag("gen_ai.provider.name", "openai");
                    activity?.SetTag("gen_ai.request.model", "gpt-4");
                    activity?.SetTag("gen_ai.operation.name", "chat");
                }
            }
            """);

    [Fact]
    public Task ShouldNotReport_WhenAllAttributesPresentViaConstants() =>
        VerifyAsync($$"""
            {{ActivityStubs}}

            public static class GenAiAttributes {
                public const string ProviderName = "gen_ai.provider.name";
                public const string RequestModel = "gen_ai.request.model";
                public const string OperationName = "gen_ai.operation.name";
            }

            public class C {
                private static readonly System.Diagnostics.ActivitySource Source = new("test");

                void M() {
                    var activity = Source.StartActivity("gen_ai.chat");
                    activity?.SetTag(GenAiAttributes.ProviderName, "openai");
                    activity?.SetTag(GenAiAttributes.RequestModel, "gpt-4");
                    activity?.SetTag(GenAiAttributes.OperationName, "chat");
                }
            }
            """);

    [Fact]
    public Task ShouldNotReport_WhenSetTagAfterNullGuard() =>
        VerifyAsync($$"""
            {{ActivityStubs}}

            public class C {
                private static readonly System.Diagnostics.ActivitySource Source = new("test");

                void M() {
                    var activity = Source.StartActivity("gen_ai.chat");
                    if (activity is null) return;
                    activity.SetTag("gen_ai.provider.name", "openai");
                    activity.SetTag("gen_ai.request.model", "gpt-4");
                    activity.SetTag("gen_ai.operation.name", "chat");
                }
            }
            """);

    [Fact]
    public Task ShouldNotReport_WhenSetTagAfterNullGuardWithConstants() =>
        VerifyAsync($$"""
            {{ActivityStubs}}

            public static class GenAiAttributes {
                public const string ProviderName = "gen_ai.provider.name";
                public const string RequestModel = "gen_ai.request.model";
                public const string OperationName = "gen_ai.operation.name";
            }

            public class C {
                private static readonly System.Diagnostics.ActivitySource Source = new("test");

                void M() {
                    var activity = Source.StartActivity("gen_ai.chat");
                    if (activity is null) return;
                    activity.SetTag(GenAiAttributes.ProviderName, "openai");
                    activity.SetTag(GenAiAttributes.RequestModel, "gpt-4");
                    activity.SetTag(GenAiAttributes.OperationName, "chat");
                }
            }
            """);

    [Fact]
    public Task ShouldNotReport_WhenActivityNameIsNotGenAi() =>
        VerifyAsync($$"""
            {{ActivityStubs}}

            public class C {
                private static readonly System.Diagnostics.ActivitySource Source = new("test");

                void M() {
                    var activity = Source.StartActivity("http.request");
                }
            }
            """);

    [Fact]
    public Task ShouldNotReport_WhenActivityNameIsNotConstant() =>
        VerifyAsync($$"""
            {{ActivityStubs}}

            public class C {
                private static readonly System.Diagnostics.ActivitySource Source = new("test");

                void M(string name) {
                    var activity = Source.StartActivity(name);
                }
            }
            """);
}
