using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL0025: Prefer static lambda.
/// </summary>
public sealed class AL0025AnalyzerTests : AnalyzerTest<AL0025PreferStaticLambdaAnalyzer> {
    [Theory]
    [InlineData("list.Where(x {|AL0025:=>|} x > 0)")]
    [InlineData("list.Select(x {|AL0025:=>|} x.ToString())")]
    [InlineData("list.Any(x {|AL0025:=>|} x == 5)")]
    [InlineData("Func<int, int> f = x {|AL0025:=>|} x * 2;")]
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
