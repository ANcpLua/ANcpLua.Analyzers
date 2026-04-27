using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Analyzers.CodeFixes.CodeFixes;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Code fix tests for AL0137: BCL throw helpers → Guard.* helpers from ANcpLua.Roslyn.Utilities.
/// </summary>
public sealed partial class Al0137UseGuardForThrowIfCodeFixTests
    : CodeFixTest<Al0137UseGuardForThrowIfAnalyzer, Al0137UseGuardForThrowIfCodeFixProvider> {
    /// <summary>
    ///     Polyfill stub — matches the public surface of <c>Guard</c> from
    ///     <c>ANcpLua.Roslyn.Utilities</c> for the three methods this analyzer fixes to.
    /// </summary>
    private const string GuardPolyfill = """
        #nullable enable
        namespace ANcpLua.Roslyn.Utilities {
            public static class Guard {
                public static T NotNull<T>(T? value) => value!;
                public static string NotNullOrEmpty(string? value) => value!;
                public static string NotNullOrWhiteSpace(string? value) => value!;
                public static int NotZero(int value) => value;
                public static int NotNegative(int value) => value;
                public static int Positive(int value) => value;
                public static int NotGreaterThan(int value, int max) => value;
                public static int LessThan(int value, int max) => value;
                public static int NotLessThan(int value, int min) => value;
                public static int GreaterThan(int value, int min) => value;
            }
        }
        """;

    [Fact]
    public Task ShouldFixArgumentNullExceptionThrowIfNull() =>
        VerifyAsync(
            $$"""
            #nullable enable
            using System;
            {{GuardPolyfill}}
            public class C {
                void M(object? x) {
                    [|ArgumentNullException.ThrowIfNull(x)|];
                }
            }
            """,
            $$"""
            #nullable enable
            using System;
            using ANcpLua.Roslyn.Utilities;
            {{GuardPolyfill}}
            public class C {
                void M(object? x) {
                    Guard.NotNull(x);
                }
            }
            """);

    [Fact]
    public Task ShouldFixArgumentExceptionThrowIfNullOrEmpty() =>
        VerifyAsync(
            $$"""
            #nullable enable
            using System;
            {{GuardPolyfill}}
            public class C {
                void M(string? s) {
                    [|ArgumentException.ThrowIfNullOrEmpty(s)|];
                }
            }
            """,
            $$"""
            #nullable enable
            using System;
            using ANcpLua.Roslyn.Utilities;
            {{GuardPolyfill}}
            public class C {
                void M(string? s) {
                    Guard.NotNullOrEmpty(s);
                }
            }
            """);

    [Fact]
    public Task ShouldFixArgumentExceptionThrowIfNullOrWhiteSpace() =>
        VerifyAsync(
            $$"""
            #nullable enable
            using System;
            {{GuardPolyfill}}
            public class C {
                void M(string? s) {
                    [|ArgumentException.ThrowIfNullOrWhiteSpace(s)|];
                }
            }
            """,
            $$"""
            #nullable enable
            using System;
            using ANcpLua.Roslyn.Utilities;
            {{GuardPolyfill}}
            public class C {
                void M(string? s) {
                    Guard.NotNullOrWhiteSpace(s);
                }
            }
            """);

    [Fact]
    public Task ShouldNotDuplicateUsingWhenAlreadyImported() =>
        VerifyAsync(
            $$"""
            #nullable enable
            using System;
            using ANcpLua.Roslyn.Utilities;
            {{GuardPolyfill}}
            public class C {
                void M(object? x) {
                    [|ArgumentNullException.ThrowIfNull(x)|];
                }
            }
            """,
            $$"""
            #nullable enable
            using System;
            using ANcpLua.Roslyn.Utilities;
            {{GuardPolyfill}}
            public class C {
                void M(object? x) {
                    Guard.NotNull(x);
                }
            }
            """);
}
