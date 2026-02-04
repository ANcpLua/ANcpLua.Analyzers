using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL0046: Use Guard.NotNullOrWhiteSpace instead of if (string.IsNullOrWhiteSpace(x)) throw.
/// </summary>
public sealed partial class Al0046UseGuardNotNullOrWhiteSpaceTests : AnalyzerTest<Al0046UseGuardNotNullOrWhiteSpaceAnalyzer> {
    [Fact]
    public Task ShouldReportForIsNullOrWhiteSpaceWithArgumentNullException() => VerifyAsync("""
        using System;
        public class C {
            void M(string? value) {
                [|if (string.IsNullOrWhiteSpace(value)) throw new ArgumentNullException(nameof(value));|]
            }
        }
        """);

    [Fact]
    public Task ShouldReportForIsNullOrWhiteSpaceWithArgumentException() => VerifyAsync("""
        using System;
        public class C {
            void M(string? value) {
                [|if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Value cannot be whitespace", nameof(value));|]
            }
        }
        """);

    [Fact]
    public Task ShouldReportForBlockBody() => VerifyAsync("""
        using System;
        public class C {
            void M(string? value) {
                [|if (string.IsNullOrWhiteSpace(value)) {
                    throw new ArgumentNullException(nameof(value));
                }|]
            }
        }
        """);

    [Fact]
    public Task ShouldNotReportForIsNullOrEmpty() => VerifyAsync("""
        using System;
        public class C {
            void M(string? value) {
                if (string.IsNullOrEmpty(value)) throw new ArgumentNullException(nameof(value));
            }
        }
        """);

    [Fact]
    public Task ShouldNotReportForOtherExceptionTypes() => VerifyAsync("""
        using System;
        public class C {
            void M(string? value) {
                if (string.IsNullOrWhiteSpace(value)) throw new InvalidOperationException("Value cannot be whitespace");
            }
        }
        """);

    [Fact]
    public Task ShouldNotReportForNullCheck() => VerifyAsync("""
        using System;
        public class C {
            void M(string? value) {
                if (value == null) throw new ArgumentNullException(nameof(value));
            }
        }
        """);

    [Fact]
    public Task ShouldNotReportForOtherConditions() => VerifyAsync("""
        using System;
        public class C {
            void M(string? value) {
                if (value.Length == 0) throw new ArgumentException("Value cannot be empty");
            }
        }
        """);

    [Fact]
    public Task ShouldNotReportForIfWithElse() => VerifyAsync("""
        using System;
        public class C {
            void M(string? value) {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentNullException(nameof(value));
                else
                    Console.WriteLine(value);
            }
        }
        """);

    [Fact]
    public Task ShouldReportForStringClassNameUppercase() => VerifyAsync("""
        using System;
        public class C {
            void M(string? value) {
                [|if (String.IsNullOrWhiteSpace(value)) throw new ArgumentNullException(nameof(value));|]
            }
        }
        """);

    [Fact]
    public Task ShouldReportForMemberAccess() => VerifyAsync("""
        using System;
        public class C {
            private string? _name;
            void M() {
                [|if (string.IsNullOrWhiteSpace(_name)) throw new ArgumentNullException(nameof(_name));|]
            }
        }
        """);
}
