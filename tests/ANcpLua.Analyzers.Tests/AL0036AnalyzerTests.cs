using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL0036: Use Guard.NotNull instead of ?? throw new ArgumentNullException.
/// </summary>
public sealed partial class Al0036AnalyzerTests : AnalyzerTest<Al0036UseGuardNotNullAnalyzer> {
    [Fact]
    public Task ShouldReportForNullCoalesceThrow() => VerifyAsync("""
        using System;
        public class C {
            string M(string? x) => [|x ?? throw new ArgumentNullException(nameof(x))|];
        }
        """);

    [Fact]
    public Task ShouldReportForAssignmentPattern() => VerifyAsync("""
        using System;
        public class C {
            void M(string? x) {
                x = [|x ?? throw new ArgumentNullException(nameof(x))|];
            }
        }
        """);

    [Fact]
    public Task ShouldReportForObjectParameter() => VerifyAsync("""
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
    public Task ShouldSuggestThrowIfNullWhenHelperAvailable() => VerifyAsync("""
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
}
