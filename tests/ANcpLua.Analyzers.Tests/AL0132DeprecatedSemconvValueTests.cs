using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

public sealed partial class Al0132DeprecatedSemconvValueTests : AnalyzerTest<Al0132DeprecatedSemconvValueAnalyzer> {
    [Theory]
    [InlineData("""
                public class Activity {
                    public void SetTag(string key, object? value) { }
                }

                public class C {
                    void M(Activity activity) {
                        activity.SetTag("gen_ai.system", [|"vertex_ai"|]);
                    }
                }
                """)]
    [InlineData("""
                public class Activity {
                    public void SetTag(string key, object? value) { }
                }

                public class C {
                    void M(Activity activity) {
                        activity.SetTag("messaging.operation.type", [|"publish"|]);
                    }
                }
                """)]
    [InlineData("""
                public class Activity {
                    public void SetTag(string key, object? value) { }
                }

                public class C {
                    void M(Activity activity) {
                        activity.SetTag("cloud.platform", [|"azure_aks"|]);
                    }
                }
                """)]
    public Task ShouldReportDeprecatedSemanticConventionValues(string source) => VerifyAsync(source);

    [Theory]
    [InlineData("""
                public class Activity {
                    public void SetTag(string key, object? value) { }
                }

                public class C {
                    void M(Activity activity) {
                        activity.SetTag("gen_ai.system", "gcp.vertex_ai");
                    }
                }
                """)]
    [InlineData("""
                public class C {
                    void M() {
                        var value = "vertex_ai";
                    }
                }
                """)]
    public Task ShouldNotReportCurrentOrNonTelemetryValues(string source) => VerifyAsync(source);
}
