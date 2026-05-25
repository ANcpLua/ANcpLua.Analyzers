using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Analyzers.CodeFixes.CodeFixes;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL1209: Use TryParse extension methods instead of verbose patterns.
/// </summary>
public sealed partial class Al1209UseTryParseExtensionsTests : AnalyzerTest<Al1209UseTryParseExtensionsAnalyzer> {
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

    [Fact]
    public Task ShouldNotReportForWrongTrueBranchExpression() => VerifyAsync("""
                    using System;
                    public class C {
                        int? M(string s) {
                            int parsed;
                            return int.TryParse(s, out parsed) ? 0 : null;
                        }
                    }
                    """);

    [Fact]
    public Task ShouldNotReportForDifferentOutFallback() => VerifyAsync("""
                    using System;
                    public class C {
                        int? M(string s) => int.TryParse(s, out var v) ? v : 0;
                    }
                    """);

    [Fact]
    public Task ShouldNotReportForSpanOverload() => VerifyAsync("""
                    using System;
                    using System.Globalization;
                    public class C {
                        int? M(string s, IFormatProvider provider) =>
                            int.TryParse(s, NumberStyles.Integer, provider, out var v) ? v : null;
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
                                                      public static class TryParseExtensions {
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
