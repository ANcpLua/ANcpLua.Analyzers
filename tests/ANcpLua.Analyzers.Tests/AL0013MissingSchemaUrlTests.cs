using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL0013: Detects OpenTelemetry configurations that don't set the schema URL.
/// </summary>
/// <remarks>
///     This analyzer requires OpenTelemetry types to be present.
///     Tests use stubs to simulate the OpenTelemetry API.
/// </remarks>
public sealed partial class Al0013MissingSchemaUrlTests : AnalyzerTest<Al0013MissingSchemaUrlAnalyzer> {
    [Theory]
    [InlineData("""
                using System;
                using System.Collections.Generic;

                namespace OpenTelemetry.Trace {
                    public abstract class TracerProviderBuilder {
                        public TracerProviderBuilder ConfigureResource(Action<OpenTelemetry.Resources.ResourceBuilder> configure) => this;
                        public TracerProviderBuilder SetResourceBuilder(OpenTelemetry.Resources.ResourceBuilder builder) => this;
                    }
                }

                namespace OpenTelemetry.Resources {
                    public class ResourceBuilder {
                        public static ResourceBuilder CreateDefault() => new();
                        public ResourceBuilder AddService(string serviceName) => this;
                        public ResourceBuilder AddAttributes(IEnumerable<KeyValuePair<string, object>> attributes) => this;
                    }
                }

                public class TestTracerBuilder : OpenTelemetry.Trace.TracerProviderBuilder { }

                public class C {
                    void M(TestTracerBuilder builder) {
                        builder.{|AL0013:ConfigureResource|}(r => r.AddService("MyService"));
                    }
                }
                """)]
    [InlineData("""
                using System;
                using System.Collections.Generic;

                namespace OpenTelemetry.Trace {
                    public abstract class TracerProviderBuilder {
                        public TracerProviderBuilder ConfigureResource(Action<OpenTelemetry.Resources.ResourceBuilder> configure) => this;
                        public TracerProviderBuilder SetResourceBuilder(OpenTelemetry.Resources.ResourceBuilder builder) => this;
                    }
                }

                namespace OpenTelemetry.Resources {
                    public class ResourceBuilder {
                        public static ResourceBuilder CreateDefault() => new();
                        public ResourceBuilder AddService(string serviceName) => this;
                        public ResourceBuilder AddAttributes(IEnumerable<KeyValuePair<string, object>> attributes) => this;
                    }
                }

                public class TestTracerBuilder : OpenTelemetry.Trace.TracerProviderBuilder { }

                public class C {
                    void M(TestTracerBuilder builder) {
                        builder.{|AL0013:SetResourceBuilder|}(OpenTelemetry.Resources.ResourceBuilder.CreateDefault().AddService("MyService"));
                    }
                }
                """)]
    [InlineData("""
                using System;
                using System.Collections.Generic;

                namespace OpenTelemetry.Metrics {
                    public abstract class MeterProviderBuilder {
                        public MeterProviderBuilder ConfigureResource(Action<OpenTelemetry.Resources.ResourceBuilder> configure) => this;
                    }
                }

                namespace OpenTelemetry.Resources {
                    public class ResourceBuilder {
                        public static ResourceBuilder CreateDefault() => new();
                        public ResourceBuilder AddService(string serviceName) => this;
                        public ResourceBuilder AddAttributes(IEnumerable<KeyValuePair<string, object>> attributes) => this;
                    }
                }

                public class TestMeterBuilder : OpenTelemetry.Metrics.MeterProviderBuilder { }

                public class C {
                    void M(TestMeterBuilder builder) {
                        builder.{|AL0013:ConfigureResource|}(r => r.AddService("MyService"));
                    }
                }
                """)]
    [InlineData("""
                using System;
                using System.Collections.Generic;

                namespace OpenTelemetry.Logs {
                    public abstract class LoggerProviderBuilder {
                        public LoggerProviderBuilder ConfigureResource(Action<OpenTelemetry.Resources.ResourceBuilder> configure) => this;
                    }
                }

                namespace OpenTelemetry.Resources {
                    public class ResourceBuilder {
                        public static ResourceBuilder CreateDefault() => new();
                        public ResourceBuilder AddService(string serviceName) => this;
                        public ResourceBuilder AddAttributes(IEnumerable<KeyValuePair<string, object>> attributes) => this;
                    }
                }

                public class TestLoggerBuilder : OpenTelemetry.Logs.LoggerProviderBuilder { }

                public class C {
                    void M(TestLoggerBuilder builder) {
                        builder.{|AL0013:ConfigureResource|}(r => r.AddService("MyService"));
                    }
                }
                """)]
    public Task ShouldReportMissingSchemaUrl(string source) => VerifyAsync(source);

    [Theory]
    [InlineData("""
                using System;
                using System.Collections.Generic;

                namespace OpenTelemetry.Trace {
                    public abstract class TracerProviderBuilder {
                        public TracerProviderBuilder ConfigureResource(Action<OpenTelemetry.Resources.ResourceBuilder> configure) => this;
                    }
                }

                namespace OpenTelemetry.Resources {
                    public class ResourceBuilder {
                        public static ResourceBuilder CreateDefault() => new();
                        public ResourceBuilder AddService(string serviceName) => this;
                        public ResourceBuilder AddAttributes(IEnumerable<KeyValuePair<string, object>> attributes) => this;
                    }
                }

                public class TestTracerBuilder : OpenTelemetry.Trace.TracerProviderBuilder { }

                public class C {
                    void M(TestTracerBuilder builder) {
                        builder.ConfigureResource(r => r.AddAttributes(new[] {
                            new KeyValuePair<string, object>("telemetry.schema_url", "https://opentelemetry.io/schemas/1.21.0")
                        }));
                    }
                }
                """)]
    [InlineData("""
                using System;
                using System.Collections.Generic;

                namespace OpenTelemetry.Trace {
                    public abstract class TracerProviderBuilder {
                        public TracerProviderBuilder ConfigureResource(Action<OpenTelemetry.Resources.ResourceBuilder> configure) => this;
                    }
                }

                namespace OpenTelemetry.Resources {
                    public class ResourceBuilder {
                        public static ResourceBuilder CreateDefault() => new();
                        public ResourceBuilder AddService(string serviceName) => this;
                        public ResourceBuilder AddAttributes(IEnumerable<KeyValuePair<string, object>> attributes) => this;
                    }
                }

                public class TestTracerBuilder : OpenTelemetry.Trace.TracerProviderBuilder { }

                public class C {
                    void M(TestTracerBuilder builder) {
                        builder.ConfigureResource(r => r.AddAttributes(new[] {
                            new KeyValuePair<string, object>("schema", "v1")
                        }));
                    }
                }
                """)]
    public Task ShouldNotReportWhenSchemaUrlPresent(string source) => VerifyAsync(source);

    [Fact]
    public Task ShouldNotReportWhenNoOtelTypes() => VerifyAsync(
        """
        public class C {
            void ConfigureResource(System.Action<object> configure) { }
            void M() {
                ConfigureResource(r => { });
            }
        }
        """);
}
