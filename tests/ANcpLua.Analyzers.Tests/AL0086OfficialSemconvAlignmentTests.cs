using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

public sealed partial class Al0086OfficialSemconvAlignmentTests : AnalyzerTest<Al0086IncorrectAttributeTypeAnalyzer> {
    [Fact]
    public Task ShouldReportBatchSizeWhenUsingStringInsteadOfInt() =>
        VerifyAsync("""
            public class Activity {
                public void SetTag(string key, object? value) { }
            }

            public class C {
                void M(Activity activity) {
                    activity.SetTag("db.operation.batch.size", [|"10"|]);
                }
            }
            """);

    [Fact]
    public Task ShouldNotReportBatchSizeWhenUsingInt() =>
        VerifyAsync("""
            public class Activity {
                public void SetTag(string key, object? value) { }
            }

            public class C {
                void M(Activity activity) {
                    activity.SetTag("db.operation.batch.size", 10);
                }
            }
            """);

    [Fact]
    public Task ShouldReportRpcResponseStatusCodeWhenUsingIntInsteadOfString() =>
        VerifyAsync("""
            public class Activity {
                public void SetTag(string key, object? value) { }
            }

            public class C {
                void M(Activity activity) {
                    activity.SetTag("rpc.response.status_code", [|14|]);
                }
            }
            """);
}
