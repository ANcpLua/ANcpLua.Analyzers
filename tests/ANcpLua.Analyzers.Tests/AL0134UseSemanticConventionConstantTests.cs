using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL0134: Use OpenTelemetry semantic convention constant instead of hardcoded string.
/// </summary>
/// <remarks>
///     The analyzer builds its catalog from the consumer's compilation via
///     <c>GetTypeByMetadataName</c>, so every test embeds a small stub of the
///     OpenTelemetry.SemanticConventions surface under test. Stubs mirror the
///     real package's namespace/type/member shape (for example
///     <c>OpenTelemetry.SemanticConventions.Attributes.HttpAttributes.AttributeHttpRequestMethod</c>).
/// </remarks>
public sealed partial class Al0134UseSemanticConventionConstantTests : AnalyzerTest<Al0134UseSemanticConventionConstantAnalyzer> {
    private const string HttpStub = """
                                    namespace OpenTelemetry.SemanticConventions.Attributes {
                                        public static class HttpAttributes {
                                            public const string AttributeHttpRequestMethod = "http.request.method";
                                            public const string AttributeHttpResponseStatusCode = "http.response.status_code";
                                        }
                                    }
                                    """;

    private const string ServerStub = """
                                      namespace OpenTelemetry.SemanticConventions.Attributes {
                                          public static class ServerAttributes {
                                              public const string AttributeServerAddress = "server.address";
                                              public const string AttributeServerPort = "server.port";
                                          }
                                      }
                                      """;

    private const string LegacyAggregatorStub = """
                                                namespace OpenTelemetry.SemanticConventions {
                                                    public static class SemanticConventions {
                                                        public const string AttributeDbSystem = "db.system";
                                                    }
                                                }
                                                """;

    [Fact]
    public Task ShouldReportHardcodedHttpMethodInSetTag() =>
        VerifyAsync($$"""
                      using System.Diagnostics;
                      {{HttpStub}}

                      public class C {
                          public void M(Activity activity) {
                              activity.SetTag([|"http.request.method"|], "GET");
                          }
                      }
                      """);

    [Fact]
    public Task ShouldReportHardcodedStatusCodeInSetTag() =>
        VerifyAsync($$"""
                      using System.Diagnostics;
                      {{HttpStub}}

                      public class C {
                          public void M(Activity activity) {
                              activity.SetTag([|"http.response.status_code"|], 200);
                          }
                      }
                      """);

    [Fact]
    public Task ShouldReportHardcodedServerAddressInAddTag() =>
        VerifyAsync($$"""
                      using System.Diagnostics;
                      {{ServerStub}}

                      public class C {
                          public void M(Activity activity) {
                              activity.AddTag([|"server.address"|], "localhost");
                          }
                      }
                      """);

    [Fact]
    public Task ShouldReportLegacyAggregatorConstant() =>
        VerifyAsync($$"""
                      using System.Diagnostics;
                      {{LegacyAggregatorStub}}

                      public class C {
                          public void M(Activity activity) {
                              activity.SetTag([|"db.system"|], "postgresql");
                          }
                      }
                      """);

    [Fact]
    public Task ShouldReportInDictionaryIndexer() =>
        VerifyAsync($$"""
                      using System.Collections.Generic;
                      {{HttpStub}}

                      public class C {
                          public void M() {
                              var tags = new Dictionary<string, object>();
                              tags[[|"http.request.method"|]] = "GET";
                          }
                      }
                      """);

    [Fact]
    public Task ShouldNotReportWhenConstantReferenceAlreadyUsed() =>
        VerifyAsync($$"""
                      using System.Diagnostics;
                      using OpenTelemetry.SemanticConventions.Attributes;
                      {{HttpStub}}

                      public class C {
                          public void M(Activity activity) {
                              activity.SetTag(HttpAttributes.AttributeHttpRequestMethod, "GET");
                          }
                      }
                      """);

    [Fact]
    public Task ShouldNotReportUnknownAttributeKey() =>
        VerifyAsync($$"""
                      using System.Diagnostics;
                      {{HttpStub}}

                      public class C {
                          public void M(Activity activity) {
                              activity.SetTag("my.custom.attribute", "value");
                          }
                      }
                      """);

    [Fact]
    public Task ShouldNotReportWhenPackageNotReferenced() =>
        VerifyAsync("""
                    using System.Diagnostics;

                    public class C {
                        public void M(Activity activity) {
                            activity.SetTag("http.request.method", "GET");
                        }
                    }
                    """);

    [Fact]
    public Task ShouldNotReportInsideConstantDeclaration() =>
        VerifyAsync($$"""
                      {{HttpStub}}

                      public class C {
                          private const string Key = "http.request.method";
                      }
                      """);

    [Fact]
    public Task ShouldNotReportInsideNameof() =>
        VerifyAsync($$"""
                      {{HttpStub}}

                      public class C {
                          public void M() {
                              var literal = "http.request.method";
                              var x = nameof(literal);
                          }
                      }
                      """);

    [Fact]
    public Task ShouldNotReportInTestMethod() =>
        VerifyAsync($$"""
                      using System.Diagnostics;
                      using Xunit;
                      {{HttpStub}}

                      namespace Xunit { public sealed class FactAttribute : System.Attribute { } }

                      public class CTests {
                          [Fact]
                          public void ShouldTag(Activity activity) {
                              activity.SetTag("http.request.method", "GET");
                          }
                      }
                      """);

    [Fact]
    public Task ShouldNotReportOutsideTelemetryContext() =>
        VerifyAsync($$"""
                      {{HttpStub}}

                      public class C {
                          public void Log(string s) { }

                          public void M() {
                              Log("http.request.method");
                          }
                      }
                      """);
}
