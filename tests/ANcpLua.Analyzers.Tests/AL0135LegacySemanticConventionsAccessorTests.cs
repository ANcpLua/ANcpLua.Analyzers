using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL0135: flag legacy aggregated SemanticConventions accessors.
/// </summary>
public sealed partial class Al0135LegacySemanticConventionsAccessorTests
    : AnalyzerTest<Al0135LegacySemanticConventionsAccessorAnalyzer> {
    private const string LegacyAggregatorStub = """
                                                namespace OpenTelemetry.SemanticConventions {
                                                    public static class SemanticConventions {
                                                        public const string AttributeHttpMethod = "http.method";
                                                        public const string AttributeHttpStatusCode = "http.status_code";
                                                    }
                                                }
                                                """;

    private const string LegacyTraceStub = """
                                           namespace OpenTelemetry.Trace {
                                               public static class TraceSemanticConventions {
                                                   public const string AttributeNetPeerName = "net.peer.name";
                                               }
                                           }
                                           """;

    private const string GroupedStub = """
                                       namespace OpenTelemetry.SemanticConventions.Attributes {
                                           public static class HttpAttributes {
                                               public const string AttributeHttpRequestMethod = "http.request.method";
                                           }
                                       }
                                       """;

    [Fact]
    public Task ShouldReportLegacyAggregatorMemberAccess() =>
        VerifyAsync($$"""
                      using System.Diagnostics;
                      using OpenTelemetry.SemanticConventions;
                      {{LegacyAggregatorStub}}

                      public class C {
                          public void M(Activity activity) {
                              activity.SetTag([|SemanticConventions.AttributeHttpMethod|], "GET");
                          }
                      }
                      """);

    [Fact]
    public Task ShouldReportLegacyTraceAccessorMemberAccess() =>
        VerifyAsync($$"""
                      using System.Diagnostics;
                      using OpenTelemetry.Trace;
                      {{LegacyTraceStub}}

                      public class C {
                          public void M(Activity activity) {
                              activity.SetTag([|TraceSemanticConventions.AttributeNetPeerName|], "localhost");
                          }
                      }
                      """);

    [Fact]
    public Task ShouldNotReportGroupedAttributeAccessor() =>
        VerifyAsync($$"""
                      using System.Diagnostics;
                      using OpenTelemetry.SemanticConventions.Attributes;
                      {{GroupedStub}}

                      public class C {
                          public void M(Activity activity) {
                              activity.SetTag(HttpAttributes.AttributeHttpRequestMethod, "GET");
                          }
                      }
                      """);

    [Fact]
    public Task ShouldNotReportWhenPackageAbsent() =>
        VerifyAsync("""
                    using System.Diagnostics;

                    public class C {
                        public void M(Activity activity) {
                            activity.SetTag("http.method", "GET");
                        }
                    }
                    """);
}
