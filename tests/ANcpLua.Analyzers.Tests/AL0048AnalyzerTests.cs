using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL0048: Use Guard.NotNegative instead of if (x &lt; 0) throw new ArgumentOutOfRangeException.
/// </summary>
public sealed partial class Al0048AnalyzerTests : AnalyzerTest<Al0048UseGuardNotNegativeAnalyzer> {
    [Fact]
    public Task ShouldReportForIntLessThanZero() => VerifyAsync("""
        using System;
        public class C {
            void M(int x) {
                [|if (x < 0) throw new ArgumentOutOfRangeException(nameof(x));|]
            }
        }
        """);

    [Fact]
    public Task ShouldReportForZeroGreaterThanX() => VerifyAsync("""
        using System;
        public class C {
            void M(int x) {
                [|if (0 > x) throw new ArgumentOutOfRangeException(nameof(x));|]
            }
        }
        """);

    [Fact]
    public Task ShouldReportForLongType() => VerifyAsync("""
        using System;
        public class C {
            void M(long value) {
                [|if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));|]
            }
        }
        """);

    [Fact]
    public Task ShouldReportForDoubleType() => VerifyAsync("""
        using System;
        public class C {
            void M(double value) {
                [|if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));|]
            }
        }
        """);

    [Fact]
    public Task ShouldReportForDecimalType() => VerifyAsync("""
        using System;
        public class C {
            void M(decimal value) {
                [|if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));|]
            }
        }
        """);

    [Fact]
    public Task ShouldReportForThrowWithMessage() => VerifyAsync("""
        using System;
        public class C {
            void M(int x) {
                [|if (x < 0) throw new ArgumentOutOfRangeException(nameof(x), "Value cannot be negative.");|]
            }
        }
        """);

    [Fact]
    public Task ShouldReportForBlockStatement() => VerifyAsync("""
        using System;
        public class C {
            void M(int x) {
                [|if (x < 0) {
                    throw new ArgumentOutOfRangeException(nameof(x));
                }|]
            }
        }
        """);

    [Fact]
    public Task ShouldReportForParenthesizedCondition() => VerifyAsync("""
        using System;
        public class C {
            void M(int x) {
                [|if ((x < 0)) throw new ArgumentOutOfRangeException(nameof(x));|]
            }
        }
        """);

    [Fact]
    public Task ShouldNotReportForLessThanOrEqual() => VerifyAsync("""
        using System;
        public class C {
            void M(int x) {
                if (x <= 0) throw new ArgumentOutOfRangeException(nameof(x));
            }
        }
        """);

    [Fact]
    public Task ShouldNotReportForOtherExceptionType() => VerifyAsync("""
        using System;
        public class C {
            void M(int x) {
                if (x < 0) throw new InvalidOperationException("Value is negative");
            }
        }
        """);

    [Fact]
    public Task ShouldNotReportForArgumentException() => VerifyAsync("""
        using System;
        public class C {
            void M(int x) {
                if (x < 0) throw new ArgumentException("Value is negative", nameof(x));
            }
        }
        """);

    [Fact]
    public Task ShouldNotReportForNonZeroComparison() => VerifyAsync("""
        using System;
        public class C {
            void M(int x) {
                if (x < 10) throw new ArgumentOutOfRangeException(nameof(x));
            }
        }
        """);

    [Fact]
    public Task ShouldNotReportForTernaryExpression() => VerifyAsync("""
        using System;
        public class C {
            int M(int x) => x < 0 ? throw new ArgumentOutOfRangeException(nameof(x)) : x;
        }
        """);

    [Fact]
    public Task ShouldNotReportForEqualityComparison() => VerifyAsync("""
        using System;
        public class C {
            void M(int x) {
                if (x == 0) throw new ArgumentOutOfRangeException(nameof(x));
            }
        }
        """);

    [Fact]
    public Task ShouldNotReportForGreaterThanZero() => VerifyAsync("""
        using System;
        public class C {
            void M(int x) {
                if (x > 0) throw new ArgumentOutOfRangeException(nameof(x));
            }
        }
        """);
}
