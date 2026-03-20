using AnalyzerTestBase = ANcpLua.Roslyn.Utilities.Testing.AnalyzerTest<ANcpLua.Analyzers.Analyzers.Al0114PreferTryParseAnalyzer>;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL0114: Prefer TryParse over Parse.
/// </summary>
public sealed partial class Al0114PreferTryParseTests : AnalyzerTestBase {
    [Fact]
    public Task ShouldReportIntParse() =>
        VerifyAsync("""
                    using System;

                    public class C {
                        public void M(string s) {
                            var x = [|int.Parse(s)|];
                        }
                    }
                    """);

    [Fact]
    public Task ShouldReportDateTimeParse() =>
        VerifyAsync("""
                    using System;

                    public class C {
                        public void M(string s) {
                            var x = [|DateTime.Parse(s)|];
                        }
                    }
                    """);

    [Fact]
    public Task ShouldReportGuidParse() =>
        VerifyAsync("""
                    using System;

                    public class C {
                        public void M(string s) {
                            var x = [|Guid.Parse(s)|];
                        }
                    }
                    """);

    [Fact]
    public Task ShouldNotReportInsideTryCatchFormatException() =>
        VerifyAsync("""
                    using System;

                    public class C {
                        public void M(string s) {
                            try {
                                var x = int.Parse(s);
                            } catch (FormatException) {
                            }
                        }
                    }
                    """);

    [Fact]
    public Task ShouldNotReportTryParse() =>
        VerifyAsync("""
                    using System;

                    public class C {
                        public void M(string s) {
                            int.TryParse(s, out _);
                        }
                    }
                    """);

    [Fact]
    public Task ShouldNotReportCustomTypeParse() =>
        VerifyAsync("""
                    using System;

                    public class MyType {
                        public static MyType Parse(string s) => new MyType();
                    }

                    public class C {
                        public void M(string s) {
                            var x = MyType.Parse(s);
                        }
                    }
                    """);
}
