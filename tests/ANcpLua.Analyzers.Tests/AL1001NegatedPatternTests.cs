using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Analyzers.CodeFixes.CodeFixes;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

public sealed partial class Al1001NegatedPatternTests : AnalyzerTest<Al1001DontRepeatNegatedPatternAnalyzer> {
    [Theory]
    [InlineData("[|not not|] null")]
    [InlineData("[|not not not|] null")]
    public Task ShouldReportDiagnostic(string pattern) =>
        VerifyAsync($"public class C {{ void M(object? o) {{ _ = o is {pattern}; }} }}");
}

public sealed partial class Al1001NegatedPatternCodeFixTests : CodeFixTest<Al1001DontRepeatNegatedPatternAnalyzer, Al1001SimplifyNegatedPatternCodeFixProvider> {
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
    public Task ShouldFix(string source, string fixedSource) => VerifyAsync(source, fixedSource);
}
