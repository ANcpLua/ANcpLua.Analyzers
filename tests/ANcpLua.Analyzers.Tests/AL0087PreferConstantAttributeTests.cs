using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL0087: Prefer constant attribute over string literal for semantic convention names.
/// </summary>
public sealed partial class Al0087PreferConstantAttributeTests : AnalyzerTest<Al0087PreferConstantAttributeAnalyzer> {
    [Theory]
    [InlineData("""
                using System.Diagnostics;

                public class C {
                    void M(Activity activity) {
                        activity.SetTag([|"gen_ai.provider.name"|], "openai");
                    }
                }
                """)]
    [InlineData("""
                using System.Diagnostics;

                public class C {
                    void M(Activity activity) {
                        activity.SetTag([|"gen_ai.request.model"|], "gpt-4");
                    }
                }
                """)]
    [InlineData("""
                using System.Diagnostics;

                public class C {
                    void M(Activity activity) {
                        activity.SetTag([|"gen_ai.operation.name"|], "chat");
                    }
                }
                """)]
    [InlineData("""
                using System.Diagnostics;

                public class C {
                    void M(Activity activity) {
                        activity.SetTag([|"gen_ai.usage.input_tokens"|], 100);
                    }
                }
                """)]
    [InlineData("""
                using System.Diagnostics;

                public class C {
                    void M(Activity activity) {
                        activity.SetTag([|"gen_ai.usage.output_tokens"|], 50);
                    }
                }
                """)]
    public Task ShouldReportGenAiAttributes(string source) => VerifyAsync(source);

    [Theory]
    [InlineData("""
                using System.Diagnostics;

                public class C {
                    void M(Activity activity) {
                        activity.SetTag([|"http.request.method"|], "GET");
                    }
                }
                """)]
    [InlineData("""
                using System.Diagnostics;

                public class C {
                    void M(Activity activity) {
                        activity.SetTag([|"http.response.status_code"|], 200);
                    }
                }
                """)]
    [InlineData("""
                using System.Diagnostics;

                public class C {
                    void M(Activity activity) {
                        activity.SetTag([|"url.full"|], "https://example.com/api");
                    }
                }
                """)]
    [InlineData("""
                using System.Diagnostics;

                public class C {
                    void M(Activity activity) {
                        activity.SetTag([|"server.address"|], "localhost");
                    }
                }
                """)]
    public Task ShouldReportHttpSemanticConventions(string source) => VerifyAsync(source);

    [Theory]
    [InlineData("""
                using System.Diagnostics;

                public class C {
                    void M(Activity activity) {
                        activity.SetTag([|"db.system.name"|], "postgresql");
                    }
                }
                """)]
    [InlineData("""
                using System.Diagnostics;

                public class C {
                    void M(Activity activity) {
                        activity.SetTag([|"db.query.text"|], "SELECT 1");
                    }
                }
                """)]
    [InlineData("""
                using System.Diagnostics;

                public class C {
                    void M(Activity activity) {
                        activity.SetTag([|"error.type"|], "ValidationError");
                    }
                }
                """)]
    public Task ShouldReportDatabaseAndErrorAttributes(string source) => VerifyAsync(source);

    [Theory]
    [InlineData("""
                using System.Collections.Generic;

                public class C {
                    void M() {
                        var tags = new Dictionary<string, object>();
                        tags[[|"gen_ai.provider.name"|]] = "anthropic";
                    }
                }
                """)]
    [InlineData("""
                using System.Collections.Generic;

                public class C {
                    void M() {
                        var attributes = new Dictionary<string, object>();
                        attributes[[|"gen_ai.request.model"|]] = "claude-3";
                    }
                }
                """)]
    public Task ShouldReportAttributesInDictionary(string source) => VerifyAsync(source);

    [Theory]
    [InlineData("""
                public class C {
                    void SetAttribute(string key, object value) { }
                    void M() {
                        SetAttribute([|"gen_ai.provider.name"|], "openai");
                    }
                }
                """)]
    [InlineData("""
                public class C {
                    void AddTag(string key, object value) { }
                    void M() {
                        AddTag([|"gen_ai.operation.name"|], "chat");
                    }
                }
                """)]
    public Task ShouldReportAttributesInCustomMethods(string source) => VerifyAsync(source);

    [Theory]
    [InlineData("""
                using System.Diagnostics;

                public class C {
                    void M(Activity activity) {
                        activity.SetTag("custom.attribute", "value");
                    }
                }
                """)]
    [InlineData("""
                using System.Diagnostics;

                public class C {
                    void M(Activity activity) {
                        activity.SetTag("my_app.request_id", "12345");
                    }
                }
                """)]
    [InlineData("""
                using System.Diagnostics;

                public class C {
                    void M(Activity activity) {
                        activity.SetTag("unknown.attribute", "value");
                    }
                }
                """)]
    public Task ShouldNotReportUnknownAttributes(string source) => VerifyAsync(source);

    [Theory]
    [InlineData("""
                public class C {
                    void M() {
                        var x = "gen_ai.provider.name";
                    }
                }
                """)]
    [InlineData("""
                public class C {
                    void Log(string message) { }
                    void M() {
                        Log("gen_ai.provider.name is the attribute name");
                    }
                }
                """)]
    [InlineData("""
                public class C {
                    const string Attr = "gen_ai.provider.name";
                }
                """)]
    public Task ShouldNotReportOutsideTelemetryContext(string source) => VerifyAsync(source);

    [Theory]
    [InlineData("""
                using System.Diagnostics;

                public static class GenAiAttributes {
                    public const string ProviderName = "gen_ai.provider.name";
                }

                public class C {
                    void M(Activity activity) {
                        activity.SetTag(GenAiAttributes.ProviderName, "openai");
                    }
                }
                """)]
    public Task ShouldNotReportWhenConstantAlreadyUsed(string source) => VerifyAsync(source);

    [Fact]
    public Task ShouldNotReportDeprecatedGenAiKeyEvenInTelemetryContext() =>
        VerifyAsync("""
            using System.Diagnostics;

            public class C {
                void M(Activity activity) {
                    activity.SetTag("gen_ai.system", "openai");
                }
            }
            """);
}
