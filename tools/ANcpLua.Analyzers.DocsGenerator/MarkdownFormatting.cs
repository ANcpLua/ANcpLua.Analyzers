// Copyright (c) Alexander Nachtmann
// SPDX-License-Identifier: Apache-2.0

namespace ANcpLua.Analyzers.DocsGenerator;

// Shared cell-escaping so every renderer escapes identically. Descriptor titles/descriptions
// occasionally contain pipes or newlines, and inconsistent escaping has bitten --check drift before.
internal static class MarkdownFormatting
{
    public static string Escape(string value) =>
        value.Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal)
            .Replace("|", "\\|", StringComparison.Ordinal);
}
