using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL1215: Use Guard.NotNegative instead of if (x &lt; 0) throw new ArgumentOutOfRangeException.
/// </summary>
public sealed partial class Al1215UseGuardNotNegativeTests : AnalyzerTest<Al1215UseGuardNotNegativeAnalyzer> {
    // AL1215 only fires when ANcpLua.Roslyn.Utilities.Guard is present and accessible.
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
    public Task ShouldReportForIntLessThanZero() => Verify("""
        using System;
        public class C {
            void M(int x) {
                [|if (x < 0) throw new ArgumentOutOfRangeException(nameof(x));|]
            }
        }
        """);

    [Fact]
    public Task ShouldReportForZeroGreaterThanX() => Verify("""
        using System;
        public class C {
            void M(int x) {
                [|if (0 > x) throw new ArgumentOutOfRangeException(nameof(x));|]
            }
        }
        """);

    [Fact]
    public Task ShouldReportForLongType() => Verify("""
        using System;
        public class C {
            void M(long value) {
                [|if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));|]
            }
        }
        """);

    [Fact]
    public Task ShouldReportForDoubleType() => Verify("""
        using System;
        public class C {
            void M(double value) {
                [|if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));|]
            }
        }
        """);

    [Fact]
    public Task ShouldReportForDecimalType() => Verify("""
        using System;
        public class C {
            void M(decimal value) {
                [|if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));|]
            }
        }
        """);

    [Fact]
    public Task ShouldReportForThrowWithMessage() => Verify("""
        using System;
        public class C {
            void M(int x) {
                [|if (x < 0) throw new ArgumentOutOfRangeException(nameof(x), "Value cannot be negative.");|]
            }
        }
        """);

    [Fact]
    public Task ShouldReportForBlockStatement() => Verify("""
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
    public Task ShouldNotReportForBlockStatementWithExtraStatements() => Verify("""
        using System;
        public class C {
            void M(int x) {
                if (x < 0) {
                    throw new ArgumentOutOfRangeException(nameof(x));
                    Console.WriteLine(x);
                }
            }
        }
        """);

    [Fact]
    public Task ShouldNotReportWhenElsePresent() => Verify("""
        using System;
        public class C {
            void M(int x) {
                if (x < 0) {
                    throw new ArgumentOutOfRangeException(nameof(x));
                } else {
                    Console.WriteLine(x);
                }
            }
        }
        """);

    [Fact]
    public Task ShouldReportForParenthesizedCondition() => Verify("""
        using System;
        public class C {
            void M(int x) {
                [|if ((x < 0)) throw new ArgumentOutOfRangeException(nameof(x));|]
            }
        }
        """);

    [Fact]
    public Task ShouldNotReportForLessThanOrEqual() => Verify("""
        using System;
        public class C {
            void M(int x) {
                if (x <= 0) throw new ArgumentOutOfRangeException(nameof(x));
            }
        }
        """);

    [Fact]
    public Task ShouldNotReportForOtherExceptionType() => Verify("""
        using System;
        public class C {
            void M(int x) {
                if (x < 0) throw new InvalidOperationException("Value is negative");
            }
        }
        """);

    [Fact]
    public Task ShouldNotReportForArgumentException() => Verify("""
        using System;
        public class C {
            void M(int x) {
                if (x < 0) throw new ArgumentException("Value is negative", nameof(x));
            }
        }
        """);

    [Fact]
    public Task ShouldNotReportForNonZeroComparison() => Verify("""
        using System;
        public class C {
            void M(int x) {
                if (x < 10) throw new ArgumentOutOfRangeException(nameof(x));
            }
        }
        """);

    [Fact]
    public Task ShouldNotReportForTernaryExpression() => Verify("""
        using System;
        public class C {
            int M(int x) => x < 0 ? throw new ArgumentOutOfRangeException(nameof(x)) : x;
        }
        """);

    [Fact]
    public Task ShouldNotReportForEqualityComparison() => Verify("""
        using System;
        public class C {
            void M(int x) {
                if (x == 0) throw new ArgumentOutOfRangeException(nameof(x));
            }
        }
        """);

    [Fact]
    public Task ShouldNotReportForGreaterThanZero() => Verify("""
        using System;
        public class C {
            void M(int x) {
                if (x > 0) throw new ArgumentOutOfRangeException(nameof(x));
            }
        }
        """);

    // The consumer scenario: a project that does not reference ANcpLua.Roslyn.Utilities has no
    // Guard type, so AL1215 must stay silent on correct guard patterns.
    [Fact]
    public Task ShouldNotReportWhenGuardNotReferenced() =>
        VerifyAsync("""
                    using System;
                    public class C {
                        void M(int x) {
                            if (x < 0) throw new ArgumentOutOfRangeException(nameof(x));
                        }
                    }
                    """);
}
