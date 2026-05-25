using AnalyzerTestBase = ANcpLua.Roslyn.Utilities.Testing.AnalyzerTest<ANcpLua.Analyzers.Analyzers.Al1307FireAndForgetTaskAnalyzer>;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL1307: Avoid fire-and-forget task discard.
/// </summary>
public sealed partial class Al1307FireAndForgetTaskTests : AnalyzerTestBase {
    [Fact]
    public Task ShouldReportDiscardedTaskRun() =>
        VerifyAsync("""
                    using System.Threading.Tasks;

                    public class C {
                        public void M() {
                            [|_ = Task.Run(() => { })|];
                        }
                    }
                    """);

    [Fact]
    public Task ShouldReportDiscardedAsyncMethodReturningTask() =>
        VerifyAsync("""
                    using System.Threading.Tasks;

                    public class C {
                        public void M() {
                            [|_ = DoWorkAsync()|];
                        }

                        private static Task DoWorkAsync() => Task.CompletedTask;
                    }
                    """);

    [Fact]
    public Task ShouldReportDiscardedTaskOfT() =>
        VerifyAsync("""
                    using System.Threading.Tasks;

                    public class C {
                        public void M() {
                            [|_ = Task.FromResult(42)|];
                        }
                    }
                    """);

    [Fact]
    public Task ShouldReportDiscardedValueTask() =>
        VerifyAsync("""
                    using System.Threading.Tasks;

                    public class C {
                        public void M() {
                            [|_ = GetValueAsync()|];
                        }

                        private static ValueTask GetValueAsync() => default;
                    }
                    """);

    [Fact]
    public Task ShouldReportDiscardedValueTaskOfT() =>
        VerifyAsync("""
                    using System.Threading.Tasks;

                    public class C {
                        public void M() {
                            [|_ = GetValueAsync()|];
                        }

                        private static ValueTask<int> GetValueAsync() => default;
                    }
                    """);

    [Fact]
    public Task ShouldNotReportTaskStoredInVariable() =>
        VerifyAsync("""
                    using System.Threading.Tasks;

                    public class C {
                        public void M() {
                            var t = Task.Run(() => { });
                        }
                    }
                    """);

    [Fact]
    public Task ShouldNotReportAwaitedTask() =>
        VerifyAsync("""
                    using System.Threading.Tasks;

                    public class C {
                        public async Task M() {
                            await Task.Run(() => { });
                        }
                    }
                    """);

    [Fact]
    public Task ShouldNotReportDiscardedNonTaskInvocation() =>
        VerifyAsync("""
                    using System.Threading.Tasks;

                    public class C {
                        public void M() {
                            _ = "hello".ToString();
                        }
                    }
                    """);

    [Fact]
    public Task ShouldNotReportDiscardedIntResult() =>
        VerifyAsync("""
                    using System.Threading.Tasks;

                    public class C {
                        public void M() {
                            _ = GetValue();
                        }

                        private static int GetValue() => 42;
                    }
                    """);
}
