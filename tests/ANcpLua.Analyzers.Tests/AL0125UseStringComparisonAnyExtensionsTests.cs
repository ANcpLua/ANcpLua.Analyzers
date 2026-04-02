using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL0125: Collapse chained == into EqualsAnyOrdinal.
/// </summary>
public sealed partial class Al0125UseStringComparisonAnyExtensionsTests
    : AnalyzerTest<Al0125UseStringComparisonAnyExtensionsAnalyzer> {
    [Fact]
    public Task ShouldReportForTwoEqualities() => VerifyAsync("""
        using System;
        public class C {
            bool M(string s) => [|s == "a" || s == "b"|];
        }
        """);

    [Fact]
    public Task ShouldReportForThreeEqualities() => VerifyAsync("""
        using System;
        public class C {
            bool M(string s) => [|s == "a" || s == "b" || s == "c"|];
        }
        """);

    [Fact]
    public Task ShouldNotReportForSingleEquality() => VerifyAsync("""
        using System;
        public class C {
            bool M(string s) => s == "a";
        }
        """);

    [Fact]
    public Task ShouldNotReportForDifferentReceivers() => VerifyAsync("""
        using System;
        public class C {
            bool M(string s, string t) => s == "a" || t == "b";
        }
        """);

    [Fact]
    public Task ShouldNotReportForNonStringEquality() => VerifyAsync("""
        using System;
        public class C {
            bool M(int x) => x == 1 || x == 2;
        }
        """);

    [Fact]
    public Task ShouldNotReportForMixedOperations() => VerifyAsync("""
        using System;
        public class C {
            bool M(string s) => s == "a" || s.Length > 5;
        }
        """);

    [Fact]
    public Task ShouldReportWithReversedConstant() => VerifyAsync("""
        using System;
        public class C {
            bool M(string s) => [|"a" == s || "b" == s|];
        }
        """);
}
