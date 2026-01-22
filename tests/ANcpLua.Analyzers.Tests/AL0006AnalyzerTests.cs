using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

public sealed class Al0006AnalyzerTests : AnalyzerTest<Al0006FieldNameConflictWithPrimaryConstructorAnalyzer> {
    [Fact]
    public Task ShouldReportDiagnostic() =>
        VerifyAsync("""
                    public class TestClass(int value)
                    {
                        private int [|value|];
                    }
                    """);

    [Fact]
    public Task ShouldNotReportWhenNoConflict() =>
        VerifyAsync("""
                    public class TestClass(int value)
                    {
                        private int _value;
                    }
                    """);
}
