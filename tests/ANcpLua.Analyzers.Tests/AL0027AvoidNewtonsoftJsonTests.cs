using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Analyzers.CodeFixes.CodeFixes;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL0027: Avoid legacy JSON library.
///     Warns on usage of types from the Newtonsoft.Json namespace.
/// </summary>
public sealed partial class Al0027AvoidNewtonsoftJsonTests : AnalyzerTest<Al0027AvoidNewtonsoftJsonAnalyzer> {
    private const string NewtonsoftJsonPolyfill = """
                                                  namespace Newtonsoft.Json {
                                                      public static class JsonConvert {
                                                          public static string SerializeObject(object value) => "";
                                                          public static string SerializeObject(object value, JsonSerializerSettings settings) => "";
                                                          public static T DeserializeObject<T>(string value) => default!;
                                                          public static T DeserializeObject<T>(string value, JsonSerializerSettings settings) => default!;
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
    public Task ShouldReportJObjectParse() =>
        VerifyAsync($$"""
                      {{SystemTextJsonPolyfill}}
                      {{NewtonsoftJsonPolyfill}}

                      public class C {
                          void M() {
                              var obj = [|Newtonsoft.Json.Linq.JObject.Parse("{}")|];
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

    [Fact]
    public Task ShouldReportUnsupportedSerializeWithSettingsOverload() =>
        VerifyAsync($$"""
                      {{SystemTextJsonPolyfill}}
                      {{NewtonsoftJsonPolyfill}}

                      public class C {
                          void M() {
                              var json = [|Newtonsoft.Json.JsonConvert.SerializeObject(new { }, new Newtonsoft.Json.JsonSerializerSettings())|];
                          }
                      }
                      """);

    [Fact]
    public Task ShouldReportUnsupportedDeserializeWithSettingsOverload() =>
        VerifyAsync($$"""
                      {{SystemTextJsonPolyfill}}
                      {{NewtonsoftJsonPolyfill}}

                      public class C {
                          void M(string json) {
                              var value = [|Newtonsoft.Json.JsonConvert.DeserializeObject<string>(json, new Newtonsoft.Json.JsonSerializerSettings())|];
                          }
                      }
                      """);
}

/// <summary>
///     Code fix tests for AL0027: prefer System.Text.Json.
/// </summary>
public sealed partial class Al0027AvoidNewtonsoftJsonCodeFixTests
    : CodeFixTest<Al0027AvoidNewtonsoftJsonAnalyzer, Al0027UseSystemTextJsonCodeFixProvider> {
    private const string CodeFixJsonConvertPolyfill = """
                                                  namespace Newtonsoft.Json {
                                                      public static class JsonConvert {
                                                          public static string SerializeObject(object value) => "";
                                                          public static string SerializeObject(object value, JsonSerializerSettings settings) => "";
                                                          public static T DeserializeObject<T>(string value) => default!;
                                                          public static T DeserializeObject<T>(string value, JsonSerializerSettings settings) => default!;
                                                          public static T DeserializeObject<T>(string value, JsonSerializerSettings settings,
                                                              params JsonConverter[] converters) => default!;
                                                          public static T DeserializeObject<T>(string value, params JsonConverter[] converters) => default!;
                                                          public class JsonSerializerSettings { }
                                                          public class JsonConverter { }
                                                      }
                                                  }
                                                  namespace System.Text.Json {
                                                      public static class JsonSerializer {
                                                          public static string Serialize<T>(T value) => "";
                                                          public static T Deserialize<T>(string json) => default!;
                                                      }
                                                  }
                                                  """;

    [Fact]
    public Task ShouldFixSerializeObject() =>
        VerifyAsync(
            $$"""
            {{CodeFixJsonConvertPolyfill}}
            public class C {
                public string M() =>
                    [|Newtonsoft.Json.JsonConvert.SerializeObject(new { })|];
            }
            """,
            $$"""
            {{CodeFixJsonConvertPolyfill}}
            public class C {
                public string M() =>
                    System.Text.Json.JsonSerializer.Serialize(new { });
            }
            """);

    [Fact]
    public Task ShouldFixDeserializeObject() =>
        VerifyAsync(
            $$"""
            {{CodeFixJsonConvertPolyfill}}
            public class C {
                public int M(string json) =>
                    [|Newtonsoft.Json.JsonConvert.DeserializeObject<int>(json)|];
            }
            """,
            $$"""
            {{CodeFixJsonConvertPolyfill}}
            public class C {
                public int M(string json) =>
                    System.Text.Json.JsonSerializer.Deserialize<int>(json);
            }
            """);
}
