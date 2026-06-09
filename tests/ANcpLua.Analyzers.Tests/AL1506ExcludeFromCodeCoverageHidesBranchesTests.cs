using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL1506: [ExcludeFromCodeCoverage] on branching code without a Justification.
/// </summary>
public sealed partial class Al1506ExcludeFromCodeCoverageHidesBranchesTests
    : AnalyzerTest<Al1506ExcludeFromCodeCoverageHidesBranchesAnalyzer> {
    [Fact]
    public Task ShouldReportWhenExcludedMethodHasIfBranch() =>
        VerifyAsync("""
                    using System.Diagnostics.CodeAnalysis;

                    public class C {
                        [{|AL1506:ExcludeFromCodeCoverage|}]
                        public int M(int x) {
                            if (x > 0) {
                                return 1;
                            }

                            return 0;
                        }
                    }
                    """);

    [Fact]
    public Task ShouldReportWhenExcludedMethodHasSwitchExpression() =>
        VerifyAsync("""
                    using System.Diagnostics.CodeAnalysis;

                    public class C {
                        [{|AL1506:ExcludeFromCodeCoverage|}]
                        public string M(int x) => x switch {
                            0 => "zero",
                            _ => "other"
                        };
                    }
                    """);

    [Fact]
    public Task ShouldReportWhenExcludedTypeHasBranchingMember() =>
        VerifyAsync("""
                    using System.Diagnostics.CodeAnalysis;

                    [{|AL1506:ExcludeFromCodeCoverage|}]
                    public class C {
                        public int Count(string s) {
                            var n = 0;
                            foreach (var c in s) {
                                n++;
                            }

                            return n;
                        }
                    }
                    """);

    [Fact]
    public Task ShouldReportWhenExcludedPropertyHasConditional() =>
        VerifyAsync("""
                    using System.Diagnostics.CodeAnalysis;

                    public class C {
                        private int _x;

                        [{|AL1506:ExcludeFromCodeCoverage|}]
                        public string Label => _x > 0 ? "pos" : "neg";
                    }
                    """);

    [Fact]
    public Task ShouldReportWhenJustificationIsWhitespace() =>
        VerifyAsync("""
                    using System.Diagnostics.CodeAnalysis;

                    public class C {
                        [{|AL1506:ExcludeFromCodeCoverage(Justification = "   ")|}]
                        public int M(int x) => x > 0 ? 1 : 0;
                    }
                    """);

    [Fact]
    public Task ShouldNotReportWhenJustificationProvided() =>
        VerifyAsync("""
                    using System.Diagnostics.CodeAnalysis;

                    public class C {
                        [ExcludeFromCodeCoverage(Justification = "Windows-only P/Invoke, unreachable on the CI matrix")]
                        public int M(int x) {
                            if (x > 0) {
                                return 1;
                            }

                            return 0;
                        }
                    }
                    """);

    [Fact]
    public Task ShouldNotReportWhenExcludedMethodHasNoBranches() =>
        VerifyAsync("""
                    using System.Diagnostics.CodeAnalysis;

                    public class C {
                        [ExcludeFromCodeCoverage]
                        public int Add(int a, int b) => a + b;
                    }
                    """);

    [Fact]
    public Task ShouldNotReportWhenExcludedTypeIsPlainData() =>
        VerifyAsync("""
                    using System.Diagnostics.CodeAnalysis;

                    [ExcludeFromCodeCoverage]
                    public class Dto {
                        public string Name { get; set; } = "";
                        public int Age { get; set; }
                    }
                    """);

    [Fact]
    public Task ShouldNotReportWhenBranchingMethodIsNotExcluded() =>
        VerifyAsync("""
                    public class C {
                        public int M(int x) {
                            if (x > 0) {
                                return 1;
                            }

                            return 0;
                        }
                    }
                    """);
}
