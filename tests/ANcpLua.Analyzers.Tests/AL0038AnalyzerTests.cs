using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL0038: Use GetOrNull instead of TryGetValue patterns.
/// </summary>
public sealed partial class Al0038AnalyzerTests : AnalyzerTest<Al0038UseGetOrNullAnalyzer> {
    [Fact]
    public Task ShouldReportForTryGetValueNull() => VerifyAsync("""
        using System.Collections.Generic;
        public class C {
            string? M(Dictionary<string, string> dict, string key) =>
                [|dict.TryGetValue(key, out var v) ? v : null|];
        }
        """);

    [Fact]
    public Task ShouldReportForTryGetValueDefault() => VerifyAsync("""
        using System.Collections.Generic;
        public class C {
            int M(Dictionary<string, int> dict, string key) =>
                [|dict.TryGetValue(key, out var v) ? v : default|];
        }
        """);

    [Fact]
    public Task ShouldNotReportForNonTryGetValue() => VerifyAsync("""
        using System.Collections.Generic;
        public class C {
            bool M(Dictionary<string, string> dict, string key) =>
                dict.ContainsKey(key) ? true : false;
        }
        """);

    [Fact]
    public Task ShouldNotReportWhenWhenTrueIsNotOutVar() => VerifyAsync("""
        using System.Collections.Generic;
        public class C {
            string? M(Dictionary<string, string> dict, string key, string other) =>
                dict.TryGetValue(key, out var v) ? other : null;
        }
        """);
}
