using ANcpLua.Analyzers.Analyzers;
using System.Threading.Tasks;
using Xunit;

namespace ANcpLua.Analyzers.Tests;

public sealed class AL0001AnalyzerTests : ALAnalyzerTest<AL0001ProhibitPrimaryConstructorParameterReassignmentAnalyzer>
{
    [Theory]
    [InlineData("""
                public class TestClass(int i)
                {
                    public void TestMethod()
                    {
                        [|i|] = 10;
                    }
                }
                """)]
    [InlineData("""
                public class TestClass(int i)
                {
                    public void TestMethod()
                    {
                        [|i|] += 10;
                    }
                }
                """)]
    [InlineData("""
                public class TestClass(int i)
                {
                    public void TestMethod()
                    {
                        [|i|]++;
                    }
                }
                """)]
    [InlineData("""
                public class TestClass(string? s)
                {
                    public void TestMethod()
                    {
                        [|s|] ??= string.Empty;
                    }
                }
                """)]
    public Task ShouldReportDiagnostic(string source)
    {
        return VerifyAsync(source);
    }
}