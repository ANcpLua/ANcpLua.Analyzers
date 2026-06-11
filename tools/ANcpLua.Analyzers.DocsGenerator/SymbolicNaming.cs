// Copyright (c) Alexander Nachtmann
// SPDX-License-Identifier: Apache-2.0

using System.Text.RegularExpressions;

namespace ANcpLua.Analyzers.DocsGenerator;

// Class-name <-> symbolic-name <-> on-disk-filename transforms, kept in one place because the
// per-rule docs filename (docs/rules/{id}_{symbolic}.md), the descriptor HelpLinkUri anchor, and
// the GitHub-case-sensitive source filename must agree byte-for-byte or --check means nothing.
internal static class SymbolicNaming
{
    private static readonly Regex EmbeddedUpperAl = new(@"AL(\d{4})", RegexOptions.Compiled);
    private static readonly Regex SymbolicPrefix = new(@"^Al\d{4}", RegexOptions.Compiled);
    private static readonly Regex PascalAl = new(@"Al(\d{4})", RegexOptions.Compiled);

    // Normalizes embedded uppercase AL\d{4} to Pascal-case Al\d{4} first so the result matches
    // RuleDocs.SymbolicNameFromFile on multi-id classes (file basename AL1003ToAL1004SpanComparison
    // vs reflected class name Al1003ToAl1004SpanComparison).
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

    // Pascal-case class name -> on-disk source filename (uppercases every Al{4-digit} to AL{4-digit},
    // so multi-id classes lift both prefixes). git tracks the uppercase AL prefix; macOS's
    // case-insensitive default masks this locally but GitHub's case-sensitive URL space does not.
    public static string FileBasenameForClass(string className) =>
        PascalAl.Replace(className, "AL$1");
}
