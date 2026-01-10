using ANcpLua.Roslyn.Utilities.Testing;
using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Analyzers.CodeFixes.CodeFixes;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL0014: Prefer pattern matching over equality operators for null and zero comparisons.
/// </summary>
public sealed class AL0014AnalyzerTests : AnalyzerTest<AL0014PreferPatternMatchingAnalyzer> {
    [Theory]
    [InlineData("object? o", "[|o == null|]")]
    [InlineData("object? o", "[|o != null|]")]
    [InlineData("object? o", "[|null == o|]")]
    [InlineData("object? o", "[|null != o|]")]
    public Task ShouldReportNullComparisons(string param, string expr) =>
        VerifyAsync($"public class C {{ void M({param}) {{ _ = {expr}; }} }}");

    [Theory]
    [InlineData("[|x == 0|]")]
    [InlineData("[|x != 0|]")]
    [InlineData("[|0 == x|]")]
    [InlineData("[|0 != x|]")]
    public Task ShouldReportZeroComparisons(string expr) =>
        VerifyAsync($"public class C {{ void M(int x) {{ _ = {expr}; }} }}");

    [Theory]
    [InlineData("object? o", "o is null")]
    [InlineData("object? o", "o is not null")]
    [InlineData("int x", "x is 0")]
    [InlineData("int x", "x is not 0")]
    public Task ShouldNotReportPatternMatching(string param, string expr) =>
        VerifyAsync($"public class C {{ void M({param}) {{ _ = {expr}; }} }}");

    [Theory]
    [InlineData("int x", "x == 1")]
    [InlineData("string s", "s == \"test\"")]
    [InlineData("int x, int y", "x == y")]
    public Task ShouldNotReportOtherComparisons(string param, string expr) =>
        VerifyAsync($"public class C {{ void M({param}) {{ _ = {expr}; }} }}");

    [Theory]
    [InlineData("""
                public class C {
                    void M(object? o) {
                        _ = o switch {
                            null => true,
                            _ => false
                        };
                    }
                }
                """)]
    [InlineData("""
                public class C {
                    void M(object? o) {
                        _ = o is null or string;
                    }
                }
                """)]
    public Task ShouldNotReportInsidePatternContext(string source) => VerifyAsync(source);

    [Theory]
    [InlineData("""
                using System;
                using System.Linq.Expressions;
                public class C {
                    void M() {
                        Expression<Func<string?, bool>> expr = s => s != null;
                    }
                }
                """)]
    [InlineData("""
                using System;
                using System.Linq.Expressions;
                public class C {
                    void M() {
                        Expression<Func<int, bool>> expr = x => x == 0;
                    }
                }
                """)]
    [InlineData("""
                using System;
                using System.Linq.Expressions;
                public class C {
                    void M() {
                        Expression<Func<string?, bool>> expr = s => s == null && s.Length == 0;
                    }
                }
                """)]
    public Task ShouldNotReportInsideExpressionTree(string source) => VerifyAsync(source);
}

/// <summary>
///     Code fix tests for AL0014: Converts equality comparisons to pattern matching.
/// </summary>
public sealed class AL0014CodeFixTests : CodeFixTest<AL0014PreferPatternMatchingAnalyzer, AL0014CodeFixProvider> {
    [Fact]
    public Task ShouldConvertEqualsNullToIsNull() => VerifyAsync(
        """
        public class C {
            void M(object? o) {
                _ = [|o == null|];
            }
        }
        """,
        """
        public class C {
            void M(object? o) {
                _ = o is null;
            }
        }
        """);

    [Fact]
    public Task ShouldConvertNotEqualsNullToIsNotNull() => VerifyAsync(
        """
        public class C {
            void M(object? o) {
                _ = [|o != null|];
            }
        }
        """,
        """
        public class C {
            void M(object? o) {
                _ = o is not null;
            }
        }
        """);

    [Fact]
    public Task ShouldConvertNullEqualsToIsNull() => VerifyAsync(
        """
        public class C {
            void M(object? o) {
                _ = [|null == o|];
            }
        }
        """,
        """
        public class C {
            void M(object? o) {
                _ = o is null;
            }
        }
        """);

    [Fact]
    public Task ShouldConvertNullNotEqualsToIsNotNull() => VerifyAsync(
        """
        public class C {
            void M(object? o) {
                _ = [|null != o|];
            }
        }
        """,
        """
        public class C {
            void M(object? o) {
                _ = o is not null;
            }
        }
        """);

    [Fact]
    public Task ShouldConvertEqualsZeroToIsZero() => VerifyAsync(
        """
        public class C {
            void M(int x) {
                _ = [|x == 0|];
            }
        }
        """,
        """
        public class C {
            void M(int x) {
                _ = x is 0;
            }
        }
        """);

    [Fact]
    public Task ShouldConvertNotEqualsZeroToIsNotZero() => VerifyAsync(
        """
        public class C {
            void M(int x) {
                _ = [|x != 0|];
            }
        }
        """,
        """
        public class C {
            void M(int x) {
                _ = x is not 0;
            }
        }
        """);

    [Fact]
    public Task ShouldConvertZeroEqualsToIsZero() => VerifyAsync(
        """
        public class C {
            void M(int x) {
                _ = [|0 == x|];
            }
        }
        """,
        """
        public class C {
            void M(int x) {
                _ = x is 0;
            }
        }
        """);

    [Fact]
    public Task ShouldPreserveTrivia() => VerifyAsync(
        """
        public class C {
            void M(object? o) {
                // Comment
                _ = [|o == null|]; // Trailing
            }
        }
        """,
        """
        public class C {
            void M(object? o) {
                // Comment
                _ = o is null; // Trailing
            }
        }
        """);
}
