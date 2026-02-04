using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL0093: Detects missing resource attributes in OpenTelemetry configuration.
/// </summary>
public sealed partial class Al0093MissingResourceAttributesTests : AnalyzerTest<Al0093MissingResourceAttributesAnalyzer> {
    [Theory]
    [InlineData("AddOpenTelemetry")]
    [InlineData("UseOpenTelemetry")]
    [InlineData("ConfigureOpenTelemetry")]
    public Task ShouldReportWhenOTelConfiguredWithoutResourceAttributes(string methodName) => VerifyAsync($$"""
        public static class OTelExtensions {
            public static void {{methodName}}(this object builder) { }
        }

        public class C {
            void Configure() {
                var builder = new object();
                builder.[|{{methodName}}|]();
            }
        }
        """);

    [Theory]
    [InlineData("AddOpenTelemetry", "ConfigureResource")]
    [InlineData("AddOpenTelemetry", "AddResource")]
    [InlineData("AddOpenTelemetry", "AddService")]
    [InlineData("AddOpenTelemetry", "SetResourceBuilder")]
    [InlineData("UseOpenTelemetry", "ConfigureResource")]
    [InlineData("ConfigureOpenTelemetry", "AddService")]
    public Task ShouldNotReportWhenResourceConfigured(string otelMethod, string resourceMethod) => VerifyAsync($$"""
        public static class OTelExtensions {
            public static object {{otelMethod}}(this object builder) => builder;
            public static object {{resourceMethod}}(this object builder) => builder;
        }

        public class C {
            void Configure() {
                var builder = new object();
                builder.{{otelMethod}}().{{resourceMethod}}();
            }
        }
        """);

    [Fact]
    public Task ShouldNotReportWhenNoOTelSetup() => VerifyAsync("""
        public class C {
            void Configure() {
                var builder = new object();
                builder.ToString();
            }
        }
        """);

    [Fact]
    public Task ShouldNotReportWhenResourceConfiguredSeparately() => VerifyAsync("""
        public static class OTelExtensions {
            public static void AddOpenTelemetry(this object builder) { }
            public static void ConfigureResource(this object builder) { }
        }

        public class C {
            void Configure() {
                var builder = new object();
                builder.AddOpenTelemetry();
                builder.ConfigureResource();
            }
        }
        """);

    [Fact]
    public Task ShouldReportOnlyOnOTelSetupMethod() => VerifyAsync("""
        public static class OTelExtensions {
            public static void AddOpenTelemetry(this object builder) { }
            public static void SomeOtherMethod(this object builder) { }
        }

        public class C {
            void Configure() {
                var builder = new object();
                builder.[|AddOpenTelemetry|]();
                builder.SomeOtherMethod();
            }
        }
        """);
}
