using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL1214: Use Guard.NotZero instead of if (x == 0) throw ArgumentOutOfRangeException.
/// </summary>
public sealed partial class Al1214UseGuardNotZeroTests : AnalyzerTest<Al1214UseGuardNotZeroAnalyzer> {
    // AL1214 only fires when ANcpLua.Roslyn.Utilities.Guard is present and accessible.
    // Each case appends this stub so the gate is open; the dedicated
    // ShouldNotReportWhenGuardNotReferenced case omits it to assert the gate itself.
    private const string Stub = """
                                namespace ANcpLua.Roslyn.Utilities { internal static class Guard { } }
                                """;

    private static Task Verify(string body) => VerifyAsync($$"""
                                                            {{body}}
                                                            {{Stub}}
                                                            """);

    [Fact]
    public Task ShouldReportForEqualsZero() => Verify("""
        using System;
        public class C {
            void M(int x) {
                [|if|] (x == 0) throw new ArgumentOutOfRangeException(nameof(x));
            }
        }
        """);

    [Fact]
    public Task ShouldReportForZeroEqualsX() => Verify("""
        using System;
        public class C {
            void M(int x) {
                [|if|] (0 == x) throw new ArgumentOutOfRangeException(nameof(x));
            }
        }
        """);

    [Fact]
    public Task ShouldReportForLongType() => Verify("""
        using System;
        public class C {
            void M(long x) {
                [|if|] (x == 0L) throw new ArgumentOutOfRangeException(nameof(x));
            }
        }
        """);

    [Fact]
    public Task ShouldReportForDoubleType() => Verify("""
        using System;
        public class C {
            void M(double x) {
                [|if|] (x == 0.0) throw new ArgumentOutOfRangeException(nameof(x));
            }
        }
        """);

    [Fact]
    public Task ShouldReportForDecimalType() => Verify("""
        using System;
        public class C {
            void M(decimal x) {
                [|if|] (x == 0.0m) throw new ArgumentOutOfRangeException(nameof(x));
            }
        }
        """);

    [Fact]
    public Task ShouldReportForBlockStatement() => Verify("""
        using System;
        public class C {
            void M(int x) {
                [|if|] (x == 0) {
                    throw new ArgumentOutOfRangeException(nameof(x));
                }
            }
        }
        """);

    [Fact]
    public Task ShouldReportForIsPattern() => Verify("""
        using System;
        public class C {
            void M(int x) {
                [|if|] (x is 0) throw new ArgumentOutOfRangeException(nameof(x));
            }
        }
        """);

    [Fact]
    public Task ShouldReportWithMessage() => Verify("""
        using System;
        public class C {
            void M(int x) {
                [|if|] (x == 0) throw new ArgumentOutOfRangeException(nameof(x), "Value cannot be zero.");
            }
        }
        """);

    [Fact]
    public Task ShouldNotReportForNonZeroComparison() => VerifyAsync("""
        using System;
        public class C {
            void M(int x) {
                if (x == 1) throw new ArgumentOutOfRangeException(nameof(x));
            }
        }
        """);

    [Fact]
    public Task ShouldNotReportForOtherExceptionTypes() => VerifyAsync("""
        using System;
        public class C {
            void M(int x) {
                if (x == 0) throw new InvalidOperationException("Value cannot be zero.");
            }
        }
        """);

    [Fact]
    public Task ShouldNotReportForArgumentNullException() => VerifyAsync("""
        using System;
        public class C {
            void M(int x) {
                if (x == 0) throw new ArgumentNullException(nameof(x));
            }
        }
        """);

    [Fact]
    public Task ShouldNotReportForIfWithElse() => VerifyAsync("""
        using System;
        public class C {
            void M(int x) {
                if (x == 0)
                    throw new ArgumentOutOfRangeException(nameof(x));
                else
                    Console.WriteLine(x);
            }
        }
        """);

    [Fact]
    public Task ShouldNotReportForMultipleStatements() => VerifyAsync("""
        using System;
        public class C {
            void M(int x) {
                if (x == 0) {
                    Console.WriteLine("zero");
                    throw new ArgumentOutOfRangeException(nameof(x));
                }
            }
        }
        """);

    [Fact]
    public Task ShouldNotReportForNotEquals() => VerifyAsync("""
        using System;
        public class C {
            void M(int x) {
                if (x != 0) throw new ArgumentOutOfRangeException(nameof(x));
            }
        }
        """);

    [Fact]
    public Task ShouldNotReportForLessThan() => VerifyAsync("""
        using System;
        public class C {
            void M(int x) {
                if (x < 0) throw new ArgumentOutOfRangeException(nameof(x));
            }
        }
        """);

    // The consumer scenario: a project that does not reference ANcpLua.Roslyn.Utilities has no
    // Guard type, so AL1214 must stay silent on correct BCL zero-check patterns.
    [Fact]
    public Task ShouldNotReportWhenGuardNotReferenced() =>
        VerifyAsync("""
                    using System;
                    public class C {
                        void M(int x) {
                            if (x == 0) throw new ArgumentOutOfRangeException(nameof(x));
                        }
                    }
                    """);
}
