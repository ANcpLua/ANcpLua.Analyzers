using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL0027: Avoid legacy JSON library.
///     Warns on usage of types from the Newtonsoft.Json namespace.
/// </summary>
public sealed partial class Al0027AnalyzerTests : AnalyzerTest<Al0027AvoidNewtonsoftJsonAnalyzer> {
    private const string NewtonsoftJsonPolyfill = """
                                                  namespace Newtonsoft.Json {
                                                      public static class JsonConvert {
                                                          public static string SerializeObject(object value) => "";
                                                          public static T DeserializeObject<T>(string value) => default!;
                                                      }
                                                      public class JsonSerializerSettings { }
                                                  }
                                                  namespace Newtonsoft.Json.Linq {
                                                      public class JObject {
                                                          public static JObject Parse(string json) => null!;
                                                      }
                                                      public class JArray { }
                                                  }
                                                  """;

    private const string SystemTextJsonPolyfill = """
                                                  namespace System.Text.Json {
                                                      public static class JsonSerializer {
                                                          public static string Serialize<T>(T value) => "";
                                                          public static T Deserialize<T>(string json) => default!;
                                                      }
                                                  }
                                                  """;

    [Fact]
    public Task ShouldReportJsonConvertUsage() =>
        VerifyAsync($$"""
                      {{SystemTextJsonPolyfill}}
                      {{NewtonsoftJsonPolyfill}}

                      public class C {
                          void M() {
                              var json = [|Newtonsoft.Json.JsonConvert.SerializeObject(new { })|];
                          }
                      }
                      """);

    [Fact]
    public Task ShouldReportJObjectCreation() =>
        VerifyAsync($$"""
                      {{SystemTextJsonPolyfill}}
                      {{NewtonsoftJsonPolyfill}}

                      public class C {
                          void M() {
                              var obj = [|new Newtonsoft.Json.Linq.JObject()|];
                          }
                      }
                      """);

    [Fact]
    public Task ShouldReportSettingsCreation() =>
        VerifyAsync($$"""
                      {{SystemTextJsonPolyfill}}
                      {{NewtonsoftJsonPolyfill}}

                      public class C {
                          void M() {
                              var settings = [|new Newtonsoft.Json.JsonSerializerSettings()|];
                          }
                      }
                      """);

    [Fact]
    public Task ShouldNotReportWhenSystemTextJsonNotAvailable() =>
        VerifyAsync($$"""
                      {{NewtonsoftJsonPolyfill}}

                      public class C {
                          void M() {
                              var json = Newtonsoft.Json.JsonConvert.SerializeObject(new { });
                          }
                      }
                      """, false);

    [Fact]
    public Task ShouldNotReportSystemTextJsonUsage() =>
        VerifyAsync($$"""
                      {{SystemTextJsonPolyfill}}
                      {{NewtonsoftJsonPolyfill}}

                      public class C {
                          void M() {
                              var json = System.Text.Json.JsonSerializer.Serialize(new { });
                          }
                      }
                      """);
}
