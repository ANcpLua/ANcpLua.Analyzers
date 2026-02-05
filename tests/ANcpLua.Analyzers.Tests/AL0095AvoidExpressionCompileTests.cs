using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL0095: Avoid Expression.Compile() in AOT context.
/// </summary>
public sealed partial class Al0095AvoidExpressionCompileTests : AnalyzerTest<Al0095AvoidExpressionCompileAnalyzer> {
    [Fact]
    public Task ShouldReportExpressionCompile() =>
        VerifyAsync("""
                    using System;
                    using System.Linq.Expressions;

                    public class C {
                        public void M() {
                            Expression<Func<int, int>> expr = x => x + 1;
                            var func = {|AL0095:expr.Compile()|};
                        }
                    }
                    """);

    [Fact]
    public Task ShouldReportCompileOnLambdaExpressionVariable() =>
        VerifyAsync("""
                    using System;
                    using System.Linq.Expressions;

                    public class C {
                        public void M(LambdaExpression lambda) {
                            var d = {|AL0095:lambda.Compile()|};
                        }
                    }
                    """);

    [Fact]
    public Task ShouldReportCompileWithPreferInterpretation() =>
        VerifyAsync("""
                    using System;
                    using System.Linq.Expressions;

                    public class C {
                        public void M() {
                            Expression<Func<int, int>> expr = x => x + 1;
                            var func = {|AL0095:expr.Compile(true)|};
                        }
                    }
                    """);

    [Fact]
    public Task ShouldNotReportNonExpressionCompile() =>
        VerifyAsync("""
                    using System.Text.RegularExpressions;

                    public class C {
                        public Regex Compile() => new Regex("test", RegexOptions.Compiled);
                    }
                    """);

    [Fact]
    public Task ShouldNotReportOtherMethodOnExpression() =>
        VerifyAsync("""
                    using System;
                    using System.Linq.Expressions;

                    public class C {
                        public void M() {
                            Expression<Func<int, int>> expr = x => x + 1;
                            var s = expr.ToString();
                        }
                    }
                    """);

    [Fact]
    public Task ShouldNotReportCompileMethodOnUnrelatedType() =>
        VerifyAsync("""
                    public class Compiler {
                        public void Compile() { }
                    }

                    public class C {
                        public void M() {
                            var compiler = new Compiler();
                            compiler.Compile();
                        }
                    }
                    """);
}
