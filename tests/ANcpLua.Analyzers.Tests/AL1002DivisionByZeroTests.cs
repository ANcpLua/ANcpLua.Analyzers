using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

public sealed partial class Al1002DivisionByZeroTests : AnalyzerTest<Al1002DontDivideByConstantZeroAnalyzer> {
    [Theory]
    [InlineData("int i", "i / 0")]
    [InlineData("int i", "i % 0")]
    [InlineData("long i", "i / 0L")]
    [InlineData("decimal d", "d / 0.0m")]
    [InlineData("System.Int128 i", "i / (System.Int128)0")]
    [InlineData("System.UInt128 u", "u / (System.UInt128)0")]
    public Task ShouldReportDiagnostic(string param, string expr) =>
        VerifyAsync($"public class C {{ void M({param}) {{ _ = [|{expr}|]; }} }}");

    [Theory]
    [InlineData("int i", "i / 1")]
    [InlineData("int i, int j", "i / j")]
    [InlineData("double d", "d / 0.0")]
    [InlineData("float f", "f % 0f")]
    [InlineData("System.Int128 i", "i / (System.Int128)1")]
    public Task ShouldNotReportDiagnostic(string param, string expr) =>
        VerifyAsync($"public class C {{ void M({param}) {{ _ = {expr}; }} }}");

    [Fact]
    public Task ShouldNotReportForUserDefinedInt128OutsideSystem() =>
        VerifyAsync("""
                    namespace MyNs {
                        public readonly struct Int128 {
                            public static Int128 operator /(Int128 left, int right) => left;
                        }
                    }

                    public class C {
                        void M(MyNs.Int128 value) {
                            _ = value / 0;
                        }
                    }
                    """);
}
