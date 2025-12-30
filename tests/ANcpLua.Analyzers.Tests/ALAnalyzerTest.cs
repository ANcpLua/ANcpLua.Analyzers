using Basic.Reference.Assemblies;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace ANcpLua.Analyzers.Tests;

public abstract class ALAnalyzerTest<TAnalyzer> where TAnalyzer : DiagnosticAnalyzer, new() {
    
    private static readonly ReferenceAssemblies EmptyNet100 = new("net10.0");
    private static readonly ReferenceAssemblies EmptyNetStandard20 = new("netstandard2.0");

    protected static Task VerifyAsync(string source, bool useNet10References = true) {
        var test = new CSharpAnalyzerTest<TAnalyzer, DefaultVerifier> { TestCode = source.ReplaceLineEndings() };

        if (useNet10References) {
            test.ReferenceAssemblies = EmptyNet100;
            test.TestState.AdditionalReferences.AddRange(Net100.References.All);
        } else {
            test.ReferenceAssemblies = EmptyNetStandard20;
            test.TestState.AdditionalReferences.AddRange(NetStandard20.References.All);
        }

        return test.RunAsync();
    }
}
