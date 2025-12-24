using ANcpLua.Analyzers.Analyzers;

namespace ANcpLua.Analyzers.Tests;

public sealed class AL0006AnalyzerTests : ALAnalyzerTest<AL0006FieldNameConflictWithPrimaryConstructorAnalyzer>
{
    [Fact]
    public Task ShouldReportDiagnostic() => VerifyAsync("""
        public class TestClass(int value)
        {
            private int [|value|];
        }
        """);

    [Fact]
    public Task ShouldNotReportWhenNoConflict() => VerifyAsync("""
        public class TestClass(int value)
        {
            private int _value;
        }
        """);
}
