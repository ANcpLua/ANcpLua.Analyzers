using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Analyzers.CodeFixes.CodeFixes;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Code-fix tests for AL0134: replace hardcoded semantic convention string with the
///     typed constant reference.
/// </summary>
public sealed partial class Al0134UseSemanticConventionConstantCodeFixTests
    : CodeFixTest<Al0134UseSemanticConventionConstantAnalyzer, Al0134UseSemanticConventionConstantCodeFixProvider> {
    private const string HttpStub = """
                                    namespace OpenTelemetry.SemanticConventions.Attributes {
                                        public static class HttpAttributes {
                                            public const string AttributeHttpRequestMethod = "http.request.method";
                                        }
                                    }
                                    """;

    [Fact]
    public Task ShouldReplaceLiteralWithTypedConstant() =>
        VerifyAsync(
            $$"""
              using System.Diagnostics;
              using OpenTelemetry.SemanticConventions.Attributes;
              {{HttpStub}}

              public class C {
                  public void M(Activity activity) {
                      activity.SetTag([|"http.request.method"|], "GET");
                  }
              }
              """,
            $$"""
              using System.Diagnostics;
              using OpenTelemetry.SemanticConventions.Attributes;
              {{HttpStub}}

              public class C {
                  public void M(Activity activity) {
                      activity.SetTag(HttpAttributes.AttributeHttpRequestMethod, "GET");
                  }
              }
              """);
}
