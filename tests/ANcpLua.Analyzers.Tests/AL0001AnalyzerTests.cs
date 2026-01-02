using ANcpLua.Analyzers.Analyzers;

namespace ANcpLua.Analyzers.Tests;

public sealed class
    AL0001AnalyzerTests : ALAnalyzerTest<AL0001ProhibitPrimaryConstructorParameterReassignmentAnalyzer> {
    [Theory]
    [InlineData("int i", "[|i|] = 10")]
    [InlineData("int i", "[|i|] += 10")]
    [InlineData("int i", "[|i|]++")]
    [InlineData("string? s", "[|s|] ??= string.Empty")]
    public Task ShouldReportDiagnostic(string param, string statement) =>
        VerifyAsync($"public class C({param}) {{ void M() {{ {statement}; }} }}");
}
