using System.IO;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace ANcpLua.Analyzers;

/// <summary>
///     Help-link URL composition for diagnostic descriptors. Lives outside
///     <see cref="AlAnalyzer"/> so the docs generator can call it without
///     having to resolve <c>ANcpLua.Roslyn.Utilities</c> (the assembly that
///     defines <c>AlAnalyzer</c>'s base type). Single source of truth for
///     "what URL does AL00XX point at?" — descriptors call it, the docs
///     generator calls it, the <c>--check</c> mode verifies them against
///     each other.
/// </summary>
public static class RuleDocs
{
    /// <summary>
    ///     Base URL for diagnostic help links. Resolves to a per-rule page under
    ///     <c>docs/rules/AL00XX_&lt;SymbolicName&gt;.md</c>, emitted by
    ///     <c>tools/ANcpLua.Analyzers.DocsGenerator</c>.
    /// </summary>
    public const string HelpLinkBase =
        "https://github.com/ANcpLua/ANcpLua.Analyzers"
        + "/blob/main/docs/rules/";

    /// <summary>
    ///     Composes the full help-link URL for a diagnostic ID + symbolic name.
    ///     Symbolic name is the analyzer class-name suffix after stripping the
    ///     <c>(AL|Al)NNNN</c> prefix and <c>Analyzer</c> suffix.
    /// </summary>
    public static string HelpLink(string id, string symbolicName) =>
        HelpLinkBase + id + "_" + symbolicName + ".md";

    /// <summary>
    ///     Convenience overload for analyzers that build descriptors manually
    ///     (not via <c>AlAnalyzer.CreateRule</c>): derives the symbolic name from the
    ///     compiler-supplied caller file path. The 7 hand-built descriptors in
    ///     <c>Analyzers/AL16xx*</c> use this so they don't have to pass a literal
    ///     symbolic name that could drift from the class file name.
    /// </summary>
    public static string HelpLinkAuto(string id, [CallerFilePath] string callerFile = "") =>
        HelpLink(id, SymbolicNameFromFile(callerFile));

    /// <summary>
    ///     Derives the symbolic part of the per-rule docs filename from an analyzer's
    ///     source file path (supplied by the compiler via <c>CallerFilePath</c>).
    ///     Normalizes uppercase <c>AL{4-digit}</c> (file convention) to Pascal-case
    ///     <c>Al{4-digit}</c> (reflected class-name convention) so file-derived and
    ///     class-derived symbolic names always agree. Multi-id classes like
    ///     <c>AL1003ToAL1004SpanComparisonAnalyzer</c> carry the second ID in the
    ///     middle of the name; without normalization, the URL would say
    ///     <c>ToAL1004</c> but the generator (working off the reflected class
    ///     <c>Al1003ToAl1004SpanComparisonAnalyzer</c>) would expect <c>ToAl1004</c>.
    /// </summary>
    public static string SymbolicNameFromFile(string callerFilePath)
    {
        var name = Path.GetFileNameWithoutExtension(callerFilePath);
        name = Regex.Replace(name, @"AL(\d{4})", "Al$1");
        if (name.EndsWith("Analyzer", System.StringComparison.Ordinal))
            name = name.Substring(0, name.Length - "Analyzer".Length);
        var prefix = Regex.Match(name, "^Al\\d{4}");
        if (prefix.Success)
            name = name.Substring(prefix.Length);
        return name;
    }
}
