using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

public sealed class Al0003AnalyzerTests : AnalyzerTest<Al0003DontDivideByConstantZeroAnalyzer> {
    [Theory]
    [InlineData("int i", "i / 0")]
    [InlineData("int i", "i % 0")]
    [InlineData("long i", "i / 0L")]
    [InlineData("decimal d", "d / 0.0m")]
    public Task ShouldReportDiagnostic(string param, string expr) =>
        VerifyAsync($"public class C {{ void M({param}) {{ _ = [|{expr}|]; }} }}");
}
