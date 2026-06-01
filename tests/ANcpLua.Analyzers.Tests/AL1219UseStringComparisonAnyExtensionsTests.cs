using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL1219: Collapse chained == into EqualsAnyOrdinal.
/// </summary>
public sealed partial class Al1219UseStringComparisonAnyExtensionsTests
    : AnalyzerTest<Al1219UseStringComparisonAnyExtensionsAnalyzer> {
    // AL1219 only fires when ANcpLua.Roslyn.Utilities.StringComparisonExtensions (which owns
    // EqualsAnyOrdinal) is present and accessible. Each case appends this stub so the gate is open;
    // the dedicated ShouldNotReportWhenStringComparisonExtensionsNotReferenced case omits it to assert
    // the gate itself.
    private const string Stub = """
                                namespace ANcpLua.Roslyn.Utilities { internal static class StringComparisonExtensions { } }
                                """;

    private static Task Verify(string body) => VerifyAsync($$"""
                                                            {{body}}
                                                            {{Stub}}
                                                            """);

    [Fact]
    public Task ShouldReportForTwoEqualities() => Verify("""
        using System;
        public class C {
            bool M(string s) => [|s == "a" || s == "b"|];
        }
        """);

    [Fact]
    public Task ShouldReportForThreeEqualities() => Verify("""
        using System;
        public class C {
            bool M(string s) => [|s == "a" || s == "b" || s == "c"|];
        }
        """);

    [Fact]
    public Task ShouldNotReportForSingleEquality() => Verify("""
        using System;
        public class C {
            bool M(string s) => s == "a";
        }
        """);

    [Fact]
    public Task ShouldNotReportForDifferentReceivers() => Verify("""
        using System;
        public class C {
            bool M(string s, string t) => s == "a" || t == "b";
        }
        """);

    [Fact]
    public Task ShouldNotReportForNonStringEquality() => Verify("""
        using System;
        public class C {
            bool M(int x) => x == 1 || x == 2;
        }
        """);

    [Fact]
    public Task ShouldNotReportForMixedOperations() => Verify("""
        using System;
        public class C {
            bool M(string s) => s == "a" || s.Length > 5;
        }
        """);

    [Fact]
    public Task ShouldReportWithReversedConstant() => Verify("""
        using System;
        public class C {
            bool M(string s) => [|"a" == s || "b" == s|];
        }
        """);

    // The consumer scenario: a project that does not reference ANcpLua.Roslyn.Utilities has no
    // StringComparisonExtensions type, so AL1219 must stay silent on chained == patterns.
    [Fact]
    public Task ShouldNotReportWhenStringComparisonExtensionsNotReferenced() =>
        VerifyAsync("""
                    using System;
                    public class C {
                        bool M(string s) => s == "a" || s == "b";
                    }
                    """);
}
