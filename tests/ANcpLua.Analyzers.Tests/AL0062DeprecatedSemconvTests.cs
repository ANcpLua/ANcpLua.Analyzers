using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

public sealed partial class Al0062DeprecatedSemconvTests : AnalyzerTest<Al0062DeprecatedSemconvAnalyzer> {
    [Theory]
    [InlineData("""
                public class Activity {
                    public void SetTag(string key, object? value) { }
                }

                public class C {
                    void M(Activity activity) {
                        activity.SetTag([|"http.request_content_length_uncompressed"|], 128);
                    }
                }
                """)]
    [InlineData("""
                public class Activity {
                    public void AddTag(string key, object? value) { }
                }

                public class C {
                    void M(Activity activity) {
                        activity.AddTag([|"db.system"|], "postgresql");
                    }
                }
                """)]
    [InlineData("""
                public class Span {
                    public void SetAttribute(string key, object? value) { }
                }

                public class C {
                    void M(Span span) {
                        span.SetAttribute([|"messaging.operation"|], "publish");
                    }
                }
                """)]
    [InlineData("""
                public class Tags {
                    public void Add(string key, object? value) { }
                }

                public class C {
                    void M(Tags tags) {
                        tags.Add([|"rpc.system"|], "grpc");
                    }
                }
                """)]
    [InlineData("""
                public class Activity {
                    public void SetTag(string key, object? value) { }
                }

                public class C {
                    void M(Activity activity) {
                        activity.SetTag([|"HTTP.STATUS_CODE"|], 200);
                    }
                }
                """)]
    public Task ShouldReportDeprecatedSemanticConventionKeys(string source) => VerifyAsync(source);

    [Theory]
    [InlineData("""
                public class Activity {
                    public void SetTag(string key, object? value) { }
                }

                public class C {
                    void M(Activity activity) {
                        activity.SetTag("http.request.method", "GET");
                    }
                }
                """)]
    [InlineData("""
                public class Activity {
                    public void SetTag(string key, object? value) { }
                }

                public class C {
                    void M(Activity activity, string key) {
                        activity.SetTag(key, "GET");
                    }
                }
                """)]
    public Task ShouldNotReportCurrentOrNonConstantKeys(string source) => VerifyAsync(source);
}
