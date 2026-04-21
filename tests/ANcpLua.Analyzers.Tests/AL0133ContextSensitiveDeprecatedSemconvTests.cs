using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

public sealed partial class Al0133ContextSensitiveDeprecatedSemconvTests : AnalyzerTest<Al0133ContextSensitiveDeprecatedSemconvAnalyzer> {
    [Theory]
    [InlineData("""
                using System.Collections.Generic;

                public class C {
                    void M() {
                        var tags = new Dictionary<string, object>();
                        tags[[|"http.host"|]] = "example.com";
                    }
                }
                """)]
    [InlineData("""
                using System.Collections.Generic;

                public class C {
                    void M() {
                        var attributes = new Dictionary<string, object>();
                        attributes[[|"rpc.service"|]] = "Greeter";
                    }
                }
                """)]
    [InlineData("""
                public class Activity {
                    public void SetTag(string key, object? value) { }
                }

                public class C {
                    void M(Activity activity) {
                        activity.SetTag([|"gen_ai.prompt"|], "hello");
                    }
                }
                """)]
    [InlineData("""
                public class Activity {
                    public void SetTag(string key, object? value) { }
                }

                public class C {
                    void M(Activity activity) {
                        activity.SetTag([|"rpc.jsonrpc.error_message"|], "oops");
                    }
                }
                """)]
    public Task ShouldReportContextSensitiveDeprecatedAttributes(string source) => VerifyAsync(source);

    [Theory]
    [InlineData("""
                namespace System.Diagnostics {
                    public readonly struct ActivityEvent {
                        public ActivityEvent(string name) { }
                    }
                }

                public class C {
                    void M() {
                        _ = new System.Diagnostics.ActivityEvent([|"event.gen_ai.system.message"|]);
                    }
                }
                """)]
    [InlineData("""
                namespace System.Diagnostics {
                    public readonly struct ActivityEvent {
                        public ActivityEvent(string name) { }
                    }

                    public class Activity {
                        public void AddEvent(ActivityEvent activityEvent) { }
                    }
                }

                public class C {
                    void M(System.Diagnostics.Activity activity) {
                        activity.AddEvent(new System.Diagnostics.ActivityEvent([|"event.rpc.message"|]));
                    }
                }
                """)]
    public Task ShouldReportDeprecatedEventNames(string source) => VerifyAsync(source);

    [Theory]
    [InlineData("""
                using System.Collections.Generic;

                public class C {
                    void M() {
                        var tags = new Dictionary<string, object>();
                        tags["server.address"] = "example.com";
                    }
                }
                """)]
    [InlineData("""
                public class C {
                    void M() {
                        var key = "http.host";
                    }
                }
                """)]
    [InlineData("""
                namespace System.Diagnostics {
                    public readonly struct ActivityEvent {
                        public ActivityEvent(string name) { }
                    }
                }

                public class C {
                    void M() {
                        _ = new System.Diagnostics.ActivityEvent("model.inference");
                    }
                }
                """)]
    // Migration-dictionary initializer: deprecated attribute names as KEYS of a plain
    // Dictionary<string,string> are a legitimate use case (mapping old → new). The rule
    // must only fire when the containing object-creation is itself a telemetry type.
    [InlineData("""
                using System.Collections.Generic;

                public static class SchemaNormalizer {
                    public static readonly Dictionary<string, string> DeprecatedMappings =
                        new() {
                            ["gen_ai.prompt"] = "gen_ai.input.messages",
                            ["gen_ai.completion"] = "gen_ai.output.messages",
                            ["rpc.service"] = "rpc.service.name",
                        };
                }
                """)]
    public Task ShouldNotReportCurrentOrNonTelemetryAttributes(string source) => VerifyAsync(source);
}
