using Basic.Reference.Assemblies;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace ANcpLua.Analyzers.Tests;

public abstract class ALAnalyzerTest<TAnalyzer> where TAnalyzer : DiagnosticAnalyzer, new() {
    private static readonly ReferenceAssemblies Net100Tfm = new("net10.0");
    private static readonly ReferenceAssemblies NetStandard20Tfm = new("netstandard2.0");

    protected static Task VerifyAsync(string source, bool useNet10References = true) {
        var test = new CSharpAnalyzerTest<TAnalyzer, DefaultVerifier> {
            TestCode = source.ReplaceLineEndings(),
            ReferenceAssemblies = useNet10References ? Net100Tfm : NetStandard20Tfm
        };

        test.TestState.AdditionalReferences.AddRange(
            useNet10References ? Net100.References.All : NetStandard20.References.All);

        return test.RunAsync();
    }
}
