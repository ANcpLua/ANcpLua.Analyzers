using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

public sealed partial class
    Al1000PrimaryConstructorReassignmentTests : AnalyzerTest<Al1000ProhibitPrimaryConstructorParameterReassignmentAnalyzer> {
    [Theory]
    [InlineData("int i", "[|i|] = 10")]
    [InlineData("int i", "[|i|] += 10")]
    [InlineData("int i", "[|i|]++")]
    [InlineData("string? s", "[|s|] ??= string.Empty")]
    public Task ShouldReportDiagnostic(string param, string statement) =>
        VerifyAsync($"public class C({param}) {{ void M() {{ {statement}; }} }}");
}
