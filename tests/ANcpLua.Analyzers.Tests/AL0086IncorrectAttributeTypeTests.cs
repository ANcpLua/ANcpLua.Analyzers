using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL0086: Detects OpenTelemetry attributes set with incorrect types.
/// </summary>
public sealed partial class Al0086IncorrectAttributeTypeTests : AnalyzerTest<Al0086IncorrectAttributeTypeAnalyzer> {
    /// <summary>
    ///     Polyfill Activity class with typed SetTag overloads to preserve type information.
    ///     The real System.Diagnostics.Activity.SetTag uses object? which loses type info.
    /// </summary>
    private const string ActivityPolyfill = """
                                            namespace System.Diagnostics {
                                                public class Activity {
                                                    public Activity SetTag(string key, string? value) => this;
                                                    public Activity SetTag(string key, int value) => this;
                                                    public Activity SetTag(string key, long value) => this;
                                                    public Activity SetTag(string key, double value) => this;
                                                    public Activity SetTag(string key, float value) => this;
                                                    public Activity SetTag(string key, bool value) => this;
                                                    public Activity SetTag(string key, string[]? value) => this;
                                                }
                                            }
                                            """;

    [Theory]
    [InlineData("gen_ai.usage.input_tokens", "\"100\"")]
    [InlineData("gen_ai.usage.output_tokens", "\"50\"")]
    [InlineData("http.response.status_code", "\"200\"")]
    [InlineData("db.operation.batch.size", "\"10\"")]
    [InlineData("server.port", "\"8080\"")]
    public Task ShouldReportStringInsteadOfInt(string attributeName, string value) =>
        VerifyAsync($$"""
                      {{ActivityPolyfill}}

                      public class C {
                          void M(System.Diagnostics.Activity activity) {
                              activity.SetTag("{{attributeName}}", [|{{value}}|]);
                          }
                      }
                      """);

    [Theory]
    [InlineData("gen_ai.request.temperature", "\"0.7\"")]
    [InlineData("gen_ai.request.top_p", "\"0.9\"")]
    public Task ShouldReportStringInsteadOfDouble(string attributeName, string value) =>
        VerifyAsync($$"""
                      {{ActivityPolyfill}}

                      public class C {
                          void M(System.Diagnostics.Activity activity) {
                              activity.SetTag("{{attributeName}}", [|{{value}}|]);
                          }
                      }
                      """);

    [Theory]
    [InlineData("gen_ai.usage.input_tokens", "100")]
    [InlineData("gen_ai.usage.output_tokens", "50")]
    [InlineData("http.response.status_code", "200")]
    [InlineData("db.operation.batch.size", "10")]
    [InlineData("server.port", "8080")]
    public Task ShouldNotReportCorrectIntType(string attributeName, string value) =>
        VerifyAsync($$"""
                      {{ActivityPolyfill}}

                      public class C {
                          void M(System.Diagnostics.Activity activity) {
                              activity.SetTag("{{attributeName}}", {{value}});
                          }
                      }
                      """);

    [Theory]
    [InlineData("gen_ai.request.temperature", "0.7")]
    [InlineData("gen_ai.request.temperature", "0.7f")]
    [InlineData("gen_ai.request.top_p", "0.9")]
    [InlineData("gen_ai.request.top_p", "1")]
    public Task ShouldNotReportCorrectDoubleType(string attributeName, string value) =>
        VerifyAsync($$"""
                      {{ActivityPolyfill}}

                      public class C {
                          void M(System.Diagnostics.Activity activity) {
                              activity.SetTag("{{attributeName}}", {{value}});
                          }
                      }
                      """);

    [Theory]
    [InlineData("gen_ai.system")]
    [InlineData("gen_ai.request.model")]
    [InlineData("http.request.method")]
    [InlineData("custom.attribute")]
    public Task ShouldNotReportUnknownAttributes(string attributeName) =>
        VerifyAsync($$"""
                      {{ActivityPolyfill}}

                      public class C {
                          void M(System.Diagnostics.Activity activity) {
                              activity.SetTag("{{attributeName}}", "some_value");
                          }
                      }
                      """);

    [Fact]
    public Task ShouldReportSetAttributeMethod() =>
        VerifyAsync("""
                    public class C {
                        void SetAttribute(string key, string value) { }
                        void M() {
                            SetAttribute("gen_ai.usage.input_tokens", [|"100"|]);
                        }
                    }
                    """);

    [Fact]
    public Task ShouldReportAddTagMethod() =>
        VerifyAsync("""
                    public class C {
                        void AddTag(string key, string value) { }
                        void M() {
                            AddTag("http.response.status_code", [|"404"|]);
                        }
                    }
                    """);

    [Fact]
    public Task ShouldNotReportNonConstantAttributeName() =>
        VerifyAsync($$"""
                      {{ActivityPolyfill}}

                      public class C {
                          void M(System.Diagnostics.Activity activity, string attrName) {
                              activity.SetTag(attrName, "100");
                          }
                      }
                      """);

    [Fact]
    public Task ShouldAcceptLongForIntAttribute() =>
        VerifyAsync($$"""
                      {{ActivityPolyfill}}

                      public class C {
                          void M(System.Diagnostics.Activity activity) {
                              long tokens = 100L;
                              activity.SetTag("gen_ai.usage.input_tokens", tokens);
                          }
                      }
                      """);

    [Fact]
    public Task ShouldAcceptIntForDoubleAttribute() =>
        VerifyAsync($$"""
                      {{ActivityPolyfill}}

                      public class C {
                          void M(System.Diagnostics.Activity activity) {
                              activity.SetTag("gen_ai.request.temperature", 1);
                          }
                      }
                      """);

    private const string ObjectOnlyActivity = """
                                              namespace System.Diagnostics {
                                                  public class Activity {
                                                      public Activity SetTag(string key, object? value) => this;
                                                  }
                                              }
                                              """;

    [Theory]
    [InlineData("gen_ai.usage.input_tokens", "int tokens = 100; activity.SetTag(\"gen_ai.usage.input_tokens\", tokens)")]
    [InlineData("gen_ai.usage.output_tokens", "activity.SetTag(\"gen_ai.usage.output_tokens\", 42)")]
    [InlineData("gen_ai.request.temperature", "activity.SetTag(\"gen_ai.request.temperature\", 0.7)")]
    [InlineData("gen_ai.request.temperature", "activity.SetTag(\"gen_ai.request.temperature\", (double)0.7f)")]
    [InlineData("gen_ai.request.max_tokens", "activity.SetTag(\"gen_ai.request.max_tokens\", (int)100L)")]
    [InlineData("gen_ai.response.finish_reasons", "activity.SetTag(\"gen_ai.response.finish_reasons\", new string[] { \"stop\" })")]
    public Task ShouldNotReportCorrectTypeBoxedToObject(string _, string statement) =>
        VerifyAsync($$"""
                      {{ObjectOnlyActivity}}

                      public class C {
                          void M(System.Diagnostics.Activity activity) {
                              {{statement}};
                          }
                      }
                      """);

    [Theory]
    [InlineData("gen_ai.usage.input_tokens", "\"100\"")]
    [InlineData("gen_ai.request.temperature", "\"0.7\"")]
    public Task ShouldReportIncorrectTypeBoxedToObject(string attributeName, string value) =>
        VerifyAsync($$"""
                      {{ObjectOnlyActivity}}

                      public class C {
                          void M(System.Diagnostics.Activity activity) {
                              activity.SetTag("{{attributeName}}", [|{{value}}|]);
                          }
                      }
                      """);
}
