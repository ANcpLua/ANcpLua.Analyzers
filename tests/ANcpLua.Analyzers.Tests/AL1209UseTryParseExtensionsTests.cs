using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Analyzers.CodeFixes.CodeFixes;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL1209: Use TryParse extension methods instead of verbose patterns.
/// </summary>
public sealed partial class Al1209UseTryParseExtensionsTests : AnalyzerTest<Al1209UseTryParseExtensionsAnalyzer> {
    // AL1209 only fires when ANcpLua.Roslyn.Utilities.TryExtensions (which owns TryParseInt32 etc.)
    // is present and accessible. Each case appends this stub so the gate is open; the dedicated
    // ShouldNotReportWhenTryExtensionsNotReferenced case omits it to assert the gate itself.
    private const string Stub = """
                                namespace ANcpLua.Roslyn.Utilities { internal static class TryExtensions { } }
                                """;

    private static Task Verify(string body) => VerifyAsync($$"""
                                                            {{body}}
                                                            {{Stub}}
                                                            """);

    [Fact]
    public Task ShouldReportForIntTryParse() => Verify("""
        using System;
        public class C {
            int? M(string s) => [|int.TryParse(s, out var v) ? v : null|];
        }
        """);

    [Fact]
    public Task ShouldReportForLongTryParse() => Verify("""
        using System;
        public class C {
            long? M(string s) => [|long.TryParse(s, out var v) ? v : null|];
        }
        """);

    [Fact]
    public Task ShouldReportForGuidTryParse() => Verify("""
        using System;
        public class C {
            Guid? M(string s) => [|Guid.TryParse(s, out var v) ? v : default(Guid?)|];
        }
        """);

    [Fact]
    public Task ShouldReportForBoolTryParse() => Verify("""
        using System;
        public class C {
            bool? M(string s) => [|bool.TryParse(s, out var v) ? v : null|];
        }
        """);

    [Fact]
    public Task ShouldNotReportForNonTryParseMethod() => Verify("""
                    using System;
                    public class C {
                        bool M(string s) => s.StartsWith("test") ? true : false;
                    }
                    """);

    [Fact]
    public Task ShouldNotReportForWrongTrueBranchExpression() => Verify("""
                    using System;
                    public class C {
                        int? M(string s) {
                            int parsed;
                            return int.TryParse(s, out parsed) ? 0 : null;
                        }
                    }
                    """);

    [Fact]
    public Task ShouldNotReportForDifferentOutFallback() => Verify("""
                    using System;
                    public class C {
                        int? M(string s) => int.TryParse(s, out var v) ? v : 0;
                    }
                    """);

    [Fact]
    public Task ShouldNotReportForSpanOverload() => Verify("""
                    using System;
                    using System.Globalization;
                    public class C {
                        int? M(string s, IFormatProvider provider) =>
                            int.TryParse(s, NumberStyles.Integer, provider, out var v) ? v : null;
                    }
                    """);

    // The consumer scenario: a project that does not reference ANcpLua.Roslyn.Utilities has no
    // TryExtensions type, so AL1209 must stay silent on correct TryParse patterns.
    [Fact]
    public Task ShouldNotReportWhenTryExtensionsNotReferenced() =>
        VerifyAsync("""
                    using System;
                    public class C {
                        int? M(string s) => int.TryParse(s, out var v) ? v : null;
                    }
                    """);
}

/// <summary>
///     Code fix tests for AL1209: use TryParse extension methods.
/// </summary>
public sealed partial class Al1209UseTryParseExtensionsCodeFixTests
    : CodeFixTest<Al1209UseTryParseExtensionsAnalyzer, Al1209UseTryParseExtensionsCodeFixProvider> {
    private const string TryParseExtensionsPolyfill = """
                                                  using ANcpLua.Roslyn.Utilities;
                                                  namespace ANcpLua.Roslyn.Utilities {
                                                      public static class TryExtensions {
                                                          public static int? TryParseInt32(this string value) => null;
                                                          public static long? TryParseInt64(this string value) => null;
                                                          public static bool? TryParseBool(this string value) => null;
                                                          public static System.Guid? TryParseGuid(this string value) => null;
                                                      }
                                                  }
                                                  """;

    [Fact]
    public Task ShouldFixForIntTryParse() =>
        VerifyAsync(
            $$"""
            using System;
            using ANcpLua.Roslyn.Utilities;
            {{TryParseExtensionsPolyfill}}
            public class C {
                int? M(string s) => [|int.TryParse(s, out var v) ? v : null|];
            }
            """,
            $$"""
            using System;
            using ANcpLua.Roslyn.Utilities;
            {{TryParseExtensionsPolyfill}}
            public class C {
                int? M(string s) => s.TryParseInt32();
            }
            """);

    [Fact]
    public Task ShouldFixForGuidTryParse() =>
        VerifyAsync(
            $$"""
            using System;
            using ANcpLua.Roslyn.Utilities;
            {{TryParseExtensionsPolyfill}}
            public class C {
                Guid? M(string s) => [|Guid.TryParse(s, out var v) ? v : default(Guid?)|];
            }
            """,
            $$"""
            using System;
            using ANcpLua.Roslyn.Utilities;
            {{TryParseExtensionsPolyfill}}
            public class C {
                Guid? M(string s) => s.TryParseGuid();
            }
            """);
}
