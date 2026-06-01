using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Analyzers.CodeFixes.CodeFixes;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL1210: Use StringComparison extensions for clearer intent.
/// </summary>
public sealed partial class Al1210UseStringComparisonExtensionsTests : AnalyzerTest<Al1210UseStringComparisonExtensionsAnalyzer> {
    // AL1210 only fires when ANcpLua.Roslyn.Utilities.StringComparisonExtensions is present and
    // accessible. Each positive/negative case is wrapped with this stub so the gate is open and the
    // test exercises the analyzer's matching logic rather than the gate. The dedicated
    // ShouldNotReportWhenExtensionsNotReferenced case omits the stub to assert the gate itself.
    private const string Stub = """
        using System;
        namespace ANcpLua.Roslyn.Utilities {
            internal static class StringComparisonExtensions { }
        }
        """;

    private static Task Verify(string body) => VerifyAsync($$"""
        {{Stub}}
        {{body}}
        """);

    [Fact]
    public Task ShouldReportForEqualsOrdinal() =>
        Verify("public class C { bool M(string s, string o) => [|s.Equals(o, StringComparison.Ordinal)|]; }");

    [Fact]
    public Task ShouldReportForEqualsIgnoreCase() =>
        Verify("public class C { bool M(string s, string o) => [|s.Equals(o, StringComparison.OrdinalIgnoreCase)|]; }");

    [Fact]
    public Task ShouldReportForStartsWithOrdinal() =>
        Verify("public class C { bool M(string s, string p) => [|s.StartsWith(p, StringComparison.Ordinal)|]; }");

    [Fact]
    public Task ShouldReportForContainsIgnoreCase() =>
        Verify("public class C { bool M(string s, string sub) => [|s.Contains(sub, StringComparison.OrdinalIgnoreCase)|]; }");

    [Fact]
    public Task ShouldReportForIndexOfWithoutStartIndex() =>
        Verify("""public class C { int M(string s) => [|s.IndexOf("x", StringComparison.Ordinal)|]; }""");

    [Fact]
    public Task ShouldReportForReplaceIgnoreCase() =>
        Verify("""public class C { string M(string s) => [|s.Replace("old", "new", StringComparison.OrdinalIgnoreCase)|]; }""");

    [Fact]
    public Task ShouldReportForEndsWithIgnoreCase() =>
        Verify("public class C { bool M(string s, string suffix) => [|s.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)|]; }");

    [Fact]
    public Task ShouldNotReportWithoutStringComparison() =>
        Verify("public class C { bool M(string s, string o) => s.Equals(o); }");

    [Fact]
    public Task ShouldNotReportForIndexOfWithStartIndex() =>
        Verify("""public class C { int M(string s, int pos) => s.IndexOf("x", pos, StringComparison.Ordinal); }""");

    [Fact]
    public Task ShouldNotReportForLastIndexOf() =>
        Verify("""public class C { int M(string s) => s.LastIndexOf("x", StringComparison.Ordinal); }""");

    [Fact]
    public Task ShouldNotReportForReplaceWithoutComparison() =>
        Verify("""public class C { string M(string s) => s.Replace("old", "new"); }""");

    // Culture-aware comparisons have no StringComparisonExtensions equivalent, so they must not be
    // flagged even when the extensions are referenced — otherwise the fix would name a method that
    // does not exist (e.g. EqualsInvariantCulture).
    [Fact]
    public Task ShouldNotReportForInvariantCulture() =>
        Verify("public class C { bool M(string s, string o) => s.Equals(o, StringComparison.InvariantCulture); }");

    // The Paperless scenario: a project that does not reference ANcpLua.Roslyn.Utilities has no
    // StringComparisonExtensions type, so AL1210 must stay silent on correct BCL StringComparison code.
    [Fact]
    public Task ShouldNotReportWhenExtensionsNotReferenced() => VerifyAsync("""
        using System;
        public class C { bool M(string s, string o) => s.Equals(o, StringComparison.Ordinal); }
        """);
}

/// <summary>
///     Code fix tests for AL1210: Converts StringComparison methods to extension methods.
/// </summary>
public sealed partial class Al1210CodeFixTests
    : CodeFixTest<Al1210UseStringComparisonExtensionsAnalyzer, Al1210UseStringComparisonExtensionsCodeFixProvider> {
    // Polyfill for StringComparison extension methods (they live in ANcpLua.Roslyn.Utilities)
    private const string ExtensionsPolyfill = """
        using System;
        using ANcpLua.Roslyn.Utilities;
        namespace ANcpLua.Roslyn.Utilities {
            public static class StringComparisonExtensions {
                public static bool EqualsOrdinal(this string s, string other) => true;
                public static bool EqualsIgnoreCase(this string s, string other) => true;
                public static bool StartsWithOrdinal(this string s, string value) => true;
                public static bool StartsWithIgnoreCase(this string s, string value) => true;
                public static bool EndsWithOrdinal(this string s, string value) => true;
                public static bool EndsWithIgnoreCase(this string s, string value) => true;
                public static bool ContainsOrdinal(this string s, string value) => true;
                public static bool ContainsIgnoreCase(this string s, string value) => true;
                public static int IndexOfOrdinal(this string s, string value) => 0;
                public static int IndexOfIgnoreCase(this string s, string value) => 0;
                public static string ReplaceIgnoreCase(this string s, string oldValue, string newValue) => s;
            }
        }
        """;

    [Fact]
    public Task ShouldFixEqualsOrdinal() =>
        VerifyAsync(
            $$"""
            {{ExtensionsPolyfill}}
            public class C { bool M(string s, string o) => [|s.Equals(o, StringComparison.Ordinal)|]; }
            """,
            $$"""
            {{ExtensionsPolyfill}}
            public class C { bool M(string s, string o) => s.EqualsOrdinal(o); }
            """);

    [Fact]
    public Task ShouldFixEqualsIgnoreCase() =>
        VerifyAsync(
            $$"""
            {{ExtensionsPolyfill}}
            public class C { bool M(string s, string o) => [|s.Equals(o, StringComparison.OrdinalIgnoreCase)|]; }
            """,
            $$"""
            {{ExtensionsPolyfill}}
            public class C { bool M(string s, string o) => s.EqualsIgnoreCase(o); }
            """);

    [Fact]
    public Task ShouldFixStartsWithOrdinal() =>
        VerifyAsync(
            $$"""
            {{ExtensionsPolyfill}}
            public class C { bool M(string s, string p) => [|s.StartsWith(p, StringComparison.Ordinal)|]; }
            """,
            $$"""
            {{ExtensionsPolyfill}}
            public class C { bool M(string s, string p) => s.StartsWithOrdinal(p); }
            """);

    [Fact]
    public Task ShouldFixEndsWithIgnoreCase() =>
        VerifyAsync(
            $$"""
            {{ExtensionsPolyfill}}
            public class C { bool M(string s, string p) => [|s.EndsWith(p, StringComparison.OrdinalIgnoreCase)|]; }
            """,
            $$"""
            {{ExtensionsPolyfill}}
            public class C { bool M(string s, string p) => s.EndsWithIgnoreCase(p); }
            """);

    [Fact]
    public Task ShouldFixContainsIgnoreCase() =>
        VerifyAsync(
            $$"""
            {{ExtensionsPolyfill}}
            public class C { bool M(string s, string sub) => [|s.Contains(sub, StringComparison.OrdinalIgnoreCase)|]; }
            """,
            $$"""
            {{ExtensionsPolyfill}}
            public class C { bool M(string s, string sub) => s.ContainsIgnoreCase(sub); }
            """);

    [Fact]
    public Task ShouldFixIndexOfOrdinal() =>
        VerifyAsync(
            $$"""
            {{ExtensionsPolyfill}}
            public class C { int M(string s) => [|s.IndexOf("x", StringComparison.Ordinal)|]; }
            """,
            $$"""
            {{ExtensionsPolyfill}}
            public class C { int M(string s) => s.IndexOfOrdinal("x"); }
            """);

    [Fact]
    public Task ShouldFixMultipleInvocations() =>
        VerifyAsync(
            $$"""
            {{ExtensionsPolyfill}}
            public class C {
                bool M(string s, string a, string b) =>
                    [|s.StartsWith(a, StringComparison.Ordinal)|] &&
                    [|s.EndsWith(b, StringComparison.Ordinal)|];
            }
            """,
            $$"""
            {{ExtensionsPolyfill}}
            public class C {
                bool M(string s, string a, string b) =>
                    s.StartsWithOrdinal(a) &&
                    s.EndsWithOrdinal(b);
            }
            """);

    [Fact]
    public Task ShouldFixReplaceIgnoreCase() =>
        VerifyAsync(
            $$"""
            {{ExtensionsPolyfill}}
            public class C { string M(string s) => [|s.Replace("old", "new", StringComparison.OrdinalIgnoreCase)|]; }
            """,
            $$"""
            {{ExtensionsPolyfill}}
            public class C { string M(string s) => s.ReplaceIgnoreCase("old", "new"); }
            """);
}
