using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Analyzers.CodeFixes.CodeFixes;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL0049: Use Guard.Positive instead of if (x &lt;= 0) throw new ArgumentOutOfRangeException.
/// </summary>
public sealed partial class Al0049UseGuardPositiveTests : AnalyzerTest<Al0049UseGuardPositiveAnalyzer> {
    [Fact]
    public Task ShouldReportForLessThanOrEqualZero() => VerifyAsync("""
        using System;
        public class C {
            void M(int x) {
                [|if|] (x <= 0) throw new ArgumentOutOfRangeException(nameof(x));
            }
        }
        """);

    [Fact]
    public Task ShouldReportForReversedComparison() => VerifyAsync("""
        using System;
        public class C {
            void M(int x) {
                [|if|] (0 >= x) throw new ArgumentOutOfRangeException(nameof(x));
            }
        }
        """);

    [Fact]
    public Task ShouldReportForLong() => VerifyAsync("""
        using System;
        public class C {
            void M(long count) {
                [|if|] (count <= 0) throw new ArgumentOutOfRangeException(nameof(count));
            }
        }
        """);

    [Fact]
    public Task ShouldReportForDouble() => VerifyAsync("""
        using System;
        public class C {
            void M(double value) {
                [|if|] (value <= 0.0) throw new ArgumentOutOfRangeException(nameof(value));
            }
        }
        """);

    [Fact]
    public Task ShouldReportForDecimal() => VerifyAsync("""
        using System;
        public class C {
            void M(decimal amount) {
                [|if|] (amount <= 0m) throw new ArgumentOutOfRangeException(nameof(amount));
            }
        }
        """);

    [Fact]
    public Task ShouldReportForBlockBody() => VerifyAsync("""
        using System;
        public class C {
            void M(int x) {
                [|if|] (x <= 0) {
                    throw new ArgumentOutOfRangeException(nameof(x), "Value must be positive.");
                }
            }
        }
        """);

    [Fact]
    public Task ShouldNotReportForLessThanZero() => VerifyAsync("""
        using System;
        public class C {
            void M(int x) {
                if (x < 0) throw new ArgumentOutOfRangeException(nameof(x));
            }
        }
        """);

    [Fact]
    public Task ShouldNotReportForOtherExceptionTypes() => VerifyAsync("""
        using System;
        public class C {
            void M(int x) {
                if (x <= 0) throw new InvalidOperationException("Value must be positive.");
            }
        }
        """);

    [Fact]
    public Task ShouldNotReportForArgumentNullException() => VerifyAsync("""
        using System;
        public class C {
            void M(int x) {
                if (x <= 0) throw new ArgumentNullException(nameof(x));
            }
        }
        """);

    [Fact]
    public Task ShouldNotReportForNonZeroComparison() => VerifyAsync("""
        using System;
        public class C {
            void M(int x) {
                if (x <= 5) throw new ArgumentOutOfRangeException(nameof(x));
            }
        }
        """);

    [Fact]
    public Task ShouldNotReportForEqualsZero() => VerifyAsync("""
        using System;
        public class C {
            void M(int x) {
                if (x == 0) throw new ArgumentOutOfRangeException(nameof(x));
            }
        }
        """);

    [Fact]
    public Task ShouldNotReportForElseBranch() => VerifyAsync("""
        using System;
        public class C {
            void M(int x) {
                if (x <= 0) throw new ArgumentOutOfRangeException(nameof(x));
                else Console.WriteLine("Valid");
            }
        }
        """);

    [Fact]
    public Task ShouldReportForMemberAccess() => VerifyAsync("""
        using System;
        public class C {
            void M(Options opts) {
                [|if|] (opts.Count <= 0) throw new ArgumentOutOfRangeException(nameof(opts));
            }
        }
        public class Options { public int Count { get; set; } }
        """);

    [Fact]
    public Task ShouldReportForFloat() => VerifyAsync("""
        using System;
        public class C {
            void M(float value) {
                [|if|] (value <= 0.0f) throw new ArgumentOutOfRangeException(nameof(value));
            }
        }
        """);

    [Fact]
    public Task ShouldReportForShort() => VerifyAsync("""
        using System;
        public class C {
            void M(short value) {
                [|if|] (value <= 0) throw new ArgumentOutOfRangeException(nameof(value));
            }
        }
        """);

    [Fact]
    public Task ShouldReportWithMessage() => VerifyAsync("""
        using System;
        public class C {
            void M(int x) {
                [|if|] (x <= 0) throw new ArgumentOutOfRangeException(nameof(x), "Value must be positive.");
            }
        }
        """);

    [Fact]
    public Task ShouldNotReportForMultipleStatements() => VerifyAsync("""
        using System;
        public class C {
            void M(int x) {
                if (x <= 0) {
                    Console.WriteLine("Invalid");
                    throw new ArgumentOutOfRangeException(nameof(x));
                }
            }
        }
        """);
}

public sealed partial class Al0049UseGuardPositiveCodeFixTests
    : CodeFixTest<Al0049UseGuardPositiveAnalyzer, Al0049UseGuardPositiveCodeFixProvider> {
    [Fact]
    public Task ShouldPreserveMemberAccessReceiver() =>
        VerifyAsync(
            """
            using System;
            public static class Guard {
                public static void Positive(int value) { }
            }
            public class Options {
                public int Count { get; set; }
            }
            public class C {
                void M(Options opts) {
                    [|if|] (opts.Count <= 0) throw new ArgumentOutOfRangeException(nameof(opts));
                }
            }
            """,
            """
            using System;
            public static class Guard {
                public static void Positive(int value) { }
            }
            public class Options {
                public int Count { get; set; }
            }
            public class C {
                void M(Options opts) {
                    Guard.Positive(opts.Count);
                }
            }
            """);
}
