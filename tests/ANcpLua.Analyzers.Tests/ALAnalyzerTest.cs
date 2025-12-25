using ANcpLua.Analyzers.Core;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace ANcpLua.Analyzers.Tests;

public abstract class ALAnalyzerTest<TAnalyzer> where TAnalyzer : ALAnalyzer, new()
{
    protected static Task VerifyAsync(string source)
    {
        return CSharpAnalyzerVerifier<TAnalyzer, DefaultVerifier>.VerifyAnalyzerAsync(source.ReplaceLineEndings());
    }
}
