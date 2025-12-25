using ANcpLua.Analyzers.Core;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace ANcpLua.Analyzers.Tests;

public abstract class ALCodeFixTest<TAnalyzer, TCodeFix>
    where TAnalyzer : ALAnalyzer, new()
    where TCodeFix : CodeFixProvider, new()
{
    protected static Task VerifyAsync(string source, string fixedSource)
    {
        return CSharpCodeFixVerifier<TAnalyzer, TCodeFix, DefaultVerifier>
            .VerifyCodeFixAsync(source.ReplaceLineEndings(), fixedSource.ReplaceLineEndings());
    }
}