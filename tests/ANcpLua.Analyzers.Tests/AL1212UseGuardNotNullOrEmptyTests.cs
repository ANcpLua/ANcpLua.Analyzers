using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL1212: Use Guard.NotNullOrEmpty instead of if (string.IsNullOrEmpty) throw.
/// </summary>
public sealed partial class Al1212UseGuardNotNullOrEmptyTests : AnalyzerTest<Al1212UseGuardNotNullOrEmptyAnalyzer> {
    [Fact]
    public Task ShouldReportForArgumentNullException() => VerifyAsync("""
        using System;
        public class C {
            void M(string? x) {
                [|if (string.IsNullOrEmpty(x)) throw new ArgumentNullException(nameof(x));|]
            }
        }
        """);

    [Fact]
    public Task ShouldReportForArgumentException() => VerifyAsync("""
        using System;
        public class C {
            void M(string? x) {
                [|if (string.IsNullOrEmpty(x)) throw new ArgumentException("Value cannot be empty", nameof(x));|]
            }
        }
        """);

    [Fact]
    public Task ShouldReportForBlockBody() => VerifyAsync("""
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
    public Task ShouldReportForParenthesizedCondition() => VerifyAsync("""
        using System;
        public class C {
            void M(string? x) {
                [|if ((string.IsNullOrEmpty(x))) throw new ArgumentNullException(nameof(x));|]
            }
        }
        """);

    [Fact]
    public Task ShouldReportForMemberAccessArgument() => VerifyAsync("""
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
}
