using AnalyzerTestBase = ANcpLua.Roslyn.Utilities.Testing.AnalyzerTest<ANcpLua.Analyzers.Analyzers.Al0104PreferAwaitUsingAnalyzer>;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL0104: Prefer 'await using' for IAsyncDisposable.
/// </summary>
public sealed partial class Al0104PreferAwaitUsingTests : AnalyzerTestBase {
    private const string AsyncDisposableStub = """
                                               public class AsyncStream : IAsyncDisposable, IDisposable {
                                                   public ValueTask DisposeAsync() => default;
                                                   public void Dispose() { }
                                               }
                                               """;

    [Fact]
    public Task ShouldReportUsingDeclarationInAsyncMethod() =>
        VerifyAsync($$"""
                      using System;
                      using System.Threading.Tasks;

                      {{AsyncDisposableStub}}

                      public class C {
                          public async Task M() {
                              [|using|] var s = new AsyncStream();
                          }
                      }
                      """);

    [Fact]
    public Task ShouldReportUsingStatementInAsyncMethod() =>
        VerifyAsync($$"""
                      using System;
                      using System.Threading.Tasks;

                      {{AsyncDisposableStub}}

                      public class C {
                          public async Task M() {
                              [|using|] (var s = new AsyncStream()) {
                              }
                          }
                      }
                      """);

    [Fact]
    public Task ShouldReportUsingStatementWithExpressionInAsyncMethod() =>
        VerifyAsync($$"""
                      using System;
                      using System.Threading.Tasks;

                      {{AsyncDisposableStub}}

                      public class C {
                          public async Task M() {
                              var s = new AsyncStream();
                              [|using|] (s) {
                              }
                          }
                      }
                      """);

    [Fact]
    public Task ShouldReportInAsyncLambda() =>
        VerifyAsync($$"""
                      using System;
                      using System.Threading.Tasks;

                      {{AsyncDisposableStub}}

                      public class C {
                          public void M() {
                              Func<Task> f = async () => {
                                  [|using|] var s = new AsyncStream();
                              };
                          }
                      }
                      """);

    [Fact]
    public Task ShouldReportInAsyncLocalFunction() =>
        VerifyAsync($$"""
                      using System;
                      using System.Threading.Tasks;

                      {{AsyncDisposableStub}}

                      public class C {
                          public void M() {
                              async Task Inner() {
                                  [|using|] var s = new AsyncStream();
                              }
                          }
                      }
                      """);

    [Fact]
    public Task ShouldNotReportAlreadyAwaitUsing() =>
        VerifyAsync($$"""
                      using System;
                      using System.Threading.Tasks;

                      {{AsyncDisposableStub}}

                      public class C {
                          public async Task M() {
                              await using var s = new AsyncStream();
                          }
                      }
                      """);

    [Fact]
    public Task ShouldNotReportInSyncMethod() =>
        VerifyAsync($$"""
                      using System;
                      using System.Threading.Tasks;

                      {{AsyncDisposableStub}}

                      public class C {
                          public void M() {
                              using var s = new AsyncStream();
                          }
                      }
                      """);

    [Fact]
    public Task ShouldNotReportNonAsyncDisposable() =>
        VerifyAsync("""
                    using System;
                    using System.Threading.Tasks;

                    public class SyncOnly : IDisposable {
                        public void Dispose() { }
                    }

                    public class C {
                        public async Task M() {
                            using var s = new SyncOnly();
                        }
                    }
                    """);

    [Fact]
    public Task ShouldNotReportInSynchronousTopLevelProgram() =>
        VerifyAsync($$"""
                      using System;
                      using System.Threading.Tasks;

                      {{AsyncDisposableStub}}

                      using var s = new AsyncStream();
                      """);

    [Fact]
    public Task ShouldReportInAsyncTopLevelProgram() =>
        VerifyAsync($$"""
                      using System;
                      using System.Threading.Tasks;

                      {{AsyncDisposableStub}}

                      [|using|] var s = new AsyncStream();
                      await Task.CompletedTask;
                      """);
}
