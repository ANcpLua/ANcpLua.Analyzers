using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL0061: Detects Activity/Span creation without semantic convention attributes.
/// </summary>
public sealed partial class Al0061ActivityMissingSemconvTests : AnalyzerTest<Al0061ActivityMissingSemconvAnalyzer> {
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

    // ── Should report: no semconv attributes ──────────────────────────────

    [Fact]
    public Task ShouldReport_WhenNoSetTagCalls() =>
        VerifyAsync($$"""
            {{ActivityStubs}}

            public class C {
                private static readonly System.Diagnostics.ActivitySource Source = new("test");

                void M() {
                    var activity = [|Source.StartActivity("gen_ai.chat")|];
                }
            }
            """);

    [Fact]
    public Task ShouldReport_WhenSetTagUsesUnrelatedPrefix() =>
        VerifyAsync($$"""
            {{ActivityStubs}}

            public class C {
                private static readonly System.Diagnostics.ActivitySource Source = new("test");

                void M() {
                    var activity = [|Source.StartActivity("http.request")|];
                    activity?.SetTag("custom.tag", "value");
                }
            }
            """);

    // ── Should NOT report: semconv attributes present ─────────────────────

    [Fact]
    public Task ShouldNotReport_WhenRelevantSemconvPresent() =>
        VerifyAsync($$"""
            {{ActivityStubs}}

            public class C {
                private static readonly System.Diagnostics.ActivitySource Source = new("test");

                void M() {
                    var activity = Source.StartActivity("http.request");
                    activity?.SetTag("http.request.method", "GET");
                }
            }
            """);

    [Fact]
    public Task ShouldNotReport_WhenDeprecatedNetworkSemconvMapsToHttpConvention() =>
        VerifyAsync($$"""
            {{ActivityStubs}}

            public class C {
                private static readonly System.Diagnostics.ActivitySource Source = new("test");

                void M() {
                    var activity = Source.StartActivity("http.request");
                    activity?.SetTag("net.host.name", "example.com");
                }
            }
            """);

    [Fact]
    // `gen_ai.system` is officially deprecated (→ `gen_ai.provider.name` per semconv 1.40)
    // but still clearly GenAI semconv. AL0061 shouldn't double-flag it as "missing semconv"
    // when the author is already migrating — AL0074 owns the migration message.
    public Task ShouldNotReport_WhenDeprecatedGenAiAliasMapsToCurrentConvention() =>
        VerifyAsync($$"""
            {{ActivityStubs}}

            public class C {
                private static readonly System.Diagnostics.ActivitySource Source = new("test");

                void M() {
                    var activity = Source.StartActivity("gen_ai.chat");
                    activity?.SetTag("gen_ai.system", "openai");
                }
            }
            """);

    [Fact]
    public Task ShouldNotReport_WhenSetTagUsesConstField() =>
        VerifyAsync($$"""
            {{ActivityStubs}}

            public static class GenAiAttributes {
                public const string ProviderName = "gen_ai.provider.name";
            }

            public class C {
                private static readonly System.Diagnostics.ActivitySource Source = new("test");

                void M() {
                    var activity = Source.StartActivity("gen_ai.chat");
                    activity?.SetTag(GenAiAttributes.ProviderName, "openai");
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
                }
            }
            """);

    [Fact]
    public Task ShouldNotReport_WhenSetTagAfterNullGuardWithConstField() =>
        VerifyAsync($$"""
            {{ActivityStubs}}

            public static class GenAiAttributes {
                public const string ProviderName = "gen_ai.provider.name";
            }

            public class C {
                private static readonly System.Diagnostics.ActivitySource Source = new("test");

                void M() {
                    var activity = Source.StartActivity("gen_ai.chat");
                    if (activity is null) return;
                    activity.SetTag(GenAiAttributes.ProviderName, "openai");
                }
            }
            """);

    [Fact]
    public Task ShouldNotReport_WhenDbSemconvPresent() =>
        VerifyAsync($$"""
            {{ActivityStubs}}

            public class C {
                private static readonly System.Diagnostics.ActivitySource Source = new("test");

                void M() {
                    var activity = Source.StartActivity("db.query");
                    if (activity is null) return;
                    activity.SetTag("db.system", "postgresql");
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
