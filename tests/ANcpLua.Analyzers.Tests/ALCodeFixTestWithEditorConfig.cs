using ANcpLua.Analyzers.Core;
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
    where TCodeFix : CodeFixProvider, new() {
    /// <summary>
    ///     Source code that provides ArgumentNullException with ThrowIfNull for test compilations.
    ///     This defines the full type to ensure constructors are available.
    /// </summary>
    private const string ThrowIfNullPolyfill = """
                                               namespace System
                                               {
                                                   public class ArgumentNullException : ArgumentException
                                                   {
                                                       public ArgumentNullException() : base("Value cannot be null.") { }
                                                       public ArgumentNullException(string? paramName) : base("Value cannot be null.", paramName) { }
                                                       public ArgumentNullException(string? paramName, string? message) : base(message, paramName) { }
                                                       public ArgumentNullException(string? message, Exception? innerException) : base(message, innerException) { }

                                                       public static void ThrowIfNull(object? argument, string? paramName = null)
                                                       {
                                                           if (argument is null)
                                                               throw new ArgumentNullException(paramName);
                                                       }
                                                   }
                                               }
                                               """;

    /// <summary>
    ///     Verifies a code fix with optional EditorConfig configuration.
    /// </summary>
    /// <param name="source">The source code before the fix.</param>
    /// <param name="fixedSource">The expected source code after the fix.</param>
    /// <param name="editorConfig">Optional EditorConfig properties to configure the analyzer.</param>
    /// <param name="includeThrowIfNullReference">
    ///     Whether to include a reference to ArgumentNullException.ThrowIfNull
    ///     (net6.0+).
    /// </param>
    protected static Task VerifyAsync(
        string source,
        string fixedSource,
        Dictionary<string, string>? editorConfig = null,
        bool includeThrowIfNullReference = false) {
        var test =
            new CustomCodeFixTest(editorConfig ?? new Dictionary<string, string>(), includeThrowIfNullReference) {
                TestCode = source.ReplaceLineEndings(), FixedCode = fixedSource.ReplaceLineEndings()
            };

        return test.RunAsync();
    }

    /// <summary>
    ///     Custom test class that configures EditorConfig options and compilation references.
    /// </summary>
    private sealed class CustomCodeFixTest : CSharpCodeFixTest<TAnalyzer, TCodeFix, DefaultVerifier> {
        private readonly Dictionary<string, string> _editorConfig;
        private readonly bool _includeThrowIfNullReference;

        public CustomCodeFixTest(Dictionary<string, string> editorConfig, bool includeThrowIfNullReference) {
            _editorConfig = editorConfig;
            _includeThrowIfNullReference = includeThrowIfNullReference;

            // Apply configurations
            ApplyEditorConfig();
            ApplyThrowIfNullReference();
        }

        private void ApplyEditorConfig() {
            if (_editorConfig.Count == 0) {
                return;
            }

            // Build global analyzer config content
            // Global configs use "is_global = true" and flat key=value pairs
            // Note: Values containing semicolons need no escaping in global configs
            var globalLines = new List<string> { "is_global = true", "" };
            foreach (var kvp in _editorConfig) {
                globalLines.Add($"{kvp.Key} = {kvp.Value}");
            }

            var globalConfigContent = string.Join("\n", globalLines);

            // Add as global config at root level
            TestState.AnalyzerConfigFiles.Add(("/.globalconfig", globalConfigContent));

            // Also add as regular editorconfig with [*.cs] pattern for per-file options
            // The path /0/.editorconfig places it in the same directory as test files (/0/Test1.cs)
            // Note: In editorconfig, semicolons start comments, so we need to quote values containing them
            var editorConfigLines = new List<string> { "root = true", "", "[*.cs]" };
            foreach (var kvp in _editorConfig) {
                // Quote values containing semicolons or other special characters
                var value = kvp.Value.Contains(';') ? $"\"{kvp.Value}\"" : kvp.Value;
                editorConfigLines.Add($"{kvp.Key} = {value}");
            }

            var editorConfigContent = string.Join("\n", editorConfigLines);
            TestState.AnalyzerConfigFiles.Add(("/0/.editorconfig", editorConfigContent));
        }

        private void ApplyThrowIfNullReference() {
            if (!_includeThrowIfNullReference) {
                return;
            }

            // Add source code that provides ThrowIfNull method to both test and fixed states
            TestState.Sources.Add(("ThrowIfNullPolyfill.cs", ThrowIfNullPolyfill));
            FixedState.Sources.Add(("ThrowIfNullPolyfill.cs", ThrowIfNullPolyfill));
        }
    }
}
