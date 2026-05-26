// Copyright (c) Alexander Nachtmann
// SPDX-License-Identifier: Apache-2.0

using ANcpLua.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ANcpLua.Analyzers.DocsGenerator;

/// <summary>
///   Reflects the analyzer assembly to enumerate everything the renderers + rewriters
///   need: the <see cref="DiagnosticDescriptor"/> catalog, the set of fixable IDs, and
///   the <c>Id → ClassName</c> map (covering multi-id analyzers by pointing every id
///   at the same class). The runtime descriptor remains the source of truth; this is
///   the only place that depends on <see cref="Activator.CreateInstance(Type)"/> over
///   analyzer types — keeping that fragility contained.
/// </summary>
internal static class DescriptorCatalog
{
    /// <summary>
    ///   Returns every distinct <see cref="DiagnosticDescriptor"/> reported by the analyzer
    ///   assembly, ordered by id for deterministic output.
    /// </summary>
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

    /// <summary>
    ///   Returns the set of <c>AL*</c> diagnostic ids any <see cref="CodeFixProvider"/>
    ///   advertises. Filters to the <c>AL</c> band so a code-fix that happens to advertise
    ///   a non-package id doesn't leak into per-rule "Code fix: Yes" labels.
    /// </summary>
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

    /// <summary>
    ///   Walks every concrete <see cref="DiagnosticAnalyzer"/> in the analyzer assembly
    ///   and builds <c>Id → ClassName</c>. Analyzers that register multiple ids point
    ///   all of those ids at the same class.
    /// </summary>
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

    /// <summary>
    ///   Variant used by <see cref="EnforceIdsRewriter"/>. Returns three views:
    ///   <list type="bullet">
    ///     <item><c>AnalyzerIds</c> — the smallest descriptor id (ordinal-sorted) per analyzer class.</item>
    ///     <item><c>CodeFixIds</c> — the smallest <c>FixableDiagnosticIds</c> entry per code-fix class.</item>
    ///     <item><c>AllAnalyzerIds</c> — the full set of ids per multi-id analyzer class so the source rewriter
    ///       can recognise existing valid id references (e.g., <c>AL1003ToAL1004</c> documenting both IDs).</item>
    ///   </list>
    /// </summary>
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
