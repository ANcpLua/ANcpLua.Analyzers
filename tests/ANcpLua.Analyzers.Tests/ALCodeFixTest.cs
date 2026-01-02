using Basic.Reference.Assemblies;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace ANcpLua.Analyzers.Tests;

public abstract class ALCodeFixTest<TAnalyzer, TCodeFix>
    where TAnalyzer : DiagnosticAnalyzer, new()
    where TCodeFix : CodeFixProvider, new() {
    private static readonly ReferenceAssemblies Net100Tfm = new("net10.0");

    protected static Task VerifyAsync(string source, string fixedSource) {
        var test = new CSharpCodeFixTest<TAnalyzer, TCodeFix, DefaultVerifier> {
            TestCode = source.ReplaceLineEndings(),
            FixedCode = fixedSource.ReplaceLineEndings(),
            ReferenceAssemblies = Net100Tfm
        };

        test.TestState.AdditionalReferences.AddRange(Net100.References.All);

        return test.RunAsync();
    }
}
