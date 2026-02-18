using AnalyzerTestBase = ANcpLua.Roslyn.Utilities.Testing.AnalyzerTest<ANcpLua.Analyzers.Analyzers.Al0106AvoidTaskRunInAspNetCoreAnalyzer>;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL0106: Avoid Task.Run in ASP.NET Core request handlers.
/// </summary>
public sealed partial class Al0106AvoidTaskRunInAspNetCoreTests : AnalyzerTestBase {
    private const string AspNetCoreStubs = """
                                           namespace Microsoft.AspNetCore.Mvc {
                                               public abstract class ControllerBase { }
                                               public abstract class Controller : ControllerBase { }
                                           }
                                           namespace Microsoft.AspNetCore.Mvc.RazorPages {
                                               public abstract class PageModel { }
                                           }
                                           """;

    [Fact]
    public Task ShouldReportTaskRunInControllerAction() =>
        VerifyAsync($$"""
                      using System;
                      using System.Threading.Tasks;

                      {{AspNetCoreStubs}}

                      public class MyController : Microsoft.AspNetCore.Mvc.Controller {
                          public async Task<int> GetData() {
                              return await [|Task.Run(() => 42)|];
                          }
                      }
                      """);

    [Fact]
    public Task ShouldReportTaskRunInControllerBaseAction() =>
        VerifyAsync($$"""
                      using System;
                      using System.Threading.Tasks;

                      {{AspNetCoreStubs}}

                      public class MyApiController : Microsoft.AspNetCore.Mvc.ControllerBase {
                          public async Task<string> Get() {
                              return await [|Task.Run(() => "hello")|];
                          }
                      }
                      """);

    [Fact]
    public Task ShouldReportTaskRunInRazorPageHandler() =>
        VerifyAsync($$"""
                      using System;
                      using System.Threading.Tasks;

                      {{AspNetCoreStubs}}

                      public class IndexModel : Microsoft.AspNetCore.Mvc.RazorPages.PageModel {
                          public async Task OnGetAsync() {
                              await [|Task.Run(() => { })|];
                          }
                      }
                      """);

    [Fact]
    public Task ShouldNotReportInNonControllerClass() =>
        VerifyAsync($$"""
                      using System;
                      using System.Threading.Tasks;

                      {{AspNetCoreStubs}}

                      public class MyService {
                          public async Task DoWork() {
                              await Task.Run(() => { });
                          }
                      }
                      """);

    [Fact]
    public Task ShouldNotReportInPrivateControllerMethod() =>
        VerifyAsync($$"""
                      using System;
                      using System.Threading.Tasks;

                      {{AspNetCoreStubs}}

                      public class MyController : Microsoft.AspNetCore.Mvc.Controller {
                          private async Task<int> InternalWork() {
                              return await Task.Run(() => 42);
                          }
                      }
                      """);

    [Fact]
    public Task ShouldNotReportNonRazorPageHandlerMethod() =>
        VerifyAsync($$"""
                      using System;
                      using System.Threading.Tasks;

                      {{AspNetCoreStubs}}

                      public class IndexModel : Microsoft.AspNetCore.Mvc.RazorPages.PageModel {
                          private async Task LoadData() {
                              await Task.Run(() => { });
                          }
                      }
                      """);

    [Fact]
    public Task ShouldNotReportWithoutAspNetCoreTypes() =>
        VerifyAsync("""
                    using System;
                    using System.Threading.Tasks;

                    public class MyService {
                        public async Task DoWork() {
                            await Task.Run(() => { });
                        }
                    }
                    """);

    [Fact]
    public Task ShouldNotReportInsideLocalFunctionInControllerAction() =>
        VerifyAsync($$"""
                      using System;
                      using System.Threading.Tasks;

                      {{AspNetCoreStubs}}

                      public class MyController : Microsoft.AspNetCore.Mvc.Controller {
                          public async Task<int> GetData() {
                              return await Helper();

                              async Task<int> Helper() {
                                  return await Task.Run(() => 42);
                              }
                          }
                      }
                      """);

    [Fact]
    public Task ShouldNotReportInProtectedControllerMethod() =>
        VerifyAsync($$"""
                      using System;
                      using System.Threading.Tasks;

                      {{AspNetCoreStubs}}

                      public class MyController : Microsoft.AspNetCore.Mvc.Controller {
                          protected async Task<int> HelperMethod() {
                              return await Task.Run(() => 42);
                          }
                      }
                      """);

    [Fact]
    public Task ShouldNotReportPublicNonHandlerMethodInPageModel() =>
        VerifyAsync($$"""
                      using System;
                      using System.Threading.Tasks;

                      {{AspNetCoreStubs}}

                      public class IndexModel : Microsoft.AspNetCore.Mvc.RazorPages.PageModel {
                          public async Task LoadData() {
                              await Task.Run(() => { });
                          }
                      }
                      """);
}
