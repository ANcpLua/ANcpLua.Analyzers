using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL0090: Detects OTLP exporter configurations using HTTP protocol without compression.
/// </summary>
/// <remarks>
///     This analyzer requires OpenTelemetry OTLP exporter types to be present.
///     Tests use stubs to simulate the OpenTelemetry.Exporter API.
/// </remarks>
public sealed partial class Al0090UncompressedExportTests : AnalyzerTest<Al0090UncompressedExportAnalyzer> {
    private const string OtlpStubs = """
        namespace OpenTelemetry.Exporter {
            public enum OtlpExportProtocol {
                Grpc = 0,
                HttpProtobuf = 1
            }

            public class OtlpExporterOptions {
                public OtlpExportProtocol Protocol { get; set; }
                public string? Endpoint { get; set; }
                public bool GzipCompression { get; set; }
            }

            public class OtlpExporterOptionsBase {
                public OtlpExportProtocol Protocol { get; set; }
            }
        }

        namespace OpenTelemetry.Trace {
            public abstract class TracerProviderBuilder {
                public TracerProviderBuilder AddOtlpExporter(System.Action<OpenTelemetry.Exporter.OtlpExporterOptions>? configure = null) => this;
                public TracerProviderBuilder UseOtlpExporter(System.Action<OpenTelemetry.Exporter.OtlpExporterOptions>? configure = null) => this;
            }
        }
        """;

    [Theory]
    [InlineData("""
        using System;
        using OpenTelemetry.Exporter;
        using OpenTelemetry.Trace;

        {STUBS}

        public class TestTracerBuilder : TracerProviderBuilder { }

        public class C {
            void M(TestTracerBuilder builder) {
                builder.{|AL0090:AddOtlpExporter|}(options => {
                    options.Protocol = OtlpExportProtocol.HttpProtobuf;
                });
            }
        }
        """)]
    [InlineData("""
        using System;
        using OpenTelemetry.Exporter;
        using OpenTelemetry.Trace;

        {STUBS}

        public class TestTracerBuilder : TracerProviderBuilder { }

        public class C {
            void M(TestTracerBuilder builder) {
                builder.{|AL0090:UseOtlpExporter|}(options => {
                    options.Protocol = OtlpExportProtocol.HttpProtobuf;
                    options.Endpoint = "http://localhost:4318";
                });
            }
        }
        """)]
    public Task ShouldReportHttpProtobufWithoutCompression(string source) =>
        VerifyAsync(source.Replace("{STUBS}", OtlpStubs, StringComparison.Ordinal));

    [Theory]
    [InlineData("""
        using System;
        using OpenTelemetry.Exporter;
        using OpenTelemetry.Trace;

        {STUBS}

        public class TestTracerBuilder : TracerProviderBuilder { }

        public class C {
            void M(TestTracerBuilder builder) {
                builder.AddOtlpExporter(options => {
                    options.Protocol = OtlpExportProtocol.HttpProtobuf;
                    options.GzipCompression = true;
                });
            }
        }
        """)]
    [InlineData("""
        using System;
        using OpenTelemetry.Exporter;
        using OpenTelemetry.Trace;

        {STUBS}

        public class TestTracerBuilder : TracerProviderBuilder { }

        public class C {
            void M(TestTracerBuilder builder) {
                builder.AddOtlpExporter(options => {
                    options.Protocol = OtlpExportProtocol.Grpc;
                });
            }
        }
        """)]
    [InlineData("""
        using System;
        using OpenTelemetry.Exporter;
        using OpenTelemetry.Trace;

        {STUBS}

        public class TestTracerBuilder : TracerProviderBuilder { }

        public class C {
            void M(TestTracerBuilder builder) {
                // Default (Grpc) - no Protocol explicitly set
                builder.AddOtlpExporter();
            }
        }
        """)]
    public Task ShouldNotReportWhenCompressionEnabledOrGrpc(string source) =>
        VerifyAsync(source.Replace("{STUBS}", OtlpStubs, StringComparison.Ordinal));

    [Fact]
    public Task ShouldNotReportWhenNoOtlpTypes() => VerifyAsync(
        """
        public class C {
            void AddOtlpExporter(System.Action<object> configure) { }
            void M() {
                AddOtlpExporter(options => { });
            }
        }
        """);

    [Fact]
    public Task ShouldReportObjectInitializerWithHttpProtobuf() => VerifyAsync(
        $$"""
        using System;
        using OpenTelemetry.Exporter;
        using OpenTelemetry.Trace;

        {{OtlpStubs}}

        public class TestTracerBuilder : TracerProviderBuilder {
            public TracerProviderBuilder AddOtlpExporter(OtlpExporterOptions options) => this;
        }

        public class C {
            void M(TestTracerBuilder builder) {
                builder.{|AL0090:AddOtlpExporter|}(new OtlpExporterOptions {
                    Protocol = OtlpExportProtocol.HttpProtobuf
                });
            }
        }
        """);

    [Fact]
    public Task ShouldNotReportObjectInitializerWithCompression() => VerifyAsync(
        $$"""
        using System;
        using OpenTelemetry.Exporter;
        using OpenTelemetry.Trace;

        {{OtlpStubs}}

        public class TestTracerBuilder : TracerProviderBuilder {
            public TracerProviderBuilder AddOtlpExporter(OtlpExporterOptions options) => this;
        }

        public class C {
            void M(TestTracerBuilder builder) {
                builder.AddOtlpExporter(new OtlpExporterOptions {
                    Protocol = OtlpExportProtocol.HttpProtobuf,
                    GzipCompression = true
                });
            }
        }
        """);
}
