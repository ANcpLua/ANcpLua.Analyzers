using Basic.Reference.Assemblies;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Enhanced code fix test base class that supports EditorConfig configuration.
/// </summary>
public abstract class ALCodeFixTestWithEditorConfig<TAnalyzer, TCodeFix>
    where TAnalyzer : DiagnosticAnalyzer, new()
    where TCodeFix : CodeFixProvider, new() {
    /// <summary>
    ///     Source code that provides Microsoft.Shared.Diagnostics.Throw for test compilations.
    /// </summary>
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

    
    private static readonly ReferenceAssemblies EmptyNet100 = new("net10.0");
    private static readonly ReferenceAssemblies EmptyNetStandard20 = new("netstandard2.0");

    /// <summary>
    ///     Verifies analyzer and code fix behavior.
    /// </summary>
    /// <param name="source">Source code with diagnostic markers.</param>
    /// <param name="fixedSource">Expected source after fix is applied.</param>
    /// <param name="editorConfig">Optional EditorConfig settings.</param>
    /// <param name="includeThrowHelper">Include Microsoft.Shared.Diagnostics.Throw polyfill.</param>
    /// <param name="useNet10References">
    ///     If true, uses .NET 10 references (includes ThrowIfNull).
    ///     If false, uses NetStandard 2.0 references (no ThrowIfNull).
    /// </param>
    protected static Task VerifyAsync(
        string source,
        string fixedSource,
        Dictionary<string, string>? editorConfig = null,
        bool includeThrowHelper = false,
        bool useNet10References = true) {
        var test = new CustomCodeFixTest(
            editorConfig ?? new Dictionary<string, string>(),
            includeThrowHelper,
            useNet10References) {
            TestCode = source.ReplaceLineEndings(), FixedCode = fixedSource.ReplaceLineEndings()
        };
        return test.RunAsync();
    }

    private sealed class CustomCodeFixTest : CSharpCodeFixTest<TAnalyzer, TCodeFix, DefaultVerifier> {
        private readonly Dictionary<string, string> _editorConfig;
        private readonly bool _includeThrowHelper;
        private readonly bool _useNet10References;

        public CustomCodeFixTest(
            Dictionary<string, string> editorConfig,
            bool includeThrowHelper,
            bool useNet10References) {
            _editorConfig = editorConfig;
            _includeThrowHelper = includeThrowHelper;
            _useNet10References = useNet10References;

            ConfigureReferences();
            ApplyEditorConfig();
            ApplyThrowHelper();
        }

        private void ConfigureReferences() {
            if (_useNet10References) {
                ReferenceAssemblies = EmptyNet100;
                TestState.AdditionalReferences.AddRange(Net100.References.All);
            } else {
                ReferenceAssemblies = EmptyNetStandard20;
                TestState.AdditionalReferences.AddRange(NetStandard20.References.All);
            }
        }

        private void ApplyEditorConfig() {
            if (_editorConfig.Count == 0) {
                return;
            }

            var globalLines = new List<string> { "is_global = true", "" };
            foreach (var kvp in _editorConfig) {
                globalLines.Add($"{kvp.Key} = {kvp.Value}");
            }

            TestState.AnalyzerConfigFiles.Add(("/.globalconfig", string.Join("\n", globalLines)));

            var editorConfigLines = new List<string> { "root = true", "", "[*.cs]" };
            foreach (var kvp in _editorConfig) {
                var value = kvp.Value.Contains(';') ? "\"" + kvp.Value + "\"" : kvp.Value;
                editorConfigLines.Add($"{kvp.Key} = {value}");
            }

            TestState.AnalyzerConfigFiles.Add(("/0/.editorconfig", string.Join("\n", editorConfigLines)));
        }

        private void ApplyThrowHelper() {
            if (!_includeThrowHelper) {
                return;
            }

            TestState.Sources.Add(("ThrowHelper.cs", ThrowHelperPolyfill));
            FixedState.Sources.Add(("ThrowHelper.cs", ThrowHelperPolyfill));
        }
    }
}
