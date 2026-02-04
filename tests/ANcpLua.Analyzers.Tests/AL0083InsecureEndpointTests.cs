using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL0083: Insecure endpoint.
///     Warns when HTTP endpoints are used where HTTPS is expected.
/// </summary>
public sealed partial class Al0083InsecureEndpointTests : AnalyzerTest<Al0083InsecureEndpointAnalyzer> {
    private const string UriPolyfill = """
                                       namespace System {
                                           public class Uri {
                                               public Uri(string uriString) { }
                                           }
                                       }
                                       """;

    private const string ConfigurationPolyfill = """
                                                 namespace MyApp {
                                                     public class ClientConfig {
                                                         public string? Endpoint { get; set; }
                                                         public string? ServiceUrl { get; set; }
                                                         public string? ApiUrl { get; set; }
                                                     }
                                                 }
                                                 """;

    [Fact]
    public Task ShouldReportHttpEndpointInPropertyAssignment() =>
        VerifyAsync($$"""
                      {{ConfigurationPolyfill}}

                      public class C {
                          void M() {
                              var config = new MyApp.ClientConfig();
                              config.Endpoint = [|"http://api.example.com/v1"|];
                          }
                      }
                      """);

    [Fact]
    public Task ShouldReportHttpEndpointInServiceUrl() =>
        VerifyAsync($$"""
                      {{ConfigurationPolyfill}}

                      public class C {
                          void M() {
                              var config = new MyApp.ClientConfig();
                              config.ServiceUrl = [|"http://service.example.com"|];
                          }
                      }
                      """);

    [Fact]
    public Task ShouldReportHttpEndpointInApiUrl() =>
        VerifyAsync($$"""
                      {{ConfigurationPolyfill}}

                      public class C {
                          void M() {
                              var config = new MyApp.ClientConfig();
                              config.ApiUrl = [|"http://api.example.com"|];
                          }
                      }
                      """);

    [Fact]
    public Task ShouldReportHttpInUriConstructor() =>
        VerifyAsync($$"""
                      {{UriPolyfill}}

                      public class C {
                          void M() {
                              var uri = new System.Uri([|"http://external.example.com"|]);
                          }
                      }
                      """);

    [Fact]
    public Task ShouldNotReportHttpsEndpoint() =>
        VerifyAsync($$"""
                      {{ConfigurationPolyfill}}

                      public class C {
                          void M() {
                              var config = new MyApp.ClientConfig();
                              config.Endpoint = "https://api.example.com/v1";
                          }
                      }
                      """);

    [Fact]
    public Task ShouldNotReportLocalhostHttp() =>
        VerifyAsync($$"""
                      {{ConfigurationPolyfill}}

                      public class C {
                          void M() {
                              var config = new MyApp.ClientConfig();
                              config.Endpoint = "http://localhost:5000";
                          }
                      }
                      """);

    [Fact]
    public Task ShouldNotReportLoopbackHttp() =>
        VerifyAsync($$"""
                      {{ConfigurationPolyfill}}

                      public class C {
                          void M() {
                              var config = new MyApp.ClientConfig();
                              config.Endpoint = "http://127.0.0.1:5000";
                          }
                      }
                      """);

    [Fact]
    public Task ShouldNotReportIpv6LoopbackHttp() =>
        VerifyAsync($$"""
                      {{ConfigurationPolyfill}}

                      public class C {
                          void M() {
                              var config = new MyApp.ClientConfig();
                              config.Endpoint = "http://[::1]:5000";
                          }
                      }
                      """);

    [Fact]
    public Task ShouldNotReportNonEndpointProperty() =>
        VerifyAsync("""
                    public class C {
                        public string? Name { get; set; }
                        void M() {
                            Name = "http://not-an-endpoint.com";
                        }
                    }
                    """);

    [Fact]
    public Task ShouldReportHttpInUriWithPath() =>
        VerifyAsync($$"""
                      {{UriPolyfill}}

                      public class C {
                          void M() {
                              var uri = new System.Uri([|"http://api.example.com/path/to/resource"|]);
                          }
                      }
                      """);

    [Fact]
    public Task ShouldNotReportLocalhostWithPath() =>
        VerifyAsync($$"""
                      {{UriPolyfill}}

                      public class C {
                          void M() {
                              var uri = new System.Uri("http://localhost/api/v1");
                          }
                      }
                      """);

    [Fact]
    public Task ShouldReportHttpInEndpointParameter() =>
        VerifyAsync("""
                    public class C {
                        void Configure(string endpoint) { }

                        void M() {
                            Configure([|"http://external.example.com"|]);
                        }
                    }
                    """);

    [Fact]
    public Task ShouldNotReportNonUrlString() =>
        VerifyAsync($$"""
                      {{ConfigurationPolyfill}}

                      public class C {
                          void M() {
                              var config = new MyApp.ClientConfig();
                              config.Endpoint = "not-a-url";
                          }
                      }
                      """);
}
