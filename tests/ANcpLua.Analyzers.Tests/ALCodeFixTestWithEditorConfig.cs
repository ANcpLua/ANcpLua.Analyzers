using ANcpLua.Analyzers.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Enhanced code fix test base class that supports EditorConfig configuration.
///     This class allows tests to configure analyzer behavior through EditorConfig properties
///     (like build_property.TargetFramework, build_property.TargetFrameworks, etc.)
///     and custom properties (like ancplua_nullguard_style).
/// </summary>
public abstract class ALCodeFixTestWithEditorConfig<TAnalyzer, TCodeFix>
    where TAnalyzer : ALAnalyzer, new()
    where TCodeFix : CodeFixProvider, new()
{
    /// <summary>
    ///     Verifies a code fix with optional EditorConfig configuration.
    /// </summary>
    /// <param name="source">The source code before the fix.</param>
    /// <param name="fixedSource">The expected source code after the fix.</param>
    /// <param name="editorConfig">Optional EditorConfig properties to configure the analyzer.</param>
    /// <param name="includeThrowIfNullReference">Whether to include a reference to ArgumentNullException.ThrowIfNull (net6.0+).</param>
    protected static Task VerifyAsync(
        string source,
        string fixedSource,
        Dictionary<string, string>? editorConfig = null,
        bool includeThrowIfNullReference = false)
    {
        var test = new CustomCodeFixTest(editorConfig ?? new(), includeThrowIfNullReference)
        {
            TestCode = source.ReplaceLineEndings(),
            FixedCode = fixedSource.ReplaceLineEndings()
        };

        return test.RunAsync();
    }

    /// <summary>
    ///     Custom test class that configures EditorConfig options and compilation references.
    /// </summary>
    private sealed class CustomCodeFixTest : CSharpCodeFixTest<TAnalyzer, TCodeFix, DefaultVerifier>
    {
        private readonly Dictionary<string, string> _editorConfig;
        private readonly bool _includeThrowIfNullReference;

        public CustomCodeFixTest(Dictionary<string, string> editorConfig, bool includeThrowIfNullReference)
        {
            _editorConfig = editorConfig;
            _includeThrowIfNullReference = includeThrowIfNullReference;

            // Apply EditorConfig options to the test
            ApplyEditorConfig();
        }

        private void ApplyEditorConfig()
        {
            if (_editorConfig.Count == 0)
                return;

            // Build .editorconfig content from the dictionary
            var lines = new List<string> { "root = false" };
            foreach (var kvp in _editorConfig)
            {
                lines.Add($"{kvp.Key} = {kvp.Value}");
            }

            var editorConfigContent = string.Join("\n", lines);

            // Add the .editorconfig file to the test
            TestState.AdditionalFiles.Add(((".editorconfig", editorConfigContent)));
        }

    }
}
