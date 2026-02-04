using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL0076: Detects when AddServiceDefaults() is called but AddOpenTelemetry() is missing.
/// </summary>
public sealed partial class Al0076MissingOTelConfigurationTests : AnalyzerTest<Al0076MissingOTelConfigurationAnalyzer> {
    [Theory]
    [InlineData("AddServiceDefaults")]
    [InlineData("AddQylServiceDefaults")]
    [InlineData("ConfigureOpenTelemetry")]
    public Task ShouldReportWhenOTelMissing(string serviceDefaultsMethod) => VerifyAsync(
        $$"""
          using Microsoft.Extensions.DependencyInjection;

          namespace Microsoft.Extensions.DependencyInjection {
              public interface IServiceCollection { }
              public static class ServiceExtensions {
                  public static IServiceCollection {{serviceDefaultsMethod}}(this IServiceCollection services) => services;
              }
          }

          public class C {
              void M(IServiceCollection services) {
                  {|AL0076:services.{{serviceDefaultsMethod}}()|};
              }
          }
          """);

    [Theory]
    [InlineData("AddServiceDefaults", "AddOpenTelemetry")]
    [InlineData("AddServiceDefaults", "WithTracing")]
    [InlineData("AddServiceDefaults", "UseOpenTelemetry")]
    [InlineData("AddQylServiceDefaults", "AddOpenTelemetry")]
    [InlineData("ConfigureOpenTelemetry", "WithTracing")]
    public Task ShouldNotReportWhenOTelPresent(string serviceDefaultsMethod, string otelMethod) => VerifyAsync(
        $$"""
          using Microsoft.Extensions.DependencyInjection;

          namespace Microsoft.Extensions.DependencyInjection {
              public interface IServiceCollection { }
              public static class ServiceExtensions {
                  public static IServiceCollection {{serviceDefaultsMethod}}(this IServiceCollection services) => services;
                  public static IServiceCollection {{otelMethod}}(this IServiceCollection services) => services;
              }
          }

          public class C {
              void M(IServiceCollection services) {
                  services.{{serviceDefaultsMethod}}();
                  services.{{otelMethod}}();
              }
          }
          """);

    [Fact]
    public Task ShouldNotReportWhenAddOpenTelemetryCalledFirst() => VerifyAsync(
        """
        using Microsoft.Extensions.DependencyInjection;

        namespace Microsoft.Extensions.DependencyInjection {
            public interface IServiceCollection { }
            public static class ServiceExtensions {
                public static IServiceCollection AddServiceDefaults(this IServiceCollection services) => services;
                public static IServiceCollection AddOpenTelemetry(this IServiceCollection services) => services;
            }
        }

        public class C {
            void M(IServiceCollection services) {
                services.AddOpenTelemetry();
                services.AddServiceDefaults();
            }
        }
        """);

    [Fact]
    public Task ShouldNotReportWhenNoServiceDefaults() => VerifyAsync(
        """
        using Microsoft.Extensions.DependencyInjection;

        namespace Microsoft.Extensions.DependencyInjection {
            public interface IServiceCollection { }
            public static class ServiceExtensions {
                public static IServiceCollection AddSomething(this IServiceCollection services) => services;
            }
        }

        public class C {
            void M(IServiceCollection services) {
                services.AddSomething();
            }
        }
        """);

    [Fact]
    public Task ShouldNotReportForUnrelatedMethod() => VerifyAsync(
        """
        public class C {
            void AddServiceDefaults() { }
            void M() {
                AddServiceDefaults();
            }
        }
        """);

    [Fact]
    public Task ShouldReportOnlyOncePerMethod() => VerifyAsync(
        """
        using Microsoft.Extensions.DependencyInjection;

        namespace Microsoft.Extensions.DependencyInjection {
            public interface IServiceCollection { }
            public static class ServiceExtensions {
                public static IServiceCollection AddServiceDefaults(this IServiceCollection services) => services;
                public static IServiceCollection AddQylServiceDefaults(this IServiceCollection services) => services;
            }
        }

        public class C {
            void M(IServiceCollection services) {
                {|AL0076:services.AddServiceDefaults()|};
                {|AL0076:services.AddQylServiceDefaults()|};
            }
        }
        """);
}
