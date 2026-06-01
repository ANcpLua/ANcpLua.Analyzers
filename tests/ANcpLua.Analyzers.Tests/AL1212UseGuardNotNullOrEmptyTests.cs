using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL1212: Use Guard.NotNullOrEmpty instead of if (string.IsNullOrEmpty) throw.
/// </summary>
public sealed partial class Al1212UseGuardNotNullOrEmptyTests : AnalyzerTest<Al1212UseGuardNotNullOrEmptyAnalyzer> {
    // AL1212 only fires when ANcpLua.Roslyn.Utilities.Guard is present and accessible.
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
    public Task ShouldReportForArgumentNullException() => Verify("""
        using System;
        public class C {
            void M(string? x) {
                [|if (string.IsNullOrEmpty(x)) throw new ArgumentNullException(nameof(x));|]
            }
        }
        """);

    [Fact]
    public Task ShouldReportForArgumentException() => Verify("""
        using System;
        public class C {
            void M(string? x) {
                [|if (string.IsNullOrEmpty(x)) throw new ArgumentException("Value cannot be empty", nameof(x));|]
            }
        }
        """);

    [Fact]
    public Task ShouldReportForBlockBody() => Verify("""
        using System;
        public class C {
            void M(string? value) {
                [|if (string.IsNullOrEmpty(value)) {
                    throw new ArgumentNullException(nameof(value));
                }|]
            }
        }
        """);

    [Fact]
    public Task ShouldReportForParenthesizedCondition() => Verify("""
        using System;
        public class C {
            void M(string? x) {
                [|if ((string.IsNullOrEmpty(x))) throw new ArgumentNullException(nameof(x));|]
            }
        }
        """);

    [Fact]
    public Task ShouldReportForMemberAccessArgument() => Verify("""
        using System;
        public class C {
            void M(Options opts) {
                [|if (string.IsNullOrEmpty(opts.Name)) throw new ArgumentNullException(nameof(opts));|]
            }
        }
        public class Options { public string? Name { get; set; } }
        """);

    [Fact]
    public Task ShouldNotReportForNullCheck() => VerifyAsync("""
        using System;
        public class C {
            void M(string? x) {
                if (x == null) throw new ArgumentNullException(nameof(x));
            }
        }
        """);

    [Fact]
    public Task ShouldNotReportForIsNullCheck() => VerifyAsync("""
        using System;
        public class C {
            void M(string? x) {
                if (x is null) throw new ArgumentNullException(nameof(x));
            }
        }
        """);

    [Fact]
    public Task ShouldNotReportForOtherExceptionTypes() => VerifyAsync("""
        using System;
        public class C {
            void M(string? x) {
                if (string.IsNullOrEmpty(x)) throw new InvalidOperationException("x is empty");
            }
        }
        """);

    [Fact]
    public Task ShouldNotReportForMultipleStatements() => VerifyAsync("""
        using System;
        public class C {
            void M(string? x) {
                if (string.IsNullOrEmpty(x)) {
                    Console.WriteLine("Error");
                    throw new ArgumentNullException(nameof(x));
                }
            }
        }
        """);

    [Fact]
    public Task ShouldNotReportForIsNullOrWhiteSpace() => VerifyAsync("""
        using System;
        public class C {
            void M(string? x) {
                if (string.IsNullOrWhiteSpace(x)) throw new ArgumentNullException(nameof(x));
            }
        }
        """);

    [Fact]
    public Task ShouldNotReportForElseBranch() => VerifyAsync("""
        using System;
        public class C {
            void M(string? x) {
                if (string.IsNullOrEmpty(x)) throw new ArgumentNullException(nameof(x));
                else Console.WriteLine(x);
            }
        }
        """);

    // The consumer scenario: a project that does not reference ANcpLua.Roslyn.Utilities has no
    // Guard type, so AL1212 must stay silent on correct BCL null/empty guard patterns.
    [Fact]
    public Task ShouldNotReportWhenGuardNotReferenced() =>
        VerifyAsync("""
                    using System;
                    public class C {
                        void M(string? x) {
                            if (string.IsNullOrEmpty(x)) throw new ArgumentNullException(nameof(x));
                        }
                    }
                    """);
}
