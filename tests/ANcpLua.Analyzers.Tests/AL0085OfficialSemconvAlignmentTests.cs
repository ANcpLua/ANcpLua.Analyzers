using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

public sealed partial class Al0085OfficialSemconvAlignmentTests : AnalyzerTest<Al0085InvalidAttributeValueAnalyzer> {
    [Fact]
    public Task ShouldReportDeprecatedProviderValueForCurrentProviderKey() =>
        VerifyAsync("""
            public class Activity {
                public void SetTag(string key, object? value) { }
            }

            public class C {
                void M(Activity activity) {
                    activity.SetTag("gen_ai.provider.name", [|"vertex_ai"|]);
                }
            }
            """);

    [Fact]
    public Task ShouldNotReportCurrentProviderValue() =>
        VerifyAsync("""
            public class Activity {
                public void SetTag(string key, object? value) { }
            }

            public class C {
                void M(Activity activity) {
                    activity.SetTag("gen_ai.provider.name", "gcp.vertex_ai");
                }
            }
            """);

    [Fact]
    public Task ShouldNotReportNewOfficialOperationName() =>
        VerifyAsync("""
            public class Activity {
                public void SetTag(string key, object? value) { }
            }

            public class C {
                void M(Activity activity) {
                    activity.SetTag("gen_ai.operation.name", "invoke_workflow");
                }
            }
            """);
}
