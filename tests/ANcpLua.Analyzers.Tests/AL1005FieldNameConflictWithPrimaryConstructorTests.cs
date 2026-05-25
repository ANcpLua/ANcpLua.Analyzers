using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

public sealed partial class Al1005FieldNameConflictWithPrimaryConstructorTests : AnalyzerTest<Al1005FieldNameConflictWithPrimaryConstructorAnalyzer> {
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
