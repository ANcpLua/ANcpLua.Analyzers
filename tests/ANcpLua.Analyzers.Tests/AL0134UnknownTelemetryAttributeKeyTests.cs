using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

public sealed partial class Al0134UnknownTelemetryAttributeKeyTests : AnalyzerTest<Al0134UnknownTelemetryAttributeKeyAnalyzer> {
    [Theory]
    [InlineData("""
                public class Activity {
                    public void SetTag(string key, object? value) { }
                }

                public class C {
                    void M(Activity activity) {
                        activity.SetTag([|"qyl.agent.status"|], "ok");
                    }
                }
                """)]
    [InlineData("""
                public class Activity {
                    public void SetTag(string key, object? value) { }
                }

                public class C {
                    void M(Activity activity) {
                        activity.SetTag([|"Http.Request.Method"|], "GET");
                    }
                }
                """)]
    [InlineData("""
                public class Activity {
                    public void SetTag(string key, object? value) { }
                }

                public class C {
                    void M(Activity activity) {
                        activity.SetTag([|"error.count"|], 1);
                    }
                }
                """)]
    [InlineData("""
                public class Activity {
                    public void SetTag(string key, object? value) { }
                }

                public class C {
                    void M(Activity activity) {
                        activity.SetTag([|"gen_ai.usage.total_tokens"|], 128);
                    }
                }
                """)]
    [InlineData("""
                public class Activity {
                    public void SetTag(string key, object? value) { }
                }

                public class C {
                    void M(Activity activity) {
                        activity.SetTag([|"db.operation.batch_size"|], 10);
                    }
                }
                """)]
    [InlineData("""
                using System.Collections.Generic;

                public class C {
                    void M() {
                        var tags = new Dictionary<string, object>();
                        tags[[|"custom.attribute"|]] = "value";
                    }
                }
                """)]
    public Task ShouldReportUnknownTelemetryKeys(string source) => VerifyAsync(source);

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
                    void M(Activity activity) {
                        activity.SetTag("http.method", "GET");
                        activity.SetTag("http.request.header.content-type", "application/json");
                        activity.SetTag("db.operation.parameter.max_rows", 10);
                        activity.SetTag("process.environment_variable.PATH", "/usr/bin");
                        activity.SetTag("db.elasticsearch.path_parts.index", "orders");
                    }
                }
                """)]
    [InlineData("""
                public class C {
                    void M() {
                        var key = "qyl.agent.status";
                    }
                }
                """)]
    public Task ShouldNotReportOfficialOrNonTelemetryStrings(string source) => VerifyAsync(source);
}
