using AnalyzerTestBase = ANcpLua.Roslyn.Utilities.Testing.AnalyzerTest<ANcpLua.Analyzers.Analyzers.Al0115EmptyCatchBlockAnalyzer>;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL0115: Empty catch block swallows exceptions.
/// </summary>
public sealed partial class Al0115EmptyCatchBlockTests : AnalyzerTestBase {
    [Fact]
    public Task ShouldReportEmptyCatchBlock() =>
        VerifyAsync("""
                    using System;

                    public class C {
                        public void M() {
                            try { } [|catch|] { }
                        }
                    }
                    """);

    [Fact]
    public Task ShouldReportEmptyCatchBlockWithExceptionType() =>
        VerifyAsync("""
                    using System;

                    public class C {
                        public void M() {
                            try { } [|catch|] (Exception) { }
                        }
                    }
                    """);

    [Fact]
    public Task ShouldReportEmptyCatchBlockWithExceptionVariable() =>
        VerifyAsync("""
                    using System;

                    public class C {
                        public void M() {
                            try { } [|catch|] (Exception ex) { }
                        }
                    }
                    """);

    [Fact]
    public Task ShouldReportEmptyCatchBlockForOperationCanceledException() =>
        VerifyAsync("""
                    using System;

                    public class C {
                        public void M() {
                            try { } [|catch|] (OperationCanceledException) { }
                        }
                    }
                    """);

    [Fact]
    public Task ShouldNotReportWhenRethrowing() =>
        VerifyAsync("""
                    using System;

                    public class C {
                        public void M() {
                            try { } catch { throw; }
                        }
                    }
                    """);

    [Fact]
    public Task ShouldNotReportWhenLogging() =>
        VerifyAsync("""
                    using System;

                    public class C {
                        public void M() {
                            try { } catch (Exception ex) { Console.WriteLine(ex); }
                        }
                    }
                    """);

    [Fact]
    public Task ShouldNotReportWhenReturning() =>
        VerifyAsync("""
                    using System;

                    public class C {
                        public int M() {
                            try { return 1; } catch { return 0; }
                        }
                    }
                    """);

    [Fact]
    public Task ShouldNotReportWhenAssigning() =>
        VerifyAsync("""
                    using System;

                    public class C {
                        private bool _failed;
                        public void M() {
                            try { } catch { _failed = true; }
                        }
                    }
                    """);

    [Fact]
    public Task ShouldNotReportWhenRethrowingWithException() =>
        VerifyAsync("""
                    using System;

                    public class C {
                        public void M() {
                            try { } catch (Exception ex) { throw new InvalidOperationException("wrapped", ex); }
                        }
                    }
                    """);

    [Fact]
    public Task ShouldReportMultipleEmptyCatchBlocks() =>
        VerifyAsync("""
                    using System;

                    public class C {
                        public void M() {
                            try { } [|catch|] (InvalidOperationException) { } [|catch|] (Exception) { }
                        }
                    }
                    """);
}
