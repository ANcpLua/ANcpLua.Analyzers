using ANcpLua.Analyzers.Analyzers;
using System.Threading.Tasks;
using Xunit;

namespace ANcpLua.Analyzers.Tests;

public sealed class AL0003AnalyzerTests : ALAnalyzerTest<AL0003DontDivideByConstantZeroAnalyzer>
{
    [Theory]
    [InlineData("""
                public class TestClass
                {
                    public void TestMethod(int i)
                    {
                        _ = [|i / 0|];
                    }
                }
                """)]
    [InlineData("""
                public class TestClass
                {
                    public void TestMethod(int i)
                    {
                        _ = [|i % 0|];
                    }
                }
                """)]
    [InlineData("""
                public class TestClass
                {
                    public void TestMethod(long i)
                    {
                        _ = [|i / 0L|];
                    }
                }
                """)]
    [InlineData("""
                public class TestClass
                {
                    public void TestMethod(decimal d)
                    {
                        _ = [|d / 0.0m|];
                    }
                }
                """)]
    public Task ShouldReportDiagnostic(string source)
    {
        return VerifyAsync(source);
    }
}