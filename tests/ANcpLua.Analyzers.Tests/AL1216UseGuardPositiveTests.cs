using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Analyzers.CodeFixes.CodeFixes;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL1216: Use Guard.Positive instead of if (x &lt;= 0) throw new ArgumentOutOfRangeException.
/// </summary>
public sealed partial class Al1216UseGuardPositiveTests : AnalyzerTest<Al1216UseGuardPositiveAnalyzer> {
    // AL1216 only fires when ANcpLua.Roslyn.Utilities.Guard is present and accessible.
    // Each positive/negative case appends this stub; ShouldNotReportWhenGuardNotReferenced omits it.
    private const string Stub = """
                                namespace ANcpLua.Roslyn.Utilities { internal static class Guard { } }
                                """;

    private static Task Verify(string body) => VerifyAsync($$"""
                                                            {{body}}
                                                            {{Stub}}
                                                            """);

    [Fact]
    public Task ShouldReportForLessThanOrEqualZero() => Verify("""
        using System;
        public class C {
            void M(int x) {
                [|if|] (x <= 0) throw new ArgumentOutOfRangeException(nameof(x));
            }
        }
        """);

    [Fact]
    public Task ShouldReportForReversedComparison() => Verify("""
        using System;
        public class C {
            void M(int x) {
                [|if|] (0 >= x) throw new ArgumentOutOfRangeException(nameof(x));
            }
        }
        """);

    [Fact]
    public Task ShouldReportForLong() => Verify("""
        using System;
        public class C {
            void M(long count) {
                [|if|] (count <= 0) throw new ArgumentOutOfRangeException(nameof(count));
            }
        }
        """);

    [Fact]
    public Task ShouldReportForDouble() => Verify("""
        using System;
        public class C {
            void M(double value) {
                [|if|] (value <= 0.0) throw new ArgumentOutOfRangeException(nameof(value));
            }
        }
        """);

    [Fact]
    public Task ShouldReportForDecimal() => Verify("""
        using System;
        public class C {
            void M(decimal amount) {
                [|if|] (amount <= 0m) throw new ArgumentOutOfRangeException(nameof(amount));
            }
        }
        """);

    [Fact]
    public Task ShouldReportForBlockBody() => Verify("""
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
    public Task ShouldNotReportForLessThanZero() => Verify("""
        using System;
        public class C {
            void M(int x) {
                if (x < 0) throw new ArgumentOutOfRangeException(nameof(x));
            }
        }
        """);

    [Fact]
    public Task ShouldNotReportForOtherExceptionTypes() => Verify("""
        using System;
        public class C {
            void M(int x) {
                if (x <= 0) throw new InvalidOperationException("Value must be positive.");
            }
        }
        """);

    [Fact]
    public Task ShouldNotReportForArgumentNullException() => Verify("""
        using System;
        public class C {
            void M(int x) {
                if (x <= 0) throw new ArgumentNullException(nameof(x));
            }
        }
        """);

    [Fact]
    public Task ShouldNotReportForNonZeroComparison() => Verify("""
        using System;
        public class C {
            void M(int x) {
                if (x <= 5) throw new ArgumentOutOfRangeException(nameof(x));
            }
        }
        """);

    [Fact]
    public Task ShouldNotReportForEqualsZero() => Verify("""
        using System;
        public class C {
            void M(int x) {
                if (x == 0) throw new ArgumentOutOfRangeException(nameof(x));
            }
        }
        """);

    [Fact]
    public Task ShouldNotReportForElseBranch() => Verify("""
        using System;
        public class C {
            void M(int x) {
                if (x <= 0) throw new ArgumentOutOfRangeException(nameof(x));
                else Console.WriteLine("Valid");
            }
        }
        """);

    [Fact]
    public Task ShouldReportForMemberAccess() => Verify("""
        using System;
        public class C {
            void M(Options opts) {
                [|if|] (opts.Count <= 0) throw new ArgumentOutOfRangeException(nameof(opts));
            }
        }
        public class Options { public int Count { get; set; } }
        """);

    [Fact]
    public Task ShouldReportForFloat() => Verify("""
        using System;
        public class C {
            void M(float value) {
                [|if|] (value <= 0.0f) throw new ArgumentOutOfRangeException(nameof(value));
            }
        }
        """);

    [Fact]
    public Task ShouldReportForShort() => Verify("""
        using System;
        public class C {
            void M(short value) {
                [|if|] (value <= 0) throw new ArgumentOutOfRangeException(nameof(value));
            }
        }
        """);

    [Fact]
    public Task ShouldReportWithMessage() => Verify("""
        using System;
        public class C {
            void M(int x) {
                [|if|] (x <= 0) throw new ArgumentOutOfRangeException(nameof(x), "Value must be positive.");
            }
        }
        """);

    [Fact]
    public Task ShouldNotReportForMultipleStatements() => Verify("""
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

    // Gate regression: no Guard type in scope → no diagnostic.
    [Fact]
    public Task ShouldNotReportWhenGuardNotReferenced() =>
        VerifyAsync("""
                    using System;
                    public class C {
                        void M(int x) {
                            if (x <= 0) throw new ArgumentOutOfRangeException(nameof(x));
                        }
                    }
                    """);
}

public sealed partial class Al1216UseGuardPositiveCodeFixTests
    : CodeFixTest<Al1216UseGuardPositiveAnalyzer, Al1216UseGuardPositiveCodeFixProvider> {
    // Polyfill in the real namespace so the analyzer gate opens and Guard.Positive() resolves.
    private const string GuardPolyfill = """
        using ANcpLua.Roslyn.Utilities;
        namespace ANcpLua.Roslyn.Utilities {
            public static class Guard {
                public static void Positive(int value) { }
            }
        }
        """;

    [Fact]
    public Task ShouldPreserveMemberAccessReceiver() =>
        VerifyAsync(
            $$"""
            using System;
            {{GuardPolyfill}}
            public class Options {
                public int Count { get; set; }
            }
            public class C {
                void M(Options opts) {
                    [|if|] (opts.Count <= 0) throw new ArgumentOutOfRangeException(nameof(opts));
                }
            }
            """,
            $$"""
            using System;
            {{GuardPolyfill}}
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
