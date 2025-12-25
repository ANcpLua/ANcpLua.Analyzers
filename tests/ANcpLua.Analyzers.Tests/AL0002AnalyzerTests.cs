using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Analyzers.CodeFixes.CodeFixes;

namespace ANcpLua.Analyzers.Tests;

public sealed class AL0002AnalyzerTests : ALAnalyzerTest<AL0002DontRepeatNegatedPatternAnalyzer>
{
    [Theory]
    [InlineData("""
                public class TestClass
                {
                    public void TestMethod(object? obj)
                    {
                        _ = obj is [|not not|] null;
                    }
                }
                """)]
    [InlineData("""
                public class TestClass
                {
                    public void TestMethod(object? obj)
                    {
                        _ = obj is [|not not not|] null;
                    }
                }
                """)]
    public Task ShouldReportDiagnostic(string source)
    {
        return VerifyAsync(source);
    }
}

public sealed class AL0002CodeFixTests : ALCodeFixTest<AL0002DontRepeatNegatedPatternAnalyzer, AL0002CodeFixProvider>
{
    [Theory]
    [InlineData("""
                public class TestClass
                {
                    public void TestMethod(object? obj)
                    {
                        _ = obj is [|not not|] null;
                    }
                }
                """,
        """
        public class TestClass
        {
            public void TestMethod(object? obj)
            {
                _ = obj is null;
            }
        }
        """)]
    [InlineData("""
                public class TestClass
                {
                    public void TestMethod(object? obj)
                    {
                        _ = obj is [|not not not|] null;
                    }
                }
                """,
        """
        public class TestClass
        {
            public void TestMethod(object? obj)
            {
                _ = obj is not null;
            }
        }
        """)]
    public Task ShouldFix(string source, string fixedSource)
    {
        return VerifyAsync(source, fixedSource);
    }
}