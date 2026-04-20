using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL0085: Detects attribute values that violate OTel semantic convention specifications.
/// </summary>
public sealed partial class Al0085InvalidAttributeValueTests : AnalyzerTest<Al0085InvalidAttributeValueAnalyzer> {
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
                                                    public Activity SetTag(string key, bool value) => this;
                                                }
                                            }
                                            """;

    [Theory]
    [InlineData("\"not-a-number\"")]
    [InlineData("99")]
    [InlineData("600")]
    public Task ShouldReportInvalidHttpStatusCode(string value) =>
        VerifyAsync($$"""
                      {{ActivityPolyfill}}

                      public class C {
                          void M(System.Diagnostics.Activity activity) {
                              activity.SetTag("http.response.status_code", [|{{value}}|]);
                          }
                      }
                      """);

    [Theory]
    [InlineData("unknown-provider")]
    [InlineData("gpt4")]
    // OTel semconv 1.40+ moved the provider attribute from `gen_ai.system` to `gen_ai.provider.name`.
    public Task ShouldReportInvalidGenAiSystem(string invalidValue) =>
        VerifyAsync($$"""
                      {{ActivityPolyfill}}

                      public class C {
                          void M(System.Diagnostics.Activity activity) {
                              activity.SetTag("gen_ai.provider.name", [|"{{invalidValue}}"|]);
                          }
                      }
                      """);

    [Theory]
    [InlineData("query")]
    [InlineData("generate")]
    public Task ShouldReportInvalidGenAiOperationName(string invalidValue) =>
        VerifyAsync($$"""
                      {{ActivityPolyfill}}

                      public class C {
                          void M(System.Diagnostics.Activity activity) {
                              activity.SetTag("gen_ai.operation.name", [|"{{invalidValue}}"|]);
                          }
                      }
                      """);

    [Theory]
    [InlineData("INVALID")]
    [InlineData("get")]
    public Task ShouldReportInvalidHttpMethod(string invalidValue) =>
        VerifyAsync($$"""
                      {{ActivityPolyfill}}

                      public class C {
                          void M(System.Diagnostics.Activity activity) {
                              activity.SetTag("http.request.method", [|"{{invalidValue}}"|]);
                          }
                      }
                      """);

    [Theory]
    [InlineData("gen_ai.usage.input_tokens", "-1")]
    [InlineData("gen_ai.usage.output_tokens", "-100")]
    public Task ShouldReportNegativeTokenCounts(string attributeName, string value) =>
        VerifyAsync($$"""
                      {{ActivityPolyfill}}

                      public class C {
                          void M(System.Diagnostics.Activity activity) {
                              activity.SetTag("{{attributeName}}", [|{{value}}|]);
                          }
                      }
                      """);

    [Theory]
    [InlineData("200")]
    [InlineData("404")]
    [InlineData("500")]
    public Task ShouldNotReportValidHttpStatusCode(string value) =>
        VerifyAsync($$"""
                      {{ActivityPolyfill}}

                      public class C {
                          void M(System.Diagnostics.Activity activity) {
                              activity.SetTag("http.response.status_code", {{value}});
                          }
                      }
                      """);

    [Theory]
    [InlineData("openai")]
    [InlineData("anthropic")]
    [InlineData("vertex_ai")]
    public Task ShouldNotReportValidGenAiSystem(string validValue) =>
        VerifyAsync($$"""
                      {{ActivityPolyfill}}

                      public class C {
                          void M(System.Diagnostics.Activity activity) {
                              activity.SetTag("gen_ai.system", "{{validValue}}");
                          }
                      }
                      """);

    [Theory]
    [InlineData("chat")]
    [InlineData("embeddings")]
    [InlineData("text_completion")]
    public Task ShouldNotReportValidGenAiOperationName(string validValue) =>
        VerifyAsync($$"""
                      {{ActivityPolyfill}}

                      public class C {
                          void M(System.Diagnostics.Activity activity) {
                              activity.SetTag("gen_ai.operation.name", "{{validValue}}");
                          }
                      }
                      """);

    [Theory]
    [InlineData("GET")]
    [InlineData("POST")]
    [InlineData("_OTHER")]
    public Task ShouldNotReportValidHttpMethod(string validValue) =>
        VerifyAsync($$"""
                      {{ActivityPolyfill}}

                      public class C {
                          void M(System.Diagnostics.Activity activity) {
                              activity.SetTag("http.request.method", "{{validValue}}");
                          }
                      }
                      """);

    [Theory]
    [InlineData("gen_ai.usage.input_tokens", "0")]
    [InlineData("gen_ai.usage.output_tokens", "100")]
    public Task ShouldNotReportValidTokenCounts(string attributeName, string value) =>
        VerifyAsync($$"""
                      {{ActivityPolyfill}}

                      public class C {
                          void M(System.Diagnostics.Activity activity) {
                              activity.SetTag("{{attributeName}}", {{value}});
                          }
                      }
                      """);

    [Theory]
    [InlineData("custom.attribute", "any-value")]
    [InlineData("my.app.operation", "custom-operation")]
    public Task ShouldNotReportUnknownAttributes(string attributeName, string value) =>
        VerifyAsync($$"""
                      {{ActivityPolyfill}}

                      public class C {
                          void M(System.Diagnostics.Activity activity) {
                              activity.SetTag("{{attributeName}}", "{{value}}");
                          }
                      }
                      """);

    [Fact]
    public Task ShouldNotReportForNonConstantValue() => VerifyAsync($$"""
        {{ActivityPolyfill}}

        public class C {
            void M(System.Diagnostics.Activity activity, int statusCode) {
                activity.SetTag("http.response.status_code", statusCode);
            }
        }
        """);

    [Fact]
    // OTel semconv 1.40+ moved the gRPC status code into the unified `rpc.response.status_code`
    // attribute. The replacement is typed as string (e.g. "DEADLINE_EXCEEDED"); AL0085 flags
    // empty / whitespace-only values.
    public Task ShouldReportInvalidRpcResponseStatusCode() => VerifyAsync($$"""
        {{ActivityPolyfill}}

        public class C {
            void M(System.Diagnostics.Activity activity) {
                activity.SetTag("rpc.response.status_code", [|""|]);
            }
        }
        """);

    [Theory]
    [InlineData("invalid")]
    [InlineData("file")]
    public Task ShouldReportInvalidUrlScheme(string invalidValue) =>
        VerifyAsync($$"""
                      {{ActivityPolyfill}}

                      public class C {
                          void M(System.Diagnostics.Activity activity) {
                              activity.SetTag("url.scheme", [|"{{invalidValue}}"|]);
                          }
                      }
                      """);

    [Theory]
    [InlineData("http")]
    [InlineData("https")]
    public Task ShouldNotReportValidUrlScheme(string validValue) =>
        VerifyAsync($$"""
                      {{ActivityPolyfill}}

                      public class C {
                          void M(System.Diagnostics.Activity activity) {
                              activity.SetTag("url.scheme", "{{validValue}}");
                          }
                      }
                      """);
}
