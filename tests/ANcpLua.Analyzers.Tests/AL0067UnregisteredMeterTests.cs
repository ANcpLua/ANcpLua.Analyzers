using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL0067: Detects Meter instances not registered via AddMeter() anywhere in the compilation.
///     Uses constant propagation so const-string references count as registrations.
/// </summary>
public sealed partial class Al0067UnregisteredMeterTests : AnalyzerTest<Al0067UnregisteredMeterAnalyzer> {
    private const string Polyfill = """
                                    namespace System.Diagnostics.Metrics {
                                        public sealed class Meter {
                                            public Meter(string name) { }
                                            public Meter(string name, string version) { }
                                        }
                                    }

                                    public sealed class MeterProviderBuilder {
                                        public MeterProviderBuilder AddMeter(params string[] names) => this;
                                    }
                                    """;

    [Fact]
    public Task ShouldReportUnregisteredMeter() => VerifyAsync($$"""
        {{Polyfill}}

        public class C {
            public static readonly System.Diagnostics.Metrics.Meter M = {|AL0067:new System.Diagnostics.Metrics.Meter("qyl.orphan", "1.0")|};
        }
        """);

    [Fact]
    public Task ShouldNotReportWhenRegisteredWithLiteral() => VerifyAsync($$"""
        {{Polyfill}}

        public class C {
            public static readonly System.Diagnostics.Metrics.Meter M = new System.Diagnostics.Metrics.Meter("qyl.agent", "1.0");

            public static void Register(MeterProviderBuilder b) {
                b.AddMeter("qyl.agent");
            }
        }
        """);

    [Fact]
    public Task ShouldNotReportWhenRegisteredViaConstReference() => VerifyAsync($$"""
        {{Polyfill}}

        public static class Names {
            public const string Agent = "qyl.agent";
        }

        public class C {
            public static readonly System.Diagnostics.Metrics.Meter M = new System.Diagnostics.Metrics.Meter(Names.Agent, "1.0");

            public static void Register(MeterProviderBuilder b) {
                b.AddMeter(Names.Agent);
            }
        }
        """);

    [Fact]
    public Task ShouldNotReportWhenRegisteredViaParamsWithMultipleNames() => VerifyAsync($$"""
        {{Polyfill}}

        public class C {
            public static readonly System.Diagnostics.Metrics.Meter Agent = new System.Diagnostics.Metrics.Meter("qyl.agent");
            public static readonly System.Diagnostics.Metrics.Meter GenAi = new System.Diagnostics.Metrics.Meter("qyl.genai");

            public static void Register(MeterProviderBuilder b) {
                b.AddMeter("qyl.agent", "qyl.genai");
            }
        }
        """);

    [Fact]
    public Task ShouldReportMeterWhenOnlyOtherMeterRegistered() => VerifyAsync($$"""
        {{Polyfill}}

        public class C {
            public static readonly System.Diagnostics.Metrics.Meter Registered = new System.Diagnostics.Metrics.Meter("qyl.agent");
            public static readonly System.Diagnostics.Metrics.Meter Orphan = {|AL0067:new System.Diagnostics.Metrics.Meter("qyl.orphan")|};

            public static void Register(MeterProviderBuilder b) {
                b.AddMeter("qyl.agent");
            }
        }
        """);
}
