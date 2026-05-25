using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL1214: Use Guard.NotZero instead of if (x == 0) throw ArgumentOutOfRangeException.
/// </summary>
public sealed partial class Al1214UseGuardNotZeroTests : AnalyzerTest<Al1214UseGuardNotZeroAnalyzer> {
    [Fact]
    public Task ShouldReportForEqualsZero() => VerifyAsync("""
        using System;
        public class C {
            void M(int x) {
                [|if|] (x == 0) throw new ArgumentOutOfRangeException(nameof(x));
            }
        }
        """);

    [Fact]
    public Task ShouldReportForZeroEqualsX() => VerifyAsync("""
        using System;
        public class C {
            void M(int x) {
                [|if|] (0 == x) throw new ArgumentOutOfRangeException(nameof(x));
            }
        }
        """);

    [Fact]
    public Task ShouldReportForLongType() => VerifyAsync("""
        using System;
        public class C {
            void M(long x) {
                [|if|] (x == 0L) throw new ArgumentOutOfRangeException(nameof(x));
            }
        }
        """);

    [Fact]
    public Task ShouldReportForDoubleType() => VerifyAsync("""
        using System;
        public class C {
            void M(double x) {
                [|if|] (x == 0.0) throw new ArgumentOutOfRangeException(nameof(x));
            }
        }
        """);

    [Fact]
    public Task ShouldReportForDecimalType() => VerifyAsync("""
        using System;
        public class C {
            void M(decimal x) {
                [|if|] (x == 0.0m) throw new ArgumentOutOfRangeException(nameof(x));
            }
        }
        """);

    [Fact]
    public Task ShouldReportForBlockStatement() => VerifyAsync("""
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
    public Task ShouldReportForIsPattern() => VerifyAsync("""
        using System;
        public class C {
            void M(int x) {
                [|if|] (x is 0) throw new ArgumentOutOfRangeException(nameof(x));
            }
        }
        """);

    [Fact]
    public Task ShouldReportWithMessage() => VerifyAsync("""
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
}
