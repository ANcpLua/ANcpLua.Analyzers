using ANcpLua.Analyzers.Analyzers;
using Basic.Reference.Assemblies;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL1106: Missing Health Checks.
/// </summary>
public sealed partial class Al1106MissingHealthChecksTests {
    private const string Stubs =
        """
        using Microsoft.Extensions.DependencyInjection;
        using Microsoft.AspNetCore.Builder;
        using Microsoft.Extensions.Hosting;
        namespace Microsoft.Extensions.DependencyInjection {
            public interface IServiceCollection { }
            public class ServiceCollection : IServiceCollection { }
            public interface IHealthChecksBuilder { }
            public static class HealthCheckServiceCollectionExtensions {
                public static IHealthChecksBuilder AddHealthChecks(this IServiceCollection services) => null!;
            }
        }
        namespace Microsoft.AspNetCore.Builder {
            public class WebApplication {
                public static WebApplicationBuilder CreateBuilder(string[] args = null) => new();
            }
            public class WebApplicationBuilder {
                public Microsoft.Extensions.DependencyInjection.IServiceCollection Services { get; } = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
                public WebApplication Build() => new();
            }
        }
        namespace Microsoft.Extensions.Hosting {
            public class Host {
                public static IHostBuilder CreateDefaultBuilder(string[] args = null) => null!;
            }
            public interface IHostBuilder { }
        }
        """;

    private static Task VerifyAsync(string source) {
        var test = new CSharpAnalyzerTest<Al1106MissingHealthChecksAnalyzer, DefaultVerifier> {
            TestCode = (Stubs + "\n" + source).ReplaceLineEndings(),
            ReferenceAssemblies = new ReferenceAssemblies("net10.0"),
            MarkupOptions = MarkupOptions.UseFirstDescriptor
        };
        test.TestState.AdditionalReferences.AddRange(Net100.References.All);
        return test.RunAsync();
    }

    [Fact]
    public Task ShouldReportWhenWebApplicationBuilderWithoutHealthChecks() =>
        VerifyAsync(
            """
            public class Program {
                public static void Main() {
                    var builder = {|AL1106:WebApplication.CreateBuilder()|};
                    var app = builder.Build();
                }
            }
            """);

    [Fact]
    public Task ShouldReportWhenHostCreateDefaultBuilderWithoutHealthChecks() =>
        VerifyAsync(
            """
            public class Program {
                public static void Main() {
                    var host = {|AL1106:Host.CreateDefaultBuilder()|};
                }
            }
            """);

    [Fact]
    public Task ShouldNotReportWhenHealthChecksConfigured() =>
        VerifyAsync(
            """
            public class Program {
                public static void Main() {
                    var builder = WebApplication.CreateBuilder();
                    builder.Services.AddHealthChecks();
                    var app = builder.Build();
                }
            }
            """);

    [Fact]
    public Task ShouldNotReportForNonWebApplicationBuilder() =>
        VerifyAsync(
            """
            public class MyBuilder {
                public static MyBuilder CreateBuilder() => new();
            }
            public class Program {
                public static void Main() {
                    var builder = MyBuilder.CreateBuilder();
                }
            }
            """);

    [Fact]
    public Task ShouldNotReportWhenHealthChecksConfiguredInDifferentOrder() =>
        VerifyAsync(
            """
            public class Program {
                public static void Main() {
                    var builder = WebApplication.CreateBuilder();
                    // Health checks added before other configuration
                    builder.Services.AddHealthChecks();
                    builder.Services.ToString(); // Other configuration
                    var app = builder.Build();
                }
            }
            """);

    [Theory]
    [InlineData("Test")]
    [InlineData("Fact")]
    [InlineData("Theory")]
    [InlineData("TestMethod")]
    public Task ShouldNotReportInsideTestMethod(string attribute) =>
        VerifyAsync(
            $$"""
            namespace TestFramework {
                public class {{attribute}}Attribute : System.Attribute { }
            }
            public class MyTests {
                [TestFramework.{{attribute}}]
                public void EndpointTest() {
                    var builder = WebApplication.CreateBuilder();
                    var app = builder.Build();
                }
            }
            """);

    // Aspire-style ServiceDefaults wrapper: Add[Prefix]ServiceDefaults composes AddHealthChecks
    // internally. The analyzer must treat this naming pattern as equivalent to direct registration
    // to avoid false positives on qyl, Aspire samples, and forked service templates.
    [Theory]
    [InlineData("AddServiceDefaults")]
    [InlineData("AddQylServiceDefaults")]
    [InlineData("AddMyAppServiceDefaults")]
    public Task ShouldNotReport_WithServiceDefaultsWrapper(string wrapperName) =>
        VerifyAsync(
            $$"""
            public static class Extensions {
                public static Microsoft.AspNetCore.Builder.WebApplicationBuilder {{wrapperName}}(
                    this Microsoft.AspNetCore.Builder.WebApplicationBuilder builder) => builder;
            }
            public class Program {
                public static void Main() {
                    var builder = WebApplication.CreateBuilder();
                    builder.{{wrapperName}}();
                    var app = builder.Build();
                }
            }
            """);

    [Fact]
    public Task ShouldReport_WhenWrapperNameDoesNotMatchPattern() =>
        VerifyAsync(
            """
            public static class Extensions {
                public static Microsoft.AspNetCore.Builder.WebApplicationBuilder AddMyDefaults(
                    this Microsoft.AspNetCore.Builder.WebApplicationBuilder builder) => builder;
            }
            public class Program {
                public static void Main() {
                    var builder = {|AL1106:WebApplication.CreateBuilder()|};
                    builder.AddMyDefaults();
                    var app = builder.Build();
                }
            }
            """);
}
