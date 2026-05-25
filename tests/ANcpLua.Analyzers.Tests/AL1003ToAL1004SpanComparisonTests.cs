using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Analyzers.CodeFixes.CodeFixes;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL1003: Use pattern matching when comparing Span and a constant.
///     C# 14 allows span == "string" via implicit conversions, but pattern matching
///     (span is "string") expresses intent more clearly.
/// </summary>
public sealed partial class Al1003SpanComparisonTests : AnalyzerTest<Al1003ToAl1004SpanComparisonAnalyzer> {
    [Theory]
    [InlineData("""
                using System;
                public class C {
                    void M(ReadOnlySpan<char> span) {
                        _ = span {|AL1003:== "test"|};
                    }
                }
                """)]
    [InlineData("""
                using System;
                public class C {
                    void M(ReadOnlySpan<char> span) {
                        _ = span {|AL1003:!= "test"|};
                    }
                }
                """)]
    [InlineData("""
                using System;
                public class C {
                    void M(ReadOnlySpan<int> span) {
                        _ = span {|AL1003:== new[] { 1, 2, 3 }|};
                    }
                }
                """)]
    [InlineData("""
                using System;
                public class C {
                    void M(ReadOnlySpan<int> span) {
                        _ = span {|AL1003:== [1, 2, 3]|};
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
///     Tests for AL1004: Use SequenceEqual when comparing Span and a non-constant.
///     For non-constant comparisons, SequenceEqual expresses content comparison intent clearly.
/// </summary>
public sealed partial class Al1004SpanSequenceEqualTests : AnalyzerTest<Al1003ToAl1004SpanComparisonAnalyzer> {
    [Theory]
    [InlineData("""
                using System;
                public class C {
                    void M(ReadOnlySpan<char> span, string other) {
                        _ = span {|AL1004:== other|};
                    }
                }
                """)]
    [InlineData("""
                using System;
                public class C {
                    void M(ReadOnlySpan<int> span, int[] other) {
                        _ = span {|AL1004:== other|};
                    }
                }
                """)]
    [InlineData("""
                using System;
                public class C {
                    void M(ReadOnlySpan<char> span) {
                        string other = GetValue();
                        _ = span {|AL1004:== other|};
                    }
                    string GetValue() => "test";
                }
                """)]
    [InlineData("""
                using System;
                public class C {
                    int[] _arr = { 1, 2, 3 };
                    void M(ReadOnlySpan<int> span) {
                        _ = span {|AL1004:== _arr|};
                    }
                }
                """)]
    [InlineData("""
                using System;
                public class C {
                    void M(ReadOnlySpan<int> span) {
                        _ = span {|AL1004:== new int[2]|};
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
///     Edge case tests for collection expression handling in AL1003/AL1004.
///     Verifies the fix for DescendantNodes().Single() bug.
/// </summary>
public sealed partial class
    Al1003ToAl1004CollectionExpressionEdgeCasesTests : AnalyzerTest<Al1003ToAl1004SpanComparisonAnalyzer> {
    [Theory]
    [InlineData("[1, 2]", "AL1003")]
    [InlineData("[1 + 2, 3]", "AL1003")]
    [InlineData("[]", "AL1003")]
    public Task ShouldReportAl1003ForConstantCollections(string collection, string expectedDiagnostic) =>
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
    public Task ShouldReportAl1004ForNonConstantCollections(string collection) =>
        VerifyAsync($$"""
                      using System;
                      class C {
                          int[] other = [1];
                          int[] x = [2];
                          void M(ReadOnlySpan<int> span, int a, int b) {
                              _ = span {|AL1004:== {{collection}}|};
                          }
                          int GetValue() => 1;
                      }
                      """);
}

/// <summary>
///     Code fix tests for AL1003: Converts Span equality to pattern matching.
/// </summary>
public sealed partial class Al1003SpanPatternMatchingCodeFixTests : CodeFixTest<Al1003ToAl1004SpanComparisonAnalyzer, Al1003SpanPatternMatchingCodeFixProvider> {
    [Fact]
    public Task ShouldConvertStringLiteralToPatternMatching() => VerifyAsync(
        """
        using System;
        public class C {
            void M(ReadOnlySpan<char> span) {
                _ = span {|AL1003:== "test"|};
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
                _ = span {|AL1003:== [1, 2, 3]|};
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
                _ = span {|AL1003:== new[] { 1, 2, 3 }|};
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
                _ = span {|AL1003:== new int[] { 1, 2, 3 }|};
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
///     Code fix tests for AL1004: Converts Span equality to SequenceEqual.
/// </summary>
public sealed partial class Al1004UseSequenceEqualCodeFixTests : CodeFixTest<Al1003ToAl1004SpanComparisonAnalyzer, Al1004UseSequenceEqualCodeFixProvider> {
    [Fact]
    public Task ShouldConvertToSequenceEqual() => VerifyAsync(
        """
        using System;
        public class C {
            void M(ReadOnlySpan<char> span, string other) {
                _ = span {|AL1004:== other|};
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
                _ = span {|AL1004:== arr|};
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
                _ = span {|AL1004:== _arr|};
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

    [Fact]
    public Task ShouldConvertArrayCreationWithoutInitializerToSequenceEqual() => VerifyAsync(
        """
        using System;
        public class C {
            void M(ReadOnlySpan<int> span) {
                _ = span {|AL1004:== new int[2]|};
            }
        }
        """,
        """
        using System;
        public class C {
            void M(ReadOnlySpan<int> span) {
                _ = span.SequenceEqual(new int[2]);
            }
        }
        """);
}
