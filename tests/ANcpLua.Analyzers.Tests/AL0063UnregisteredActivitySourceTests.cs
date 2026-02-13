using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL0063: Detects ActivitySource instances not registered with AddSource().
/// </summary>
public sealed partial class Al0063UnregisteredActivitySourceTests : AnalyzerTest<Al0063UnregisteredActivitySourceAnalyzer> {
    private const string ActivitySourceAndTracerSetup = """
        namespace System.Diagnostics {
            public class ActivitySource {
                public ActivitySource(string name) { }
                public ActivitySource(string name, string? version) { }
            }
        }

        namespace OpenTelemetry.Trace {
            public class TracerProviderBuilder {
                public TracerProviderBuilder AddSource(string name) => this;
            }
        }
        """;

    // ── Should report: no AddSource call ────────────────────────────────

    [Fact]
    public Task ShouldReport_WhenNoAddSourceCall() =>
        VerifyAsync($$"""
            {{ActivitySourceAndTracerSetup}}

            public class C {
                private static readonly System.Diagnostics.ActivitySource Source =
                    new System.Diagnostics.ActivitySource([|"my.service"|]);
            }
            """);

    [Fact]
    public Task ShouldReport_WhenAddSourceUseDifferentName() =>
        VerifyAsync($$"""
            {{ActivitySourceAndTracerSetup}}

            public class Config {
                public void Setup(OpenTelemetry.Trace.TracerProviderBuilder tracing) {
                    tracing.AddSource("other.service");
                }
            }

            public class C {
                private static readonly System.Diagnostics.ActivitySource Source =
                    new System.Diagnostics.ActivitySource([|"my.service"|]);
            }
            """);

    [Fact]
    public Task ShouldReport_MultipleUnregisteredSources() =>
        VerifyAsync($$"""
            {{ActivitySourceAndTracerSetup}}

            public class C {
                private static readonly System.Diagnostics.ActivitySource A =
                    new System.Diagnostics.ActivitySource([|"unregistered.a"|]);
                private static readonly System.Diagnostics.ActivitySource B =
                    new System.Diagnostics.ActivitySource([|"unregistered.b"|]);
            }
            """);

    // ── Should NOT report: AddSource matches ────────────────────────────

    [Fact]
    public Task ShouldNotReport_WhenAddSourceMatchesExactly() =>
        VerifyAsync($$"""
            {{ActivitySourceAndTracerSetup}}

            public class Config {
                public void Setup(OpenTelemetry.Trace.TracerProviderBuilder tracing) {
                    tracing.AddSource("my.service");
                }
            }

            public class C {
                private static readonly System.Diagnostics.ActivitySource Source =
                    new System.Diagnostics.ActivitySource("my.service");
            }
            """);

    [Fact]
    public Task ShouldNotReport_WhenAddSourceMatchesConst() =>
        VerifyAsync($$"""
            {{ActivitySourceAndTracerSetup}}

            public static class Sources {
                public const string Name = "my.service";
            }

            public class Config {
                public void Setup(OpenTelemetry.Trace.TracerProviderBuilder tracing) {
                    tracing.AddSource(Sources.Name);
                }
            }

            public class C {
                private static readonly System.Diagnostics.ActivitySource Source =
                    new System.Diagnostics.ActivitySource(Sources.Name);
            }
            """);

    [Fact]
    public Task ShouldNotReport_WhenAddSourceMatchesCrossFile() =>
        VerifyAsync($$"""
            {{ActivitySourceAndTracerSetup}}

            // Simulates cross-file: AddSource in one class, new ActivitySource in another
            public class Startup {
                public void ConfigureTracing(OpenTelemetry.Trace.TracerProviderBuilder tracing) {
                    tracing.AddSource("qyl.gen_ai");
                }
            }

            public class Instrumentation {
                private static readonly System.Diagnostics.ActivitySource Source =
                    new System.Diagnostics.ActivitySource("qyl.gen_ai");
            }
            """);

    // ── Wildcard matching ───────────────────────────────────────────────

    [Fact]
    public Task ShouldNotReport_WhenWildcardPatternMatches() =>
        VerifyAsync($$"""
            {{ActivitySourceAndTracerSetup}}

            public class Config {
                public void Setup(OpenTelemetry.Trace.TracerProviderBuilder tracing) {
                    tracing.AddSource("OpenAI.*");
                }
            }

            public class C {
                private static readonly System.Diagnostics.ActivitySource Source =
                    new System.Diagnostics.ActivitySource("OpenAI.Chat");
            }
            """);

    [Fact]
    public Task ShouldNotReport_WhenMultipleWildcardsOneMatches() =>
        VerifyAsync($$"""
            {{ActivitySourceAndTracerSetup}}

            public class Config {
                public void Setup(OpenTelemetry.Trace.TracerProviderBuilder tracing) {
                    tracing.AddSource("Azure.AI.OpenAI.*");
                    tracing.AddSource("Anthropic.*");
                }
            }

            public class C {
                private static readonly System.Diagnostics.ActivitySource Source =
                    new System.Diagnostics.ActivitySource("Anthropic.Client");
            }
            """);

    [Fact]
    public Task ShouldReport_WhenWildcardDoesNotMatch() =>
        VerifyAsync($$"""
            {{ActivitySourceAndTracerSetup}}

            public class Config {
                public void Setup(OpenTelemetry.Trace.TracerProviderBuilder tracing) {
                    tracing.AddSource("OpenAI.*");
                }
            }

            public class C {
                private static readonly System.Diagnostics.ActivitySource Source =
                    new System.Diagnostics.ActivitySource([|"Anthropic.Client"|]);
            }
            """);

    // ── Edge cases ──────────────────────────────────────────────────────

    [Fact]
    public Task ShouldNotReport_WhenSourceNameIsNotConstant() =>
        VerifyAsync($$"""
            {{ActivitySourceAndTracerSetup}}

            public class C {
                private static readonly string Name = "my.service";
                private static readonly System.Diagnostics.ActivitySource Source =
                    new System.Diagnostics.ActivitySource(Name);
            }
            """);

    [Fact]
    public Task ShouldNotReport_WhenNotActivitySourceType() =>
        VerifyAsync("""
            public class ActivitySource {
                public ActivitySource(string name) { }
            }

            public class C {
                private static readonly ActivitySource Source = new ActivitySource("my.service");
            }
            """);

    [Fact]
    public Task ShouldNotReport_RegisteredWithVersionConstructor() =>
        VerifyAsync($$"""
            {{ActivitySourceAndTracerSetup}}

            public class Config {
                public void Setup(OpenTelemetry.Trace.TracerProviderBuilder tracing) {
                    tracing.AddSource("my.service");
                }
            }

            public class C {
                private static readonly System.Diagnostics.ActivitySource Source =
                    new System.Diagnostics.ActivitySource("my.service", "1.0.0");
            }
            """);

    [Fact]
    public Task ShouldReport_OnlyUnregisteredWhenMixed() =>
        VerifyAsync($$"""
            {{ActivitySourceAndTracerSetup}}

            public class Config {
                public void Setup(OpenTelemetry.Trace.TracerProviderBuilder tracing) {
                    tracing.AddSource("registered.source");
                }
            }

            public class C {
                private static readonly System.Diagnostics.ActivitySource Registered =
                    new System.Diagnostics.ActivitySource("registered.source");
                private static readonly System.Diagnostics.ActivitySource Unregistered =
                    new System.Diagnostics.ActivitySource([|"unregistered.source"|]);
            }
            """);

    [Fact]
    public Task ShouldReport_WhenBareWildcardPattern() =>
        VerifyAsync($$"""
            {{ActivitySourceAndTracerSetup}}

            public class Config {
                public void Setup(OpenTelemetry.Trace.TracerProviderBuilder tracing) {
                    tracing.AddSource(".*");
                }
            }

            public class C {
                private static readonly System.Diagnostics.ActivitySource Source =
                    new System.Diagnostics.ActivitySource([|"my.service"|]);
            }
            """);

    [Fact]
    public Task ShouldNotReport_WhenActivitySourceNameIsNonConstant() =>
        VerifyAsync($$"""
            {{ActivitySourceAndTracerSetup}}

            public class C {
                private static string GetName() => "my.service";
                private static readonly System.Diagnostics.ActivitySource Source =
                    new System.Diagnostics.ActivitySource(GetName());
            }
            """);

    [Fact]
    public Task ShouldReport_WhenForeachOverInstanceField() =>
        VerifyAsync($$"""
            {{ActivitySourceAndTracerSetup}}

            public class Config {
                private readonly string[] Sources = new[] { "my.service" };

                public void Setup(OpenTelemetry.Trace.TracerProviderBuilder tracing) {
                    foreach (var source in Sources)
                        tracing.AddSource(source);
                }
            }

            public class C {
                private static readonly System.Diagnostics.ActivitySource Source =
                    new System.Diagnostics.ActivitySource([|"my.service"|]);
            }
            """);

    [Fact]
    public Task ShouldNotReport_DuplicateRegistrations() =>
        VerifyAsync($$"""
            {{ActivitySourceAndTracerSetup}}

            public class Config {
                public void Setup(OpenTelemetry.Trace.TracerProviderBuilder tracing) {
                    tracing.AddSource("my.service");
                    tracing.AddSource("my.service");
                }
            }

            public class C {
                private static readonly System.Diagnostics.ActivitySource Source =
                    new System.Diagnostics.ActivitySource("my.service");
            }
            """);

    // ── Foreach-over-array resolution ─────────────────────────────────

    [Fact]
    public Task ShouldNotReport_WhenRegisteredViaForeachOverArray() =>
        VerifyAsync($$"""
            {{ActivitySourceAndTracerSetup}}

            public class Config {
                private static readonly string[] Sources = new[] { "my.service", "other.service" };

                public void Setup(OpenTelemetry.Trace.TracerProviderBuilder tracing) {
                    foreach (var source in Sources)
                        tracing.AddSource(source);
                }
            }

            public class C {
                private static readonly System.Diagnostics.ActivitySource Source =
                    new System.Diagnostics.ActivitySource("my.service");
            }
            """);

    [Fact]
    public Task ShouldNotReport_WhenRegisteredViaForeachOverCollectionExpression() =>
        VerifyAsync($$"""
            {{ActivitySourceAndTracerSetup}}

            public class Config {
                private static readonly string[] Sources = ["my.service", "other.service"];

                public void Setup(OpenTelemetry.Trace.TracerProviderBuilder tracing) {
                    foreach (var source in Sources)
                        tracing.AddSource(source);
                }
            }

            public class C {
                private static readonly System.Diagnostics.ActivitySource Source =
                    new System.Diagnostics.ActivitySource("my.service");
            }
            """);

    [Fact]
    public Task ShouldNotReport_WhenForeachArrayIncludesConstReference() =>
        VerifyAsync($$"""
            {{ActivitySourceAndTracerSetup}}

            public static class ActivitySources {
                public const string GenAi = "qyl.gen_ai";
            }

            public class Config {
                private static readonly string[] Sources = [ActivitySources.GenAi, "OpenAI.*"];

                public void Setup(OpenTelemetry.Trace.TracerProviderBuilder tracing) {
                    foreach (var source in Sources)
                        tracing.AddSource(source);
                }
            }

            public class C {
                private static readonly System.Diagnostics.ActivitySource Source =
                    new System.Diagnostics.ActivitySource(ActivitySources.GenAi);
            }
            """);
}
