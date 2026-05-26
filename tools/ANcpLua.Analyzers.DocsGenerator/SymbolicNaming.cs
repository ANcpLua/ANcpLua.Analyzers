// Copyright (c) Alexander Nachtmann
// SPDX-License-Identifier: Apache-2.0

using System.Text.RegularExpressions;

namespace ANcpLua.Analyzers.DocsGenerator;

/// <summary>
///   Class-name ↔ symbolic-name ↔ on-disk-filename transforms. Kept in one place because
///   the per-rule docs filename (<c>docs/rules/{id}_{symbolic}.md</c>), the descriptor
///   <c>HelpLinkUri</c> anchor, and the GitHub-case-sensitive source filename all have
///   to agree byte-for-byte for <c>--check</c> drift detection to mean anything.
/// </summary>
internal static class SymbolicNaming
{
    private static readonly Regex EmbeddedUpperAl = new(@"AL(\d{4})", RegexOptions.Compiled);
    private static readonly Regex SymbolicPrefix = new(@"^Al\d{4}", RegexOptions.Compiled);
    private static readonly Regex PascalAl = new(@"Al(\d{4})", RegexOptions.Compiled);

    /// <summary>
    ///   Strips the <c>Analyzer</c> suffix and any <c>Al\d{4}</c> prefix off the
    ///   class name; the remainder is the symbolic part used in per-rule docs
    ///   filenames <c>docs/rules/{id}_{symbolic}.md</c> and in the help-link URL.
    ///   Normalizes embedded uppercase <c>AL\d{4}</c> to Pascal-case <c>Al\d{4}</c>
    ///   first so the output matches <c>RuleDocs.SymbolicNameFromFile</c> on multi-id
    ///   classes (e.g., <c>AL1003ToAL1004SpanComparison</c> in a file basename vs
    ///   <c>Al1003ToAl1004SpanComparison</c> in a reflected class name).
    /// </summary>
    public static string ToSymbolicName(string className)
    {
        var name = EmbeddedUpperAl.Replace(className, "Al$1");
        if (name.EndsWith("Analyzer", StringComparison.Ordinal))
            name = name[..^"Analyzer".Length];
        var prefix = SymbolicPrefix.Match(name);
        if (prefix.Success)
            name = name[prefix.Length..];
        return name;
    }

    /// <summary>
    ///   Maps a Pascal-case class name (e.g., <c>Al1003ToAl1004SpanComparisonAnalyzer</c>)
    ///   to its on-disk source filename (<c>AL1003ToAL1004SpanComparisonAnalyzer.cs</c>).
    ///   git tracks files with uppercase <c>AL</c> prefix; macOS's case-insensitive
    ///   default masks this locally but GitHub's case-sensitive URL space does not.
    ///   Replaces every <c>Al{4-digit}</c> occurrence with uppercase <c>AL{4-digit}</c>
    ///   so multi-id classes (which carry two IDs in the name) lift both prefixes.
    /// </summary>
    public static string FileBasenameForClass(string className) =>
        PascalAl.Replace(className, "AL$1");
}
