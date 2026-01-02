using Basic.Reference.Assemblies;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace ANcpLua.Analyzers.Tests;

public abstract class ALCodeFixTestWithEditorConfig<TAnalyzer, TCodeFix>
    where TAnalyzer : DiagnosticAnalyzer, new()
    where TCodeFix : CodeFixProvider, new() {
    private const string ThrowHelperPolyfill = """
                                               namespace Microsoft.Shared.Diagnostics
                                               {
                                                   internal static class Throw
                                                   {
                                                       public static T IfNull<T>(T argument, string? paramName = null) where T : class
                                                       {
                                                           if (argument is null)
                                                               throw new System.ArgumentNullException(paramName);
                                                           return argument;
                                                       }
                                                   }
                                               }
                                               """;

    private static readonly ReferenceAssemblies Net100Tfm = new("net10.0");
    private static readonly ReferenceAssemblies NetStandard20Tfm = new("netstandard2.0");

    protected static Task VerifyAsync(
        string source,
        string fixedSource,
        Dictionary<string, string>? editorConfig = null,
        bool includeThrowHelper = false,
        bool useNet10References = true) {
        var test = new CustomCodeFixTest(
            editorConfig ?? [],
            includeThrowHelper,
            useNet10References) {
            TestCode = source.ReplaceLineEndings(), FixedCode = fixedSource.ReplaceLineEndings()
        };

        return test.RunAsync();
    }

    private sealed class CustomCodeFixTest : CSharpCodeFixTest<TAnalyzer, TCodeFix, DefaultVerifier> {
        public CustomCodeFixTest(
            Dictionary<string, string> editorConfig,
            bool includeThrowHelper,
            bool useNet10References) {
            ReferenceAssemblies = useNet10References ? Net100Tfm : NetStandard20Tfm;
            TestState.AdditionalReferences.AddRange(
                useNet10References ? Net100.References.All : NetStandard20.References.All);

            ApplyEditorConfig(editorConfig);
            ApplyThrowHelper(includeThrowHelper);
        }

        private void ApplyEditorConfig(Dictionary<string, string> editorConfig) {
            if (editorConfig.Count == 0) {
                return;
            }

            var globalLines = new List<string> { "is_global = true", "" };
            foreach (var kvp in editorConfig) {
                globalLines.Add($"{kvp.Key} = {kvp.Value}");
            }

            TestState.AnalyzerConfigFiles.Add(("/.globalconfig", string.Join("\n", globalLines)));

            var editorConfigLines = new List<string> { "root = true", "", "[*.cs]" };
            foreach (var kvp in editorConfig) {
                var value = kvp.Value.Contains(';') ? $"\"{kvp.Value}\"" : kvp.Value;
                editorConfigLines.Add($"{kvp.Key} = {value}");
            }

            TestState.AnalyzerConfigFiles.Add(("/0/.editorconfig", string.Join("\n", editorConfigLines)));
        }

        private void ApplyThrowHelper(bool includeThrowHelper) {
            if (!includeThrowHelper) {
                return;
            }

            TestState.Sources.Add(("ThrowHelper.cs", ThrowHelperPolyfill));
            FixedState.Sources.Add(("ThrowHelper.cs", ThrowHelperPolyfill));
        }
    }
}
