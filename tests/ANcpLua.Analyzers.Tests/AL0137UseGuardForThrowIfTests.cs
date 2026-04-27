using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL0137: Use Guard.* helpers from ANcpLua.Roslyn.Utilities instead of the BCL
///     ArgumentNullException.ThrowIfNull / ArgumentException.ThrowIfNullOrEmpty /
///     ArgumentException.ThrowIfNullOrWhiteSpace throw helpers.
/// </summary>
public sealed partial class Al0137UseGuardForThrowIfTests : AnalyzerTest<Al0137UseGuardForThrowIfAnalyzer> {
    [Fact]
    public Task ShouldReportForArgumentNullExceptionThrowIfNull() => VerifyAsync("""
        using System;
        public class C {
            void M(object? x) {
                [|ArgumentNullException.ThrowIfNull(x)|];
            }
        }
        """);

    [Fact]
    public Task ShouldReportForArgumentExceptionThrowIfNullOrEmpty() => VerifyAsync("""
        using System;
        public class C {
            void M(string? s) {
                [|ArgumentException.ThrowIfNullOrEmpty(s)|];
            }
        }
        """);

    [Fact]
    public Task ShouldReportForArgumentExceptionThrowIfNullOrWhiteSpace() => VerifyAsync("""
        using System;
        public class C {
            void M(string? s) {
                [|ArgumentException.ThrowIfNullOrWhiteSpace(s)|];
            }
        }
        """);

    [Fact]
    public Task ShouldReportWithFullyQualifiedName() => VerifyAsync("""
        public class C {
            void M(object? x) {
                [|System.ArgumentNullException.ThrowIfNull(x)|];
            }
        }
        """);

    [Fact]
    public Task ShouldNotReportForUnrelatedStaticMethod() => VerifyAsync("""
        using System;
        public class C {
            void M() {
                Console.WriteLine("hi");
            }
        }
        """);

    [Fact]
    public Task ShouldNotReportForCustomThrowIfNullInDifferentNamespace() => VerifyAsync("""
        using Other;
        namespace Other {
            public static class ArgumentNullException {
                public static void ThrowIfNull(object? x) { }
            }
        }
        public class C {
            void M(object? x) {
                ArgumentNullException.ThrowIfNull(x);
            }
        }
        """);

    [Fact]
    public Task ShouldNotReportForInstanceMethodWithSameName() => VerifyAsync("""
        public class Stub {
            public void ThrowIfNull(object? x) { }
        }
        public class C {
            void M(Stub stub, object? x) {
                stub.ThrowIfNull(x);
            }
        }
        """);
}
