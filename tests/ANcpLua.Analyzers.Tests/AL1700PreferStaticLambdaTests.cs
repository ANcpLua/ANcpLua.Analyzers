using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Analyzers.CodeFixes.CodeFixes;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL1700: Prefer static lambda.
/// </summary>
public sealed partial class Al1700PreferStaticLambdaTests : AnalyzerTest<Al1700PreferStaticLambdaAnalyzer> {
    [Theory]
    [InlineData("list.Where({|AL1700:x => x > 0|})")]
    [InlineData("list.Select({|AL1700:x => x.ToString()|})")]
    [InlineData("list.Any({|AL1700:x => x == 5|})")]
    [InlineData("Func<int, int> f = {|AL1700:x => x * 2|};")]
    public Task ShouldReportLambdaThatCanBeStatic(string expr) =>
        VerifyAsync($$"""
                      using System;
                      using System.Linq;
                      public class C {
                          void M() {
                              var list = new[] { 1, 2, 3 };
                              {{expr}};
                          }
                      }
                      """);

    [Theory]
    [InlineData("list.Where(static x => x > 0)")]
    [InlineData("list.Where(x => x > _field)")]
    [InlineData("list.Where(x => x > Property)")]
    [InlineData("list.Where(x => InstanceMethod(x))")]
    public Task ShouldNotReportLambdaThatCannotBeStatic(string expr) =>
        VerifyAsync($$"""
                      using System;
                      using System.Linq;
                      public class C {
                          private int _field = 10;
                          public int Property => 20;
                          private bool InstanceMethod(int x) => x > 0;
                          void M() {
                              var list = new[] { 1, 2, 3 };
                              {{expr}};
                          }
                      }
                      """);

    [Fact]
    public Task ShouldNotReportLambdaCapturingLocalVariable() =>
        VerifyAsync("""
                    using System;
                    using System.Linq;
                    public class C {
                        void M() {
                            var list = new[] { 1, 2, 3 };
                            int threshold = 5;
                            list.Where(x => x > threshold);
                        }
                    }
                    """);

    [Fact]
    public Task ShouldNotReportLambdaUsingThis() =>
        VerifyAsync("""
                    using System;
                    using System.Linq;
                    public class C {
                        private int _value = 10;
                        void M() {
                            var list = new[] { 1, 2, 3 };
                            list.Where(x => x > this._value);
                        }
                    }
                    """);

    [Fact]
    public Task ShouldReportNestedLambdaThatCanBeStatic() =>
        VerifyAsync("""
                    using System;
                    public class C {
                        void M() {
                            Action outer = static () => {
                                Func<int, int> inner = {|AL1700:x => x * 2|};
                            };
                        }
                    }
                    """);

    [Fact]
    public Task ShouldNotReportNestedLambdaCapturingOuterParameter() =>
        VerifyAsync("""
                    using System;
                    public class C {
                        void M() {
                            Func<int, Action> outer = static y => () => {
                                var z = y; // captures y from outer static lambda
                            };
                        }
                    }
                    """);
}

/// <summary>
///     Code fix tests for AL1700: Makes lambda static.
/// </summary>
public sealed partial class
    Al1700CodeFixTests : CodeFixTest<Al1700PreferStaticLambdaAnalyzer, Al1700StaticLambdaCodeFixProvider> {
    [Fact]
    public Task ShouldAddStaticToSimpleLambda() =>
        VerifyAsync(
            "using System; public class C { Func<int, int> f = [|x => x * 2|]; }",
            "using System; public class C { Func<int, int> f = static x => x * 2; }");

    [Fact]
    public Task ShouldAddStaticToParenthesizedLambda() =>
        VerifyAsync(
            "using System; public class C { Func<int, int, int> f = [|(x, y) => x + y|]; }",
            "using System; public class C { Func<int, int, int> f = static (x, y) => x + y; }");

    [Fact]
    public Task ShouldFixMultipleLambdasInFile() =>
        VerifyAsync(
            """
            using System;
            using System.Linq;
            public class C {
                void M() {
                    var list = new[] { 1, 2, 3 };
                    list.Where([|x => x > 0|]).Select([|x => x * 2|]);
                }
            }
            """,
            """
            using System;
            using System.Linq;
            public class C {
                void M() {
                    var list = new[] { 1, 2, 3 };
                    list.Where(static x => x > 0).Select(static x => x * 2);
                }
            }
            """);
}
