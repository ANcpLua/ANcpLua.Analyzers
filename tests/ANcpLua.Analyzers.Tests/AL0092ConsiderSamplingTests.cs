using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL0092: Detects OpenTelemetry tracing configurations without sampling configured.
/// </summary>
/// <remarks>
///     This analyzer requires OpenTelemetry types to be present.
///     Tests use stubs to simulate the OpenTelemetry API.
/// </remarks>
public sealed partial class Al0092ConsiderSamplingTests : AnalyzerTest<Al0092ConsiderSamplingAnalyzer> {
    private const string OtelStubs = """
        namespace OpenTelemetry.Trace {
            public abstract class TracerProviderBuilder {
                public TracerProviderBuilder WithTracing(System.Action<TracerProviderBuilder> configure) => this;
                public TracerProviderBuilder SetSampler(Sampler sampler) => this;
                public TracerProviderBuilder AddSource(string name) => this;
            }

            public abstract class Sampler { }

            public class AlwaysOnSampler : Sampler { }

            public class AlwaysOffSampler : Sampler { }

            public class ParentBasedSampler : Sampler {
                public ParentBasedSampler(Sampler rootSampler) { }
            }

            public class TraceIdRatioBasedSampler : Sampler {
                public TraceIdRatioBasedSampler(double ratio) { }
            }
        }

        namespace OpenTelemetry {
            public interface IOpenTelemetryBuilder {
                IOpenTelemetryBuilder WithTracing(System.Action<OpenTelemetry.Trace.TracerProviderBuilder> configure);
            }
        }

        public class TestTracerBuilder : OpenTelemetry.Trace.TracerProviderBuilder { }

        public class TestOtelBuilder : OpenTelemetry.IOpenTelemetryBuilder {
            public OpenTelemetry.IOpenTelemetryBuilder WithTracing(System.Action<OpenTelemetry.Trace.TracerProviderBuilder> configure) => this;
        }
        """;

    [Fact]
    public Task ShouldReportWhenNoSamplerConfigured() => VerifyAsync($$"""
        {{OtelStubs}}

        public class C {
            void M(TestOtelBuilder builder) {
                builder.{|AL0092:WithTracing|}(tracing => tracing.AddSource("MyService"));
            }
        }
        """);

    [Fact]
    public Task ShouldReportWhenAlwaysOnSamplerUsed() => VerifyAsync($$"""
        {{OtelStubs}}

        public class C {
            void M(TestOtelBuilder builder) {
                builder.{|AL0092:WithTracing|}(tracing => tracing
                    .AddSource("MyService")
                    .SetSampler(new OpenTelemetry.Trace.AlwaysOnSampler()));
            }
        }
        """);

    [Fact]
    public Task ShouldNotReportWhenParentBasedSamplerUsed() => VerifyAsync($$"""
        {{OtelStubs}}

        public class C {
            void M(TestOtelBuilder builder) {
                builder.WithTracing(tracing => tracing
                    .AddSource("MyService")
                    .SetSampler(new OpenTelemetry.Trace.ParentBasedSampler(
                        new OpenTelemetry.Trace.TraceIdRatioBasedSampler(0.1))));
            }
        }
        """);

    [Fact]
    public Task ShouldNotReportWhenTraceIdRatioBasedSamplerUsed() => VerifyAsync($$"""
        {{OtelStubs}}

        public class C {
            void M(TestOtelBuilder builder) {
                builder.WithTracing(tracing => tracing
                    .AddSource("MyService")
                    .SetSampler(new OpenTelemetry.Trace.TraceIdRatioBasedSampler(0.5)));
            }
        }
        """);

    [Fact]
    public Task ShouldNotReportWhenAlwaysOffSamplerUsed() => VerifyAsync($$"""
        {{OtelStubs}}

        public class C {
            void M(TestOtelBuilder builder) {
                builder.WithTracing(tracing => tracing
                    .AddSource("MyService")
                    .SetSampler(new OpenTelemetry.Trace.AlwaysOffSampler()));
            }
        }
        """);

    [Fact]
    public Task ShouldNotReportWhenNoOtelTypes() => VerifyAsync(
        """
        public class C {
            void WithTracing(System.Action<object> configure) { }
            void M() {
                WithTracing(t => { });
            }
        }
        """);

    [Fact]
    public Task ShouldNotReportForNonTracingMethods() => VerifyAsync($$"""
        {{OtelStubs}}

        namespace OpenTelemetry.Metrics {
            public abstract class MeterProviderBuilder {
                public MeterProviderBuilder WithMetrics(System.Action<MeterProviderBuilder> configure) => this;
            }
        }

        public class TestMeterBuilder : OpenTelemetry.Metrics.MeterProviderBuilder { }

        public class C {
            void M(TestMeterBuilder builder) {
                // WithMetrics is not a tracing method - should not report
                builder.WithMetrics(metrics => { });
            }
        }
        """);
}
