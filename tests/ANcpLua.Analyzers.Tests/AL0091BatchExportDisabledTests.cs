using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL0091: Detects when SimpleSpanProcessor or SimpleActivityExportProcessor is used
///     instead of batch processors.
/// </summary>
public sealed partial class Al0091BatchExportDisabledTests : AnalyzerTest<Al0091BatchExportDisabledAnalyzer> {
    [Theory]
    [InlineData("SimpleSpanProcessor")]
    [InlineData("SimpleActivityExportProcessor")]
    public Task ShouldReportSimpleProcessor(string processorType) => VerifyAsync(
        $$"""
          namespace OpenTelemetry.Trace {
              public class BaseExporter { }
              public class {{processorType}} {
                  public {{processorType}}(BaseExporter exporter) { }
              }
          }

          public class C {
              void M() {
                  var exporter = new OpenTelemetry.Trace.BaseExporter();
                  var processor = {|AL0091:new OpenTelemetry.Trace.{{processorType}}(exporter)|};
              }
          }
          """);

    [Theory]
    [InlineData("BatchSpanProcessor")]
    [InlineData("BatchActivityExportProcessor")]
    public Task ShouldNotReportBatchProcessor(string processorType) => VerifyAsync(
        $$"""
          namespace OpenTelemetry.Trace {
              public class BaseExporter { }
              public class {{processorType}} {
                  public {{processorType}}(BaseExporter exporter) { }
              }
          }

          public class C {
              void M() {
                  var exporter = new OpenTelemetry.Trace.BaseExporter();
                  var processor = new OpenTelemetry.Trace.{{processorType}}(exporter);
              }
          }
          """);

    [Fact]
    public Task ShouldReportSimpleProcessorWithoutNamespace() => VerifyAsync(
        """
        public class BaseExporter { }
        public class SimpleSpanProcessor {
            public SimpleSpanProcessor(BaseExporter exporter) { }
        }

        public class C {
            void M() {
                var exporter = new BaseExporter();
                var processor = {|AL0091:new SimpleSpanProcessor(exporter)|};
            }
        }
        """);

    [Fact]
    public Task ShouldNotReportUnrelatedTypes() => VerifyAsync(
        """
        public class SomeOtherProcessor {
            public SomeOtherProcessor() { }
        }

        public class C {
            void M() {
                var processor = new SomeOtherProcessor();
            }
        }
        """);

    [Fact]
    public Task ShouldReportInTracerProviderBuilder() => VerifyAsync(
        """
        namespace OpenTelemetry.Trace {
            public class BaseExporter { }
            public class SimpleSpanProcessor {
                public SimpleSpanProcessor(BaseExporter exporter) { }
            }
            public class TracerProviderBuilder {
                public TracerProviderBuilder AddProcessor(SimpleSpanProcessor processor) => this;
            }
        }

        public class C {
            void M(OpenTelemetry.Trace.TracerProviderBuilder builder) {
                var exporter = new OpenTelemetry.Trace.BaseExporter();
                builder.AddProcessor({|AL0091:new OpenTelemetry.Trace.SimpleSpanProcessor(exporter)|});
            }
        }
        """);
}
