using ANcpLua.Analyzers.Analyzers;
using Basic.Reference.Assemblies;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL0084: Missing service discovery analyzer.
/// </summary>
public sealed partial class Al0084MissingServiceDiscoveryTests {
    private const string Stubs = """
                                 namespace System.Net.Http {
                                     public class HttpClient {
                                         public Uri? BaseAddress { get; set; }
                                     }
                                 }
                                 """;

    private static Task VerifyAsync(string source) {
        var test = new CSharpAnalyzerTest<Al0084MissingServiceDiscoveryAnalyzer, DefaultVerifier> {
            TestCode = (Stubs + "\n" + source).ReplaceLineEndings(),
            ReferenceAssemblies = new ReferenceAssemblies("net10.0"),
            MarkupOptions = MarkupOptions.UseFirstDescriptor
        };
        test.TestState.AdditionalReferences.AddRange(Net100.References.All);
        return test.RunAsync();
    }

    #region AL0084: Should report hardcoded URLs

    [Theory]
    [InlineData("http://localhost:5000")]
    [InlineData("http://localhost:5001")]
    [InlineData("https://localhost:5001")]
    [InlineData("http://127.0.0.1:5000")]
    [InlineData("http://192.168.1.100:8080")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1054:URI-like parameters should not be strings")]
    public Task ShouldReport_HardcodedLocalUrls(string urlString) =>
        VerifyAsync($$"""
                      public class C {
                          void M(System.Net.Http.HttpClient client) {
                              {|AL0084:client.BaseAddress = new System.Uri("{{urlString}}")|};
                          }
                      }
                      """);

    [Theory]
    [InlineData("http://api.mycompany.com")]
    [InlineData("https://service.internal.company.com")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1054:URI-like parameters should not be strings")]
    public Task ShouldReport_HardcodedDomainUrls(string urlString) =>
        VerifyAsync($$"""
                      public class C {
                          void M(System.Net.Http.HttpClient client) {
                              {|AL0084:client.BaseAddress = new System.Uri("{{urlString}}")|};
                          }
                      }
                      """);

    [Fact]
    public Task ShouldReport_HardcodedUrlWithPort() =>
        VerifyAsync("""
                    public class C {
                        void M(System.Net.Http.HttpClient client) {
                            {|AL0084:client.BaseAddress = new System.Uri("http://myservice:8080")|};
                        }
                    }
                    """);

    #endregion

    #region AL0084: Should not report service discovery URLs

    [Theory]
    [InlineData("http+https://apiservice")]
    [InlineData("https+http://orderservice")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1054:URI-like parameters should not be strings")]
    public Task ShouldNotReport_ServiceDiscoveryUrls(string urlString) =>
        VerifyAsync($$"""
                      public class C {
                          void M(System.Net.Http.HttpClient client) {
                              client.BaseAddress = new System.Uri("{{urlString}}");
                          }
                      }
                      """);

    [Theory]
    [InlineData("http://apiservice")]
    [InlineData("https://orderservice")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1054:URI-like parameters should not be strings")]
    public Task ShouldNotReport_SimpleServiceNames(string urlString) =>
        VerifyAsync($$"""
                      public class C {
                          void M(System.Net.Http.HttpClient client) {
                              client.BaseAddress = new System.Uri("{{urlString}}");
                          }
                      }
                      """);

    #endregion

    #region AL0084: Should not report non-HttpClient contexts

    [Fact]
    public Task ShouldNotReport_NonHttpClientContext() =>
        VerifyAsync("""
                    public class C {
                        void M() {
                            var uri = new System.Uri("http://localhost:5000");
                            System.Console.WriteLine(uri);
                        }
                    }
                    """);

    [Fact]
    public Task ShouldNotReport_VariableAssignment() =>
        VerifyAsync("""
                    public class C {
                        private static readonly System.Uri BaseUri = new System.Uri("http://localhost:5000");
                    }
                    """);

    #endregion
}
