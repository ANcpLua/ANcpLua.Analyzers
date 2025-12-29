using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ANcpLua.Analyzers.Internal;

internal static class RoslynExtensions {
    /// <summary>
    /// Gets the value for the specified key, or null if not found.
    /// </summary>
    public static string? GetValueOrNull(this AnalyzerConfigOptions options, string key) =>
        options.TryGetValue(key, out var value) ? value : null;

    /// <summary>
    /// Checks if the compilation's language version is at least the specified version.
    /// </summary>
    public static bool HasLanguageVersionAtLeastEqualTo(this Compilation compilation, LanguageVersion version) =>
        compilation is CSharpCompilation csharp && csharp.LanguageVersion >= version;
}
