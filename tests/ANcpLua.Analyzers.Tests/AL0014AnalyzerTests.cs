using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Analyzers.CodeFixes.CodeFixes;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL0014: Prefer pattern matching over equality operators for null and zero comparisons.
/// </summary>
public sealed class AL0014AnalyzerTests : ALAnalyzerTest<AL0014PreferPatternMatchingAnalyzer> {
    [Theory]
    [InlineData("""
                public class C {
                    void M(object? o) {
                        _ = [|o == null|];
                    }
                }
                """)]
    [InlineData("""
                public class C {
                    void M(object? o) {
                        _ = [|o != null|];
                    }
                }
                """)]
    [InlineData("""
                public class C {
                    void M(object? o) {
                        _ = [|null == o|];
                    }
                }
                """)]
    [InlineData("""
                public class C {
                    void M(object? o) {
                        _ = [|null != o|];
                    }
                }
                """)]
    public Task ShouldReportNullComparisons(string source) => VerifyAsync(source);

    [Theory]
    [InlineData("""
                public class C {
                    void M(int x) {
                        _ = [|x == 0|];
                    }
                }
                """)]
    [InlineData("""
                public class C {
                    void M(int x) {
                        _ = [|x != 0|];
                    }
                }
                """)]
    [InlineData("""
                public class C {
                    void M(int x) {
                        _ = [|0 == x|];
                    }
                }
                """)]
    [InlineData("""
                public class C {
                    void M(int x) {
                        _ = [|0 != x|];
                    }
                }
                """)]
    public Task ShouldReportZeroComparisons(string source) => VerifyAsync(source);

    [Theory]
    [InlineData("""
                public class C {
                    void M(object? o) {
                        _ = o is null;
                    }
                }
                """)]
    [InlineData("""
                public class C {
                    void M(object? o) {
                        _ = o is not null;
                    }
                }
                """)]
    [InlineData("""
                public class C {
                    void M(int x) {
                        _ = x is 0;
                    }
                }
                """)]
    [InlineData("""
                public class C {
                    void M(int x) {
                        _ = x is not 0;
                    }
                }
                """)]
    public Task ShouldNotReportPatternMatching(string source) => VerifyAsync(source);

    [Theory]
    [InlineData("""
                public class C {
                    void M(int x) {
                        _ = x == 1;
                    }
                }
                """)]
    [InlineData("""
                public class C {
                    void M(string s) {
                        _ = s == "test";
                    }
                }
                """)]
    [InlineData("""
                public class C {
                    void M(int x, int y) {
                        _ = x == y;
                    }
                }
                """)]
    public Task ShouldNotReportOtherComparisons(string source) => VerifyAsync(source);

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
}

/// <summary>
///     Code fix tests for AL0014: Converts equality comparisons to pattern matching.
/// </summary>
public sealed class AL0014CodeFixTests : ALCodeFixTest<AL0014PreferPatternMatchingAnalyzer, AL0014CodeFixProvider> {
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
