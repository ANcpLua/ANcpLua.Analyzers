using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL1407: Avoid Expression.Compile() in AOT context.
/// </summary>
/// <remarks>
///     The analyzer is gated on MSBuild <c>PublishAot=true</c> or <c>IsAotCompatible=true</c>.
///     In unit tests without a .globalconfig the properties aren't set, so the analyzer correctly
///     produces no diagnostics. The positive case (AOT project calling Expression.Compile) is
///     verified by build-time integration tests — same pattern as AL1406.
/// </remarks>
public sealed partial class Al1407AvoidExpressionCompileTests : AnalyzerTest<Al1407AvoidExpressionCompileAnalyzer> {
    [Fact]
    public Task ShouldNotReportExpressionCompileOutsideAotContext() =>
        VerifyAsync("""
                    using System;
                    using System.Linq.Expressions;

                    public class C {
                        public void M() {
                            Expression<Func<int, int>> expr = x => x + 1;
                            var func = expr.Compile();
                        }
                    }
                    """);

    [Fact]
    public Task ShouldNotReportCompileOnLambdaExpressionVariableOutsideAotContext() =>
        VerifyAsync("""
                    using System;
                    using System.Linq.Expressions;

                    public class C {
                        public void M(LambdaExpression lambda) {
                            var d = lambda.Compile();
                        }
                    }
                    """);

    [Fact]
    public Task ShouldNotReportCompileWithPreferInterpretationOutsideAotContext() =>
        VerifyAsync("""
                    using System;
                    using System.Linq.Expressions;

                    public class C {
                        public void M() {
                            Expression<Func<int, int>> expr = x => x + 1;
                            var func = expr.Compile(true);
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
