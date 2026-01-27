using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL0039: Use StringComparison extensions for clearer intent.
/// </summary>
public sealed partial class Al0039AnalyzerTests : AnalyzerTest<Al0039UseStringComparisonExtensionsAnalyzer> {
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
}
