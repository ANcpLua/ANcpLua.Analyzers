using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL0037: Use TryParse extension methods instead of verbose patterns.
/// </summary>
public sealed partial class Al0037UseTryParseExtensionsTests : AnalyzerTest<Al0037UseTryParseExtensionsAnalyzer> {
    [Fact]
    public Task ShouldReportForIntTryParse() => VerifyAsync("""
        using System;
        public class C {
            int? M(string s) => [|int.TryParse(s, out var v) ? v : null|];
        }
        """);

    [Fact]
    public Task ShouldReportForLongTryParse() => VerifyAsync("""
        using System;
        public class C {
            long? M(string s) => [|long.TryParse(s, out var v) ? v : null|];
        }
        """);

    [Fact]
    public Task ShouldReportForGuidTryParse() => VerifyAsync("""
        using System;
        public class C {
            Guid? M(string s) => [|Guid.TryParse(s, out var v) ? v : default(Guid?)|];
        }
        """);

    [Fact]
    public Task ShouldReportForBoolTryParse() => VerifyAsync("""
        using System;
        public class C {
            bool? M(string s) => [|bool.TryParse(s, out var v) ? v : null|];
        }
        """);

    [Fact]
    public Task ShouldNotReportForNonTryParseMethod() => VerifyAsync("""
        using System;
        public class C {
            bool M(string s) => s.StartsWith("test") ? true : false;
        }
        """);
}
