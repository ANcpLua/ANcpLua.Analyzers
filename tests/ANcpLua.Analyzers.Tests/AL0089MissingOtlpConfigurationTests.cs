using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL0089: Detects OTLP exporter calls without explicit endpoint configuration.
/// </summary>
public sealed partial class Al0089MissingOtlpConfigurationTests : AnalyzerTest<Al0089MissingOtlpConfigurationAnalyzer> {
    [Theory]
    [InlineData("UseOtlpExporter")]
    [InlineData("AddOtlpExporter")]
    public Task ShouldReportWhenEndpointNotConfigured(string methodName) => VerifyAsync(
        $$"""
          using OpenTelemetry;

          namespace OpenTelemetry {
              public static class OTelExtensions {
                  public static void {{methodName}}(this object builder) { }
              }
          }

          public class C {
              void M() {
                  var builder = new object();
                  {|AL0089:builder.{{methodName}}()|};
              }
          }
          """);

    [Theory]
    [InlineData("UseOtlpExporter")]
    [InlineData("AddOtlpExporter")]
    public Task ShouldNotReportWhenEndpointSetInLambda(string methodName) => VerifyAsync(
        $$"""
          using System;
          using OpenTelemetry;

          namespace OpenTelemetry {
              public class OtlpExporterOptions {
                  public Uri? Endpoint { get; set; }
              }
              public static class OTelExtensions {
                  public static void {{methodName}}(this object builder, Action<OtlpExporterOptions> configure) { }
              }
          }

          public class C {
              void M() {
                  var builder = new object();
                  builder.{{methodName}}(options => options.Endpoint = new Uri("http://localhost:4317"));
              }
          }
          """);

    [Theory]
    [InlineData("UseOtlpExporter")]
    [InlineData("AddOtlpExporter")]
    public Task ShouldNotReportWhenEndpointSetInParenthesizedLambda(string methodName) => VerifyAsync(
        $$"""
          using System;
          using OpenTelemetry;

          namespace OpenTelemetry {
              public class OtlpExporterOptions {
                  public Uri? Endpoint { get; set; }
              }
              public static class OTelExtensions {
                  public static void {{methodName}}(this object builder, Action<OtlpExporterOptions> configure) { }
              }
          }

          public class C {
              void M() {
                  var builder = new object();
                  builder.{{methodName}}((options) => { options.Endpoint = new Uri("http://localhost:4317"); });
              }
          }
          """);

    [Fact]
    public Task ShouldNotReportWhenUriPassedAsSecondArgument() => VerifyAsync(
        """
        using System;
        using OpenTelemetry;

        namespace OpenTelemetry {
            public enum OtlpExportProtocol { Grpc, HttpProtobuf }
            public static class OTelExtensions {
                public static void UseOtlpExporter(this object builder, OtlpExportProtocol protocol, Uri endpoint) { }
            }
        }

        public class C {
            void M() {
                var builder = new object();
                builder.UseOtlpExporter(OtlpExportProtocol.Grpc, new Uri("http://collector:4317"));
            }
        }
        """);

    [Fact]
    public Task ShouldNotReportWhenNamedEndpointArgumentProvided() => VerifyAsync(
        """
        using System;
        using OpenTelemetry;

        namespace OpenTelemetry {
            public static class OTelExtensions {
                public static void AddOtlpExporter(this object builder, Uri? endpoint = null) { }
            }
        }

        public class C {
            void M() {
                var builder = new object();
                builder.AddOtlpExporter(endpoint: new Uri("http://localhost:4317"));
            }
        }
        """);

    [Fact]
    public Task ShouldNotReportWhenEnvironmentVariableSetBefore() => VerifyAsync(
        """
        using System;
        using OpenTelemetry;

        namespace OpenTelemetry {
            public static class OTelExtensions {
                public static void UseOtlpExporter(this object builder) { }
            }
        }

        public class C {
            void M() {
                Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", "http://collector:4317");
                var builder = new object();
                builder.UseOtlpExporter();
            }
        }
        """);

    [Fact]
    public Task ShouldReportWhenEnvironmentVariableSetAfter() => VerifyAsync(
        """
        using System;
        using OpenTelemetry;

        namespace OpenTelemetry {
            public static class OTelExtensions {
                public static void UseOtlpExporter(this object builder) { }
            }
        }

        public class C {
            void M() {
                var builder = new object();
                {|AL0089:builder.UseOtlpExporter()|};
                Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", "http://collector:4317");
            }
        }
        """);

    [Fact]
    public Task ShouldNotReportForUnrelatedMethods() => VerifyAsync(
        """
        public class C {
            void UseOtlpExporter() { }
            void M() {
                UseOtlpExporter();
            }
        }
        """);

    [Fact]
    public Task ShouldNotReportWhenOptionsObjectCreationHasEndpoint() => VerifyAsync(
        """
        using System;
        using OpenTelemetry;

        namespace OpenTelemetry {
            public class OtlpExporterOptions {
                public Uri? Endpoint { get; set; }
            }
            public static class OTelExtensions {
                public static void AddOtlpExporter(this object builder, OtlpExporterOptions options) { }
            }
        }

        public class C {
            void M() {
                var builder = new object();
                builder.AddOtlpExporter(new OtlpExporterOptions { Endpoint = new Uri("http://localhost:4317") });
            }
        }
        """);

    [Fact]
    public Task ShouldReportEachUnconfiguredExporter() => VerifyAsync(
        """
        using OpenTelemetry;

        namespace OpenTelemetry {
            public static class OTelExtensions {
                public static void UseOtlpExporter(this object builder) { }
                public static void AddOtlpExporter(this object builder) { }
            }
        }

        public class C {
            void M() {
                var builder = new object();
                {|AL0089:builder.UseOtlpExporter()|};
                {|AL0089:builder.AddOtlpExporter()|};
            }
        }
        """);
}
