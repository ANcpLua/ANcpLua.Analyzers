using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL1208: Use Guard.NotNull instead of ?? throw new ArgumentNullException.
/// </summary>
public sealed partial class Al1208UseGuardNotNullTests : AnalyzerTest<Al1208UseGuardNotNullAnalyzer> {
    // AL1208 only fires when ANcpLua.Roslyn.Utilities.Guard is present and accessible.
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
    public Task ShouldReportForNullCoalesceThrow() => Verify("""
        using System;
        public class C {
            string M(string? x) => [|x ?? throw new ArgumentNullException(nameof(x))|];
        }
        """);

    [Fact]
    public Task ShouldReportForAssignmentPattern() => Verify("""
        using System;
        public class C {
            void M(string? x) {
                x = [|x ?? throw new ArgumentNullException(nameof(x))|];
            }
        }
        """);

    [Fact]
    public Task ShouldReportForObjectParameter() => Verify("""
        using System;
        public class C {
            object M(object? value) => [|value ?? throw new ArgumentNullException(nameof(value))|];
        }
        """);

    [Fact]
    public Task ShouldNotReportForOtherExceptionTypes() => VerifyAsync("""
        using System;
        public class C {
            string M(string? x) => x ?? throw new InvalidOperationException("value is null");
        }
        """);

    [Fact]
    public Task ShouldNotReportForNullCoalesceValue() => VerifyAsync("""
        using System;
        public class C {
            string M(string? x) => x ?? "default";
        }
        """);

    [Fact]
    public Task ShouldSuggestThrowIfNullWhenHelperAvailable() => Verify("""
        using System;
        namespace Microsoft.Shared.Diagnostics {
            public static class Throw {
                public static void IfNull(object? argument) { }
            }
        }
        public class C {
            string M(string? x) => [|x ?? throw new ArgumentNullException(nameof(x))|];
        }
        """);

    // The consumer scenario: a project that does not reference ANcpLua.Roslyn.Utilities has no
    // Guard type, so AL1208 must stay silent on correct BCL null-coalescing throw patterns.
    [Fact]
    public Task ShouldNotReportWhenGuardNotReferenced() =>
        VerifyAsync("""
                    using System;
                    public class C {
                        string M(string? x) => x ?? throw new ArgumentNullException(nameof(x));
                    }
                    """);
}
