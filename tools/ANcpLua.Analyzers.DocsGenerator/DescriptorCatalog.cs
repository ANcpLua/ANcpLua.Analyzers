// Copyright (c) Alexander Nachtmann
// SPDX-License-Identifier: Apache-2.0

using ANcpLua.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ANcpLua.Analyzers.DocsGenerator;

// Runtime descriptors are the source of truth. This is the only place that reflects over
// analyzer types via Activator.CreateInstance, deliberately containing that fragility.
internal static class DescriptorCatalog
{
    public static IReadOnlyList<DiagnosticDescriptor> GetDescriptors()
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var descriptors = new List<DiagnosticDescriptor>();

        foreach (var type in typeof(AlAnalyzer).Assembly.GetTypes())
        {
            if (type.IsAbstract || !typeof(DiagnosticAnalyzer).IsAssignableFrom(type))
                continue;

            DiagnosticAnalyzer? analyzer;
            try { analyzer = (DiagnosticAnalyzer?)Activator.CreateInstance(type); }
            catch { continue; }
            if (analyzer is null) continue;

            foreach (var d in analyzer.SupportedDiagnostics)
                if (seen.Add(d.Id))
                    descriptors.Add(d);
        }

        return descriptors.OrderBy(d => d.Id, StringComparer.Ordinal).ToArray();
    }

    // Filtered to the AL band so a code-fix advertising a non-package id can't leak into
    // per-rule "Code fix: Yes" labels.
    public static HashSet<string> GetFixableDiagnosticIds()
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var type in typeof(AlAnalyzer).Assembly.GetTypes())
        {
            if (type.IsAbstract || !typeof(CodeFixProvider).IsAssignableFrom(type))
                continue;

            CodeFixProvider? provider;
            try { provider = (CodeFixProvider?)Activator.CreateInstance(type); }
            catch { continue; }
            if (provider is null) continue;

            foreach (var id in provider.FixableDiagnosticIds)
                if (id.StartsWith("AL", StringComparison.Ordinal))
                    ids.Add(id);
        }
        return ids;
    }

    public static Dictionary<string, string> BuildIdToClassMap()
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var type in typeof(AlAnalyzer).Assembly.GetTypes())
        {
            if (type.IsAbstract) continue;
            if (!typeof(DiagnosticAnalyzer).IsAssignableFrom(type)) continue;
            try
            {
                if (Activator.CreateInstance(type) is DiagnosticAnalyzer a)
                {
                    foreach (var d in a.SupportedDiagnostics)
                        map[d.Id] = type.Name;
                }
            }
            catch { /* analyzers with non-default ctors are skipped */ }
        }
        return map;
    }

    // Three views for EnforceIdsRewriter:
    //   AnalyzerIds    -> smallest ordinal-sorted descriptor id per analyzer class.
    //   CodeFixIds     -> smallest FixableDiagnosticIds entry per code-fix class.
    //   AllAnalyzerIds -> full id set per multi-id analyzer class, so the rewriter recognises
    //                     valid existing references (e.g. AL1003ToAL1004 documenting both IDs).
    public static (
        Dictionary<string, string> AnalyzerIds,
        Dictionary<string, string> CodeFixIds,
        Dictionary<string, HashSet<string>> AllAnalyzerIds) BuildClassMaps()
    {
        var analyzers = new Dictionary<string, string>(StringComparer.Ordinal);
        var codeFixes = new Dictionary<string, string>(StringComparer.Ordinal);
        var allAnalyzer = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

        foreach (var type in typeof(AlAnalyzer).Assembly.GetTypes())
        {
            if (type.IsAbstract) continue;

            if (typeof(DiagnosticAnalyzer).IsAssignableFrom(type))
            {
                try
                {
                    if (Activator.CreateInstance(type) is DiagnosticAnalyzer a && a.SupportedDiagnostics.Length > 0)
                    {
                        var sorted = a.SupportedDiagnostics
                            .OrderBy(d => d.Id, StringComparer.Ordinal).ToArray();
                        analyzers[type.Name] = sorted[0].Id;
                        allAnalyzer[type.Name] = new HashSet<string>(
                            sorted.Select(d => d.Id), StringComparer.Ordinal);
                    }
                }
                catch { }
            }
            else if (typeof(CodeFixProvider).IsAssignableFrom(type))
            {
                try
                {
                    if (Activator.CreateInstance(type) is CodeFixProvider p && p.FixableDiagnosticIds.Length > 0)
                        codeFixes[type.Name] = p.FixableDiagnosticIds
                            .OrderBy(s => s, StringComparer.Ordinal).First();
                }
                catch { }
            }
        }
        return (analyzers, codeFixes, allAnalyzer);
    }
}
