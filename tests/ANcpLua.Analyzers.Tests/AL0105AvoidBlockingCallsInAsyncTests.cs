using AnalyzerTestBase = ANcpLua.Roslyn.Utilities.Testing.AnalyzerTest<ANcpLua.Analyzers.Analyzers.Al0105AvoidBlockingCallsInAsyncAnalyzer>;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL0105: Avoid blocking calls in async methods.
/// </summary>
public sealed partial class Al0105AvoidBlockingCallsInAsyncTests : AnalyzerTestBase {
    [Fact]
    public Task ShouldReportTaskResult() =>
        VerifyAsync("""
                    using System.Threading.Tasks;

                    public class C {
                        public async Task M() {
                            var task = Task.FromResult(42);
                            var x = task.[|Result|];
                        }
                    }
                    """);

    [Fact]
    public Task ShouldReportTaskWait() =>
        VerifyAsync("""
                    using System.Threading.Tasks;

                    public class C {
                        public async Task M() {
                            var task = Task.CompletedTask;
                            task.[|Wait|]();
                        }
                    }
                    """);

    [Fact]
    public Task ShouldReportGetAwaiterGetResult() =>
        VerifyAsync("""
                    using System.Threading.Tasks;

                    public class C {
                        public async Task M() {
                            var task = Task.FromResult(42);
                            var x = [|task.GetAwaiter().GetResult|]();
                        }
                    }
                    """);

    [Fact]
    public Task ShouldReportTaskOfTResult() =>
        VerifyAsync("""
                    using System.Threading.Tasks;

                    public class C {
                        public async Task<string> M() {
                            var task = Task.FromResult("hello");
                            var x = task.[|Result|];
                            return x;
                        }
                    }
                    """);

    [Fact]
    public Task ShouldReportInAsyncLambda() =>
        VerifyAsync("""
                    using System;
                    using System.Threading.Tasks;

                    public class C {
                        public void M() {
                            Func<Task> f = async () => {
                                var task = Task.FromResult(42);
                                var x = task.[|Result|];
                            };
                        }
                    }
                    """);

    [Fact]
    public Task ShouldReportInAsyncLocalFunction() =>
        VerifyAsync("""
                    using System.Threading.Tasks;

                    public class C {
                        public void M() {
                            async Task Inner() {
                                var task = Task.FromResult(42);
                                var x = task.[|Result|];
                            }
                        }
                    }
                    """);

    [Fact]
    public Task ShouldNotReportInSyncMethod() =>
        VerifyAsync("""
                    using System.Threading.Tasks;

                    public class C {
                        public void M() {
                            var task = Task.FromResult(42);
                            var x = task.Result;
                        }
                    }
                    """);

    [Fact]
    public Task ShouldNotReportAwaitedTask() =>
        VerifyAsync("""
                    using System.Threading.Tasks;

                    public class C {
                        public async Task M() {
                            var x = await Task.FromResult(42);
                        }
                    }
                    """);

    [Fact]
    public Task ShouldNotReportNonTaskResult() =>
        VerifyAsync("""
                    using System.Threading.Tasks;

                    public class MyClass {
                        public int Result => 42;
                    }

                    public class C {
                        public async Task M() {
                            var obj = new MyClass();
                            var x = obj.Result;
                        }
                    }
                    """);

    [Fact]
    public Task ShouldNotReportInSynchronousTopLevelProgram() =>
        VerifyAsync("""
                    using System.Threading.Tasks;

                    var task = Task.FromResult(42);
                    var x = task.Result;
                    """);

    [Fact]
    public Task ShouldReportInAsyncTopLevelProgram() =>
        VerifyAsync("""
                    using System.Threading.Tasks;

                    var task = Task.FromResult(42);
                    var x = task.[|Result|];
                    await Task.CompletedTask;
                    """);
}
