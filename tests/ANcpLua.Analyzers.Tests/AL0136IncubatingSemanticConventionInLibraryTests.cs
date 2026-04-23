using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL0136: flag incubating semantic-convention usage inside libraries.
/// </summary>
/// <remarks>
///     The test harness compiles the source as a DLL (<see cref="OutputKind.DynamicallyLinkedLibrary"/>)
///     and sets the assembly name so the analyzer's "library project" heuristic triggers.
/// </remarks>
public sealed partial class Al0136IncubatingSemanticConventionInLibraryTests
    : AnalyzerTest<Al0136IncubatingSemanticConventionInLibraryAnalyzer> {
    private const string IncubatingStub = """
                                          namespace OpenTelemetry.SemanticConventions.Incubating.Attributes {
                                              public static class HttpIncubatingAttributes {
                                                  public const string AttributeHttpRoute = "http.route";
                                              }
                                          }
                                          """;

    [Fact]
    public Task ShouldReportIncubatingMemberInLibrary() =>
        VerifyAsync($$"""
                      using System.Diagnostics;
                      using OpenTelemetry.SemanticConventions.Incubating.Attributes;
                      {{IncubatingStub}}

                      public class Instrumentation {
                          public void Record(Activity activity, string route) {
                              activity.SetTag([|HttpIncubatingAttributes.AttributeHttpRoute|], route);
                          }
                      }
                      """);

    [Fact]
    public Task ShouldNotReportIncubatingMemberInsideLocalConstCopy() =>
        VerifyAsync($$"""
                      {{IncubatingStub}}

                      public class Instrumentation {
                          private const string AttributeHttpRoute =
                              OpenTelemetry.SemanticConventions.Incubating.Attributes.HttpIncubatingAttributes.AttributeHttpRoute;
                      }
                      """);

    [Fact]
    public Task ShouldNotReportStableMembers() =>
        VerifyAsync("""
                    using System.Diagnostics;
                    using OpenTelemetry.SemanticConventions.Attributes;

                    namespace OpenTelemetry.SemanticConventions.Attributes {
                        public static class HttpAttributes {
                            public const string AttributeHttpRequestMethod = "http.request.method";
                        }
                    }

                    public class Instrumentation {
                        public void Record(Activity activity) {
                            activity.SetTag(HttpAttributes.AttributeHttpRequestMethod, "GET");
                        }
                    }
                    """);
}
