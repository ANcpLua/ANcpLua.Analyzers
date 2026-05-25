using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Analyzers.CodeFixes.CodeFixes;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL1210: Use StringComparison extensions for clearer intent.
/// </summary>
public sealed partial class Al1210UseStringComparisonExtensionsTests : AnalyzerTest<Al1210UseStringComparisonExtensionsAnalyzer> {
    [Fact]
    public Task ShouldReportForEqualsOrdinal() => VerifyAsync("""
        using System;
        public class C {
            bool M(string s, string other) => [|s.Equals(other, StringComparison.Ordinal)|];
        }
        """);

    [Fact]
    public Task ShouldReportForEqualsIgnoreCase() => VerifyAsync("""
        using System;
        public class C {
            bool M(string s, string other) => [|s.Equals(other, StringComparison.OrdinalIgnoreCase)|];
        }
        """);

    [Fact]
    public Task ShouldReportForStartsWithOrdinal() => VerifyAsync("""
        using System;
        public class C {
            bool M(string s, string prefix) => [|s.StartsWith(prefix, StringComparison.Ordinal)|];
        }
        """);

    [Fact]
    public Task ShouldReportForContainsIgnoreCase() => VerifyAsync("""
        using System;
        public class C {
            bool M(string s, string sub) => [|s.Contains(sub, StringComparison.OrdinalIgnoreCase)|];
        }
        """);

    [Fact]
    public Task ShouldNotReportWithoutStringComparison() => VerifyAsync("""
        using System;
        public class C {
            bool M(string s, string other) => s.Equals(other);
        }
        """);

    [Fact]
    public Task ShouldNotReportForIndexOfWithStartIndex() => VerifyAsync("""
        using System;
        public class C {
            int M(string s, int pos) => s.IndexOf("x", pos, StringComparison.Ordinal);
        }
        """);

    [Fact]
    public Task ShouldReportForIndexOfWithoutStartIndex() => VerifyAsync("""
        using System;
        public class C {
            int M(string s) => [|s.IndexOf("x", StringComparison.Ordinal)|];
        }
        """);

    [Fact]
    public Task ShouldNotReportForLastIndexOf() => VerifyAsync("""
        using System;
        public class C {
            int M(string s) => s.LastIndexOf("x", StringComparison.Ordinal);
        }
        """);

    [Fact]
    public Task ShouldReportForReplaceIgnoreCase() => VerifyAsync("""
        using System;
        public class C {
            string M(string s) => [|s.Replace("old", "new", StringComparison.OrdinalIgnoreCase)|];
        }
        """);

    [Fact]
    public Task ShouldNotReportForReplaceWithoutComparison() => VerifyAsync("""
        using System;
        public class C {
            string M(string s) => s.Replace("old", "new");
        }
        """);

    [Fact]
    public Task ShouldReportForEndsWithIgnoreCase() => VerifyAsync("""
        using System;
        public class C {
            bool M(string s, string suffix) => [|s.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)|];
        }
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
