using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL1213: Use Guard.NotNullOrWhiteSpace instead of if (string.IsNullOrWhiteSpace(x)) throw.
/// </summary>
public sealed partial class Al1213UseGuardNotNullOrWhiteSpaceTests : AnalyzerTest<Al1213UseGuardNotNullOrWhiteSpaceAnalyzer> {
    // AL1213 only fires when ANcpLua.Roslyn.Utilities.Guard is present and accessible.
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
    public Task ShouldReportForIsNullOrWhiteSpaceWithArgumentNullException() => Verify("""
        using System;
        public class C {
            void M(string? value) {
                [|if (string.IsNullOrWhiteSpace(value)) throw new ArgumentNullException(nameof(value));|]
            }
        }
        """);

    [Fact]
    public Task ShouldReportForIsNullOrWhiteSpaceWithArgumentException() => Verify("""
        using System;
        public class C {
            void M(string? value) {
                [|if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Value cannot be whitespace", nameof(value));|]
            }
        }
        """);

    [Fact]
    public Task ShouldReportForBlockBody() => Verify("""
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
    public Task ShouldReportForStringClassNameUppercase() => Verify("""
        using System;
        public class C {
            void M(string? value) {
                [|if (String.IsNullOrWhiteSpace(value)) throw new ArgumentNullException(nameof(value));|]
            }
        }
        """);

    [Fact]
    public Task ShouldReportForMemberAccess() => Verify("""
        using System;
        public class C {
            private string? _name;
            void M() {
                [|if (string.IsNullOrWhiteSpace(_name)) throw new ArgumentNullException(nameof(_name));|]
            }
        }
        """);

    // The consumer scenario: a project that does not reference ANcpLua.Roslyn.Utilities has no
    // Guard type, so AL1213 must stay silent on correct BCL whitespace guard patterns.
    [Fact]
    public Task ShouldNotReportWhenGuardNotReferenced() =>
        VerifyAsync("""
                    using System;
                    public class C {
                        void M(string? value) {
                            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentNullException(nameof(value));
                        }
                    }
                    """);
}
