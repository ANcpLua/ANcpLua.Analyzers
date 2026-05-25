using ANcpLua.Analyzers.Analyzers;
using Basic.Reference.Assemblies;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL1105: Missing resilience configuration analyzer.
/// </summary>
public sealed partial class Al1105MissingResilienceConfigurationTests {
    private const string Isc = "IServiceCollection";

    private const string Stubs = """
                                 using Microsoft.Extensions.DependencyInjection;
                                 namespace Microsoft.Extensions.DependencyInjection {
                                     public interface IServiceCollection { }
                                     public interface IHttpClientBuilder { }
                                     public static class HttpClientFactoryServiceCollectionExtensions {
                                         public static IHttpClientBuilder AddHttpClient(this IServiceCollection services) => null!;
                                         public static IHttpClientBuilder AddHttpClient(this IServiceCollection services, string name) => null!;
                                         public static IHttpClientBuilder AddHttpClient<TClient>(this IServiceCollection services) where TClient : class => null!;
                                         public static IHttpClientBuilder AddHttpClient<TClient, TImplementation>(this IServiceCollection services) where TClient : class where TImplementation : class, TClient => null!;
                                     }
                                     public static class HttpClientBuilderExtensions {
                                         public static IHttpClientBuilder AddStandardResilienceHandler(this IHttpClientBuilder builder) => builder;
                                         public static IHttpClientBuilder AddResilienceHandler(this IHttpClientBuilder builder, string name, System.Action<object> configure) => builder;
                                         public static IHttpClientBuilder AddTransientHttpErrorPolicy(this IHttpClientBuilder builder, System.Func<object, object> policy) => builder;
                                         public static IHttpClientBuilder AddPolicyHandler(this IHttpClientBuilder builder, object policy) => builder;
                                         public static IHttpClientBuilder AddStandardHedgingHandler(this IHttpClientBuilder builder) => builder;
                                     }
                                 }
                                 """;

    private static Task VerifyAsync(string source) {
        var test = new CSharpAnalyzerTest<Al1105MissingResilienceConfigurationAnalyzer, DefaultVerifier> {
            TestCode = (Stubs + "\n" + source).ReplaceLineEndings(),
            ReferenceAssemblies = new ReferenceAssemblies("net10.0"),
            MarkupOptions = MarkupOptions.UseFirstDescriptor
        };
        test.TestState.AdditionalReferences.AddRange(Net100.References.All);
        return test.RunAsync();
    }

    #region AL1105: Should Report

    [Fact]
    public Task AL1105_ShouldReport_AddHttpClient_NoResilience() =>
        VerifyAsync($$"""
                      public class Startup {
                          public void ConfigureServices({{Isc}} services) {
                              {|AL1105:services.AddHttpClient("MyClient")|};
                          }
                      }
                      """);

    [Fact]
    public Task AL1105_ShouldReport_AddHttpClient_Generic_NoResilience() =>
        VerifyAsync($$"""
                      public interface IMyClient { }
                      public class MyClient : IMyClient { }
                      public class Startup {
                          public void ConfigureServices({{Isc}} services) {
                              {|AL1105:services.AddHttpClient<IMyClient, MyClient>()|};
                          }
                      }
                      """);

    #endregion

    #region AL1105: Should Not Report

    [Fact]
    public Task AL1105_ShouldNotReport_WithStandardResilienceHandler() =>
        VerifyAsync($$"""
                      public class Startup {
                          public void ConfigureServices({{Isc}} services) {
                              services.AddHttpClient("MyClient")
                                  .AddStandardResilienceHandler();
                          }
                      }
                      """);

    [Fact]
    public Task AL1105_ShouldNotReport_WithResilienceHandler() =>
        VerifyAsync($$"""
                      public class Startup {
                          public void ConfigureServices({{Isc}} services) {
                              services.AddHttpClient("MyClient")
                                  .AddResilienceHandler("custom", _ => { });
                          }
                      }
                      """);

    [Fact]
    public Task AL1105_ShouldNotReport_WithTransientHttpErrorPolicy() =>
        VerifyAsync($$"""
                      public class Startup {
                          public void ConfigureServices({{Isc}} services) {
                              services.AddHttpClient("MyClient")
                                  .AddTransientHttpErrorPolicy(p => p);
                          }
                      }
                      """);

    [Fact]
    public Task AL1105_ShouldNotReport_WithPolicyHandler() =>
        VerifyAsync($$"""
                      public class Startup {
                          public void ConfigureServices({{Isc}} services) {
                              services.AddHttpClient("MyClient")
                                  .AddPolicyHandler(new object());
                          }
                      }
                      """);

    [Fact]
    public Task AL1105_ShouldNotReport_WithStandardHedgingHandler() =>
        VerifyAsync($$"""
                      public class Startup {
                          public void ConfigureServices({{Isc}} services) {
                              services.AddHttpClient("MyClient")
                                  .AddStandardHedgingHandler();
                          }
                      }
                      """);

    #endregion
}
