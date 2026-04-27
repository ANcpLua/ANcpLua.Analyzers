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

    [Fact]
    public Task ShouldReportForArgumentOutOfRangeExceptionThrowIfZero() => VerifyAsync("""
        using System;
        public class C {
            void M(int n) {
                [|ArgumentOutOfRangeException.ThrowIfZero(n)|];
            }
        }
        """);

    [Fact]
    public Task ShouldReportForArgumentOutOfRangeExceptionThrowIfNegative() => VerifyAsync("""
        using System;
        public class C {
            void M(int n) {
                [|ArgumentOutOfRangeException.ThrowIfNegative(n)|];
            }
        }
        """);

    [Fact]
    public Task ShouldReportForArgumentOutOfRangeExceptionThrowIfNegativeOrZero() => VerifyAsync("""
        using System;
        public class C {
            void M(int n) {
                [|ArgumentOutOfRangeException.ThrowIfNegativeOrZero(n)|];
            }
        }
        """);

    [Fact]
    public Task ShouldReportForArgumentOutOfRangeExceptionThrowIfGreaterThan() => VerifyAsync("""
        using System;
        public class C {
            void M(int n) {
                [|ArgumentOutOfRangeException.ThrowIfGreaterThan(n, 100)|];
            }
        }
        """);

    [Fact]
    public Task ShouldReportForArgumentOutOfRangeExceptionThrowIfGreaterThanOrEqual() => VerifyAsync("""
        using System;
        public class C {
            void M(int n) {
                [|ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(n, 100)|];
            }
        }
        """);

    [Fact]
    public Task ShouldReportForArgumentOutOfRangeExceptionThrowIfLessThan() => VerifyAsync("""
        using System;
        public class C {
            void M(int n) {
                [|ArgumentOutOfRangeException.ThrowIfLessThan(n, 0)|];
            }
        }
        """);

    [Fact]
    public Task ShouldReportForArgumentOutOfRangeExceptionThrowIfLessThanOrEqual() => VerifyAsync("""
        using System;
        public class C {
            void M(int n) {
                [|ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(n, 0)|];
            }
        }
        """);

    // ─────────────────────────────────────────────────────────────────────────
    // Microsoft Agent Framework — Microsoft.Shared.Diagnostics.Throw.*
    // ─────────────────────────────────────────────────────────────────────────

    private const string MafThrowStub = """
        #nullable enable
        using System.Runtime.CompilerServices;
        namespace Microsoft.Shared.Diagnostics {
            public static class Throw {
                public static T IfNull<T>(T argument, [CallerArgumentExpression(nameof(argument))] string paramName = "") => argument!;
                public static TMember IfNullOrMemberNull<TParam, TMember>(TParam argument, TMember member, [CallerArgumentExpression(nameof(argument))] string paramName = "", [CallerArgumentExpression(nameof(member))] string memberName = "") => member!;
                public static string IfNullOrEmpty(string? argument, [CallerArgumentExpression(nameof(argument))] string paramName = "") => argument!;
                public static string IfNullOrWhitespace(string? argument, [CallerArgumentExpression(nameof(argument))] string paramName = "") => argument!;
                public static int IfZero(int argument, [CallerArgumentExpression(nameof(argument))] string paramName = "") => argument;
                public static int IfLessThan(int argument, int min, [CallerArgumentExpression(nameof(argument))] string paramName = "") => argument;
                public static int IfGreaterThan(int argument, int max, [CallerArgumentExpression(nameof(argument))] string paramName = "") => argument;
                public static int IfLessThanOrEqual(int argument, int min, [CallerArgumentExpression(nameof(argument))] string paramName = "") => argument;
                public static int IfGreaterThanOrEqual(int argument, int max, [CallerArgumentExpression(nameof(argument))] string paramName = "") => argument;
                public static int IfOutOfRange(int argument, int min, int max, [CallerArgumentExpression(nameof(argument))] string paramName = "") => argument;
                // Cold-path throwers — analyzer must NOT report on these.
                public static void ArgumentNullException(string paramName) => throw new System.ArgumentNullException(paramName);
                public static void InvalidOperationException(string message) => throw new System.InvalidOperationException(message);
            }
        }
        """;

    [Fact]
    public Task ShouldReportForMafThrowIfNull() => VerifyAsync($$"""
        {{MafThrowStub}}
        public class C {
            void M(object? x) {
                [|Microsoft.Shared.Diagnostics.Throw.IfNull(x)|];
            }
        }
        """);

    [Fact]
    public Task ShouldReportForMafThrowIfNullOrMemberNull() => VerifyAsync($$"""
        {{MafThrowStub}}
        public class C {
            void M(string? p, object? m) {
                [|Microsoft.Shared.Diagnostics.Throw.IfNullOrMemberNull(p, m)|];
            }
        }
        """);

    [Fact]
    public Task ShouldReportForMafThrowIfNullOrEmpty() => VerifyAsync($$"""
        {{MafThrowStub}}
        public class C {
            void M(string? s) {
                [|Microsoft.Shared.Diagnostics.Throw.IfNullOrEmpty(s)|];
            }
        }
        """);

    [Fact]
    public Task ShouldReportForMafThrowIfNullOrWhitespace() => VerifyAsync($$"""
        {{MafThrowStub}}
        public class C {
            void M(string? s) {
                [|Microsoft.Shared.Diagnostics.Throw.IfNullOrWhitespace(s)|];
            }
        }
        """);

    [Fact]
    public Task ShouldReportForMafThrowIfZero() => VerifyAsync($$"""
        {{MafThrowStub}}
        public class C {
            void M(int n) {
                [|Microsoft.Shared.Diagnostics.Throw.IfZero(n)|];
            }
        }
        """);

    [Fact]
    public Task ShouldReportForMafThrowIfLessThan() => VerifyAsync($$"""
        {{MafThrowStub}}
        public class C {
            void M(int n) {
                [|Microsoft.Shared.Diagnostics.Throw.IfLessThan(n, 0)|];
            }
        }
        """);

    [Fact]
    public Task ShouldReportForMafThrowIfGreaterThan() => VerifyAsync($$"""
        {{MafThrowStub}}
        public class C {
            void M(int n) {
                [|Microsoft.Shared.Diagnostics.Throw.IfGreaterThan(n, 100)|];
            }
        }
        """);

    [Fact]
    public Task ShouldReportForMafThrowIfLessThanOrEqual() => VerifyAsync($$"""
        {{MafThrowStub}}
        public class C {
            void M(int n) {
                [|Microsoft.Shared.Diagnostics.Throw.IfLessThanOrEqual(n, 0)|];
            }
        }
        """);

    [Fact]
    public Task ShouldReportForMafThrowIfGreaterThanOrEqual() => VerifyAsync($$"""
        {{MafThrowStub}}
        public class C {
            void M(int n) {
                [|Microsoft.Shared.Diagnostics.Throw.IfGreaterThanOrEqual(n, 100)|];
            }
        }
        """);

    [Fact]
    public Task ShouldReportForMafThrowIfOutOfRange() => VerifyAsync($$"""
        {{MafThrowStub}}
        public class C {
            void M(int n) {
                [|Microsoft.Shared.Diagnostics.Throw.IfOutOfRange(n, 0, 100)|];
            }
        }
        """);

    [Fact]
    public Task ShouldNotReportForMafColdPathArgumentNullException() => VerifyAsync($$"""
        {{MafThrowStub}}
        public class C {
            void M() {
                Microsoft.Shared.Diagnostics.Throw.ArgumentNullException("paramName");
            }
        }
        """);

    [Fact]
    public Task ShouldNotReportForMafColdPathInvalidOperationException() => VerifyAsync($$"""
        {{MafThrowStub}}
        public class C {
            void M() {
                Microsoft.Shared.Diagnostics.Throw.InvalidOperationException("message");
            }
        }
        """);

    [Fact]
    public Task ShouldNotReportForUnrelatedThrowClass() => VerifyAsync("""
        namespace Other {
            public static class Throw {
                public static T IfNull<T>(T x) => x!;
            }
        }
        public class C {
            void M(object? x) {
                Other.Throw.IfNull(x);
            }
        }
        """);
}
