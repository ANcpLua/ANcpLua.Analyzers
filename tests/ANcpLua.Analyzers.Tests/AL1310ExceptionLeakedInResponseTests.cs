using AnalyzerTestBase = ANcpLua.Roslyn.Utilities.Testing.AnalyzerTest<ANcpLua.Analyzers.Analyzers.Al1310ExceptionLeakedInResponseAnalyzer>;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL1310: Exception details leaked in HTTP response.
/// </summary>
public sealed partial class Al1310ExceptionLeakedInResponseTests : AnalyzerTestBase {
    private const string AspNetStubs = """
                                       namespace Microsoft.AspNetCore.Http {
                                           public static class Results {
                                               public static IResult BadRequest(object? error = null) => null!;
                                               public static IResult Problem(string? detail = null) => null!;
                                               public static IResult StatusCode(int statusCode) => null!;
                                               public static IResult Json(object? data = null) => null!;
                                           }
                                           public static class TypedResults {
                                               public static IResult BadRequest(object? error = null) => null!;
                                               public static IResult Problem(string? detail = null) => null!;
                                               public static IResult StatusCode(int statusCode) => null!;
                                               public static IResult Json(object? data = null) => null!;
                                           }
                                           public interface IResult { }
                                       }
                                       """;

    [Fact]
    public Task ShouldReportExMessageInBadRequest() =>
        VerifyAsync($$"""
                      using System;
                      using Microsoft.AspNetCore.Http;

                      {{AspNetStubs}}

                      public class Api {
                          public IResult Handle() {
                              try { return Results.BadRequest("ok"); }
                              catch (Exception ex) { return Results.BadRequest([|ex.Message|]); }
                          }
                      }
                      """);

    [Fact]
    public Task ShouldReportExToStringInProblem() =>
        VerifyAsync($$"""
                      using System;
                      using Microsoft.AspNetCore.Http;

                      {{AspNetStubs}}

                      public class Api {
                          public IResult Handle() {
                              try { return Results.BadRequest("ok"); }
                              catch (Exception ex) { return Results.Problem([|ex.ToString|]()); }
                          }
                      }
                      """);

    [Fact]
    public Task ShouldReportExStackTraceInBadRequest() =>
        VerifyAsync($$"""
                      using System;
                      using Microsoft.AspNetCore.Http;

                      {{AspNetStubs}}

                      public class Api {
                          public IResult Handle() {
                              try { return Results.BadRequest("ok"); }
                              catch (Exception ex) { return Results.BadRequest([|ex.StackTrace|]); }
                          }
                      }
                      """);

    [Fact]
    public Task ShouldReportInTypedResults() =>
        VerifyAsync($$"""
                      using System;
                      using Microsoft.AspNetCore.Http;

                      {{AspNetStubs}}

                      public class Api {
                          public IResult Handle() {
                              try { return TypedResults.BadRequest("ok"); }
                              catch (Exception ex) { return TypedResults.BadRequest([|ex.Message|]); }
                          }
                      }
                      """);

    [Fact]
    public Task ShouldNotReportGenericErrorMessage() =>
        VerifyAsync($$"""
                      using System;
                      using Microsoft.AspNetCore.Http;

                      {{AspNetStubs}}

                      public class Api {
                          public IResult Handle() {
                              try { return Results.BadRequest("ok"); }
                              catch (Exception ex) { return Results.BadRequest("An error occurred"); }
                          }
                      }
                      """);

    [Fact]
    public Task ShouldNotReportAnonymousObjectError() =>
        VerifyAsync($$"""
                      using System;
                      using Microsoft.AspNetCore.Http;

                      {{AspNetStubs}}

                      public class Api {
                          public IResult Handle() {
                              try { return Results.BadRequest("ok"); }
                              catch (Exception ex) { return Results.BadRequest(new { error = "Something went wrong" }); }
                          }
                      }
                      """);

    [Fact]
    public Task ShouldNotReportExMessageOutsideResultsCall() =>
        VerifyAsync($$"""
                      using System;
                      using Microsoft.AspNetCore.Http;

                      {{AspNetStubs}}

                      public class Api {
                          public void Handle() {
                              try { }
                              catch (Exception ex) { var msg = ex.Message; Console.WriteLine(msg); }
                          }
                      }
                      """);

    [Fact]
    public Task ShouldNotReportWithoutAspNetCoreTypes() =>
        VerifyAsync("""
                    using System;

                    public class Api {
                        public void Handle() {
                            try { }
                            catch (Exception ex) { var msg = ex.Message; }
                        }
                    }
                    """);

    [Fact]
    public Task ShouldReportExMessageInJson() =>
        VerifyAsync($$"""
                      using System;
                      using Microsoft.AspNetCore.Http;

                      {{AspNetStubs}}

                      public class Api {
                          public IResult Handle() {
                              try { return Results.Json("ok"); }
                              catch (Exception ex) { return Results.Json([|ex.Message|]); }
                          }
                      }
                      """);
}
