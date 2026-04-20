using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

public sealed partial class Al0074DeprecatedGenAiAttributeTests : AnalyzerTest<Al0074DeprecatedGenAiAttributeAnalyzer> {
    [Theory]
    [InlineData("""
                public class Activity {
                    public void SetTag(string key, object? value) { }
                }

                public class C {
                    void M(Activity activity) {
                        activity.SetTag([|"gen_ai.system"|], "openai");
                    }
                }
                """)]
    [InlineData("""
                public class Activity {
                    public void SetTag(string key, object? value) { }
                }

                public class C {
                    void M(Activity activity) {
                        activity.SetTag([|"gen_ai.usage.prompt_tokens"|], 42);
                    }
                }
                """)]
    [InlineData("""
                public class Activity {
                    public void SetTag(string key, object? value) { }
                }

                public class C {
                    void M(Activity activity) {
                        activity.SetTag([|"gen_ai.openai.request.service_tier"|], "default");
                    }
                }
                """)]
    public Task ShouldReportDeprecatedGenAiKeysInTelemetryContext(string source) => VerifyAsync(source);

    [Theory]
    [InlineData("""
                public class Activity {
                    public void SetTag(string key, object? value) { }
                }

                public class C {
                    void M(Activity activity) {
                        activity.SetTag("gen_ai.provider.name", "openai");
                    }
                }
                """)]
    [InlineData("""
                public class C {
                    void M() {
                        var key = "gen_ai.system";
                    }
                }
                """)]
    public Task ShouldNotReportCurrentOrNonTelemetryStrings(string source) => VerifyAsync(source);
}
