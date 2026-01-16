using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Analyzers.CodeFixes.CodeFixes;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL0004: Use pattern matching when comparing Span and a constant.
///     C# 14 allows span == "string" via implicit conversions, but pattern matching
///     (span is "string") expresses intent more clearly.
/// </summary>
public sealed partial class Al0004AnalyzerTests : AnalyzerTest<Al0004ToAl0005SpanComparisonAnalyzer> {
    [Theory]
    [InlineData("""
                using System;
                public class C {
                    void M(ReadOnlySpan<char> span) {
                        _ = span {|AL0004:== "test"|};
                    }
                }
                """)]
    [InlineData("""
                using System;
                public class C {
                    void M(ReadOnlySpan<char> span) {
                        _ = span {|AL0004:!= "test"|};
                    }
                }
                """)]
    [InlineData("""
                using System;
                public class C {
                    void M(ReadOnlySpan<int> span) {
                        _ = span {|AL0004:== new[] { 1, 2, 3 }|};
                    }
                }
                """)]
    [InlineData("""
                using System;
                public class C {
                    void M(ReadOnlySpan<int> span) {
                        _ = span {|AL0004:== [1, 2, 3]|};
                    }
                }
                """)]
    public Task ShouldReportConstantComparison(string source) => VerifyAsync(source);

    [Theory]
    [InlineData("""
                using System;
                public class C {
                    void M(ReadOnlySpan<char> span) {
                        _ = span is "test";
                    }
                }
                """)]
    [InlineData("""
                using System;
                public class C {
                    void M(ReadOnlySpan<int> span) {
                        _ = span is [1, 2, 3];
                    }
                }
                """)]
    public Task ShouldNotReportPatternMatching(string source) => VerifyAsync(source);
}

/// <summary>
///     Tests for AL0005: Use SequenceEqual when comparing Span and a non-constant.
///     For non-constant comparisons, SequenceEqual expresses content comparison intent clearly.
/// </summary>
public sealed partial class Al0005AnalyzerTests : AnalyzerTest<Al0004ToAl0005SpanComparisonAnalyzer> {
    [Theory]
    [InlineData("""
                using System;
                public class C {
                    void M(ReadOnlySpan<char> span, string other) {
                        _ = span {|AL0005:== other|};
                    }
                }
                """)]
    [InlineData("""
                using System;
                public class C {
                    void M(ReadOnlySpan<int> span, int[] other) {
                        _ = span {|AL0005:== other|};
                    }
                }
                """)]
    [InlineData("""
                using System;
                public class C {
                    void M(ReadOnlySpan<char> span) {
                        string other = GetValue();
                        _ = span {|AL0005:== other|};
                    }
                    string GetValue() => "test";
                }
                """)]
    [InlineData("""
                using System;
                public class C {
                    int[] _arr = { 1, 2, 3 };
                    void M(ReadOnlySpan<int> span) {
                        _ = span {|AL0005:== _arr|};
                    }
                }
                """)]
    public Task ShouldReportNonConstantComparison(string source) => VerifyAsync(source);

    [Theory]
    [InlineData("""
                using System;
                public class C {
                    void M(ReadOnlySpan<char> span, string other) {
                        _ = span.SequenceEqual(other);
                    }
                }
                """)]
    public Task ShouldNotReportSequenceEqual(string source) => VerifyAsync(source);
}

/// <summary>
///     Edge case tests for collection expression handling in AL0004/AL0005.
///     Verifies the fix for DescendantNodes().Single() bug.
/// </summary>
public sealed partial class
    Al0004ToAl0005CollectionExpressionEdgeCasesTests : AnalyzerTest<Al0004ToAl0005SpanComparisonAnalyzer> {
    [Theory]
    [InlineData("[1, 2]", "AL0004")]
    [InlineData("[1 + 2, 3]", "AL0004")]
    [InlineData("[]", "AL0004")]
    public Task ShouldReportAl0004ForConstantCollections(string collection, string expectedDiagnostic) =>
        VerifyAsync($$"""
                      using System;
                      class C {
                          void M(ReadOnlySpan<int> span) {
                              _ = span {|{{expectedDiagnostic}}:== {{collection}}|};
                          }
                      }
                      """);

    [Theory]
    [InlineData("[a, b]")] // Identifiers - non-constant
    [InlineData("[GetValue()]")] // Method call - non-constant
    [InlineData("[..other]")] // Spread element - not ExpressionElementSyntax
    [InlineData("[1, ..x, 2]")] // Mixed with spread - non-constant
    public Task ShouldReportAl0005ForNonConstantCollections(string collection) =>
        VerifyAsync($$"""
                      using System;
                      class C {
                          int[] other = [1];
                          int[] x = [2];
                          void M(ReadOnlySpan<int> span, int a, int b) {
                              _ = span {|AL0005:== {{collection}}|};
                          }
                          int GetValue() => 1;
                      }
                      """);
}

/// <summary>
///     Code fix tests for AL0004: Converts Span equality to pattern matching.
/// </summary>
public sealed partial class Al0004CodeFixTests : CodeFixTest<Al0004ToAl0005SpanComparisonAnalyzer, Al0004CodeFixProvider> {
    [Fact]
    public Task ShouldConvertStringLiteralToPatternMatching() => VerifyAsync(
        """
        using System;
        public class C {
            void M(ReadOnlySpan<char> span) {
                _ = span {|AL0004:== "test"|};
            }
        }
        """,
        """
        using System;
        public class C {
            void M(ReadOnlySpan<char> span) {
                _ = span is "test";
            }
        }
        """);

    [Fact]
    public Task ShouldConvertCollectionExpressionToListPattern() => VerifyAsync(
        """
        using System;
        public class C {
            void M(ReadOnlySpan<int> span) {
                _ = span {|AL0004:== [1, 2, 3]|};
            }
        }
        """,
        """
        using System;
        public class C {
            void M(ReadOnlySpan<int> span) {
                _ = span is [1, 2, 3];
            }
        }
        """);

    [Fact]
    public Task ShouldConvertArrayCreationToListPattern() => VerifyAsync(
        """
        using System;
        public class C {
            void M(ReadOnlySpan<int> span) {
                _ = span {|AL0004:== new[] { 1, 2, 3 }|};
            }
        }
        """,
        """
        using System;
        public class C {
            void M(ReadOnlySpan<int> span) {
                _ = span is [1, 2, 3];
            }
        }
        """);

    [Fact]
    public Task ShouldConvertExplicitArrayCreationToListPattern() => VerifyAsync(
        """
        using System;
        public class C {
            void M(ReadOnlySpan<int> span) {
                _ = span {|AL0004:== new int[] { 1, 2, 3 }|};
            }
        }
        """,
        """
        using System;
        public class C {
            void M(ReadOnlySpan<int> span) {
                _ = span is [1, 2, 3];
            }
        }
        """);
}

/// <summary>
///     Code fix tests for AL0005: Converts Span equality to SequenceEqual.
/// </summary>
public sealed partial class Al0005CodeFixTests : CodeFixTest<Al0004ToAl0005SpanComparisonAnalyzer, Al0005CodeFixProvider> {
    [Fact]
    public Task ShouldConvertToSequenceEqual() => VerifyAsync(
        """
        using System;
        public class C {
            void M(ReadOnlySpan<char> span, string other) {
                _ = span {|AL0005:== other|};
            }
        }
        """,
        """
        using System;
        public class C {
            void M(ReadOnlySpan<char> span, string other) {
                _ = span.SequenceEqual(other);
            }
        }
        """);

    [Fact]
    public Task ShouldConvertArrayVariableToSequenceEqual() => VerifyAsync(
        """
        using System;
        public class C {
            void M(ReadOnlySpan<int> span, int[] arr) {
                _ = span {|AL0005:== arr|};
            }
        }
        """,
        """
        using System;
        public class C {
            void M(ReadOnlySpan<int> span, int[] arr) {
                _ = span.SequenceEqual(arr);
            }
        }
        """);

    [Fact]
    public Task ShouldConvertFieldToSequenceEqual() => VerifyAsync(
        """
        using System;
        public class C {
            int[] _arr = { 1, 2, 3 };
            void M(ReadOnlySpan<int> span) {
                _ = span {|AL0005:== _arr|};
            }
        }
        """,
        """
        using System;
        public class C {
            int[] _arr = { 1, 2, 3 };
            void M(ReadOnlySpan<int> span) {
                _ = span.SequenceEqual(_arr);
            }
        }
        """);
}
