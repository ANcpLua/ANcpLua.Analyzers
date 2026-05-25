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
}
