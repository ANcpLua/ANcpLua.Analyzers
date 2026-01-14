using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Analyzers.CodeFixes.CodeFixes;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL0025: Prefer static lambda.
/// </summary>
public sealed class AL0025AnalyzerTests : AnalyzerTest<AL0025PreferStaticLambdaAnalyzer> {
    [Theory]
    [InlineData("list.Where({|AL0025:x => x > 0|})")]
    [InlineData("list.Select({|AL0025:x => x.ToString()|})")]
    [InlineData("list.Any({|AL0025:x => x == 5|})")]
    [InlineData("Func<int, int> f = {|AL0025:x => x * 2|};")]
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
}

/// <summary>
///     Code fix tests for AL0025: Makes lambda static.
/// </summary>
public sealed class AL0025CodeFixTests : CodeFixTest<AL0025PreferStaticLambdaAnalyzer, AL0025StaticLambdaCodeFixProvider> {
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
