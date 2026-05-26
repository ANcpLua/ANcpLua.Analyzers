// Copyright (c) Alexander Nachtmann
// SPDX-License-Identifier: Apache-2.0

namespace ANcpLua.Analyzers.DocsGenerator;

/// <summary>
///   Top-level orchestrator. Owns the mode dispatch (<see cref="CliModes.Parse"/>),
///   the four generated-artifact pipelines (<c>Generate</c> + <c>Check</c>), and the
///   source-side <c>EnforceIds</c> rewriter. Every other class in this project is
///   pure logic invoked from here.
///
///   Extension point: each generated artifact (index, per-rule pages, SARIF,
///   editorconfig) is one numbered step in <see cref="Generate"/> + <see cref="Check"/>.
///   Adding a new artifact means adding a focused renderer class and one numbered step
///   in both methods.
/// </summary>
internal static class DocsGenerator
{
    public static int Run(string[] args)
    {
        var repoRoot = RepoLayout.FindRepoRoot(AppContext.BaseDirectory);
        var outputPath = RepoLayout.IndexPath(repoRoot);
        var mode = CliModes.Parse(args);

        if (mode is Mode.EnforceIdsCheck or Mode.EnforceIdsApply)
            return EnforceIdsRewriter.Run(repoRoot, apply: mode == Mode.EnforceIdsApply);

        var descriptors = DescriptorCatalog.GetDescriptors();
        var fixableIds = DescriptorCatalog.GetFixableDiagnosticIds();

        return mode switch
        {
            Mode.Audit => Audit(descriptors, fixableIds),
            Mode.Check => Check(descriptors, fixableIds, outputPath, repoRoot),
            _ => Generate(descriptors, fixableIds, outputPath, repoRoot),
        };
    }

    private static int Audit(IReadOnlyList<Microsoft.CodeAnalysis.DiagnosticDescriptor> descriptors, HashSet<string> fixableIds)
    {
        Console.WriteLine($"{RepoLayout.PackageName} catalog audit");
        Console.WriteLine($"  Total descriptors: {descriptors.Count}");
        Console.WriteLine($"  With code fix:     {descriptors.Count(d => fixableIds.Contains(d.Id))}");
        foreach (var g in descriptors.GroupBy(d => d.DefaultSeverity).OrderByDescending(g => g.Key))
            Console.WriteLine($"  Severity {g.Key,-10}  {g.Count()}");
        return 0;
    }

    private static int Check(
        IReadOnlyList<Microsoft.CodeAnalysis.DiagnosticDescriptor> descriptors,
        HashSet<string> fixableIds,
        string outputPath,
        string repoRoot)
    {
        var idToClass = DescriptorCatalog.BuildIdToClassMap();

        // (1) Slim index file.
        if (!File.Exists(outputPath))
        {
            Console.Error.WriteLine($"Missing generated docs: {outputPath}");
            return 1;
        }
        if (!string.Equals(File.ReadAllText(outputPath), IndexDocsRenderer.Render(descriptors, fixableIds, idToClass), StringComparison.Ordinal))
        {
            Console.Error.WriteLine($"Index docs are stale: {Path.GetRelativePath(repoRoot, outputPath)}");
            return 1;
        }

        // (2) Per-rule pages under docs/rules/.
        var rulesDir = RepoLayout.RulesDir(repoRoot);
        var expectedRuleFiles = new HashSet<string>(StringComparer.Ordinal);
        foreach (var d in descriptors)
        {
            if (!idToClass.TryGetValue(d.Id, out var className))
            {
                Console.Error.WriteLine($"Descriptor {d.Id} has no owning DiagnosticAnalyzer class — cannot place per-rule page.");
                return 1;
            }
            var symbolic = SymbolicNaming.ToSymbolicName(className);
            var rulePath = RepoLayout.RulePath(repoRoot, d.Id, symbolic);
            expectedRuleFiles.Add(Path.GetFileName(rulePath));

            if (!File.Exists(rulePath))
            {
                Console.Error.WriteLine($"Missing per-rule page: {Path.GetRelativePath(repoRoot, rulePath)}");
                return 1;
            }
            if (!string.Equals(File.ReadAllText(rulePath), RulePageRenderer.Render(d, className, fixableIds), StringComparison.Ordinal))
            {
                Console.Error.WriteLine($"Per-rule page is stale: {Path.GetRelativePath(repoRoot, rulePath)}");
                return 1;
            }

            // HelpLinkUri drift: descriptor's URL must equal what the generator would emit now.
            var expectedUri = RuleDocs.HelpLink(d.Id, symbolic);
            if (!string.Equals(d.HelpLinkUri, expectedUri, StringComparison.Ordinal))
            {
                Console.Error.WriteLine($"HelpLinkUri drift on {d.Id}: descriptor='{d.HelpLinkUri}' expected='{expectedUri}'");
                return 1;
            }
        }

        // Fail on stale files left over in docs/rules/ that no descriptor produces.
        if (Directory.Exists(rulesDir))
        {
            foreach (var file in Directory.EnumerateFiles(rulesDir, "*.md"))
            {
                if (!expectedRuleFiles.Contains(Path.GetFileName(file)))
                {
                    Console.Error.WriteLine($"Stale per-rule page (no matching descriptor): {Path.GetRelativePath(repoRoot, file)}");
                    return 1;
                }
            }
        }

        // (3) Editorconfig profiles.
        foreach (var (path, expected) in EditorconfigRenderer.EnumerateProfiles(repoRoot, descriptors))
        {
            if (!File.Exists(path))
            {
                Console.Error.WriteLine($"Missing editorconfig profile: {Path.GetRelativePath(repoRoot, path)}");
                return 1;
            }
            if (!string.Equals(File.ReadAllText(path), expected, StringComparison.Ordinal))
            {
                Console.Error.WriteLine($"Editorconfig profile is stale: {Path.GetRelativePath(repoRoot, path)}");
                return 1;
            }
        }

        // (4) SARIF v2.1.0 rule manifest for tool interop (Sonar bridges, GitHub
        // Advanced Security uploads, IDE rule catalogs).
        var sarifPath = RepoLayout.SarifPath(repoRoot);
        if (!File.Exists(sarifPath))
        {
            Console.Error.WriteLine($"Missing SARIF manifest: {Path.GetRelativePath(repoRoot, sarifPath)}");
            return 1;
        }
        if (!string.Equals(File.ReadAllText(sarifPath), SarifRenderer.Render(descriptors, idToClass), StringComparison.Ordinal))
        {
            Console.Error.WriteLine($"SARIF manifest is stale: {Path.GetRelativePath(repoRoot, sarifPath)}");
            return 1;
        }

        Console.WriteLine($"Index docs are up to date: {Path.GetRelativePath(repoRoot, outputPath)}");
        Console.WriteLine($"Per-rule pages are up to date ({descriptors.Count}).");
        Console.WriteLine("Editorconfig profiles are up to date.");
        Console.WriteLine($"SARIF manifest is up to date: {Path.GetRelativePath(repoRoot, sarifPath)}");
        Console.WriteLine("HelpLinkUri values match per-rule page URLs.");
        return 0;
    }

    private static int Generate(
        IReadOnlyList<Microsoft.CodeAnalysis.DiagnosticDescriptor> descriptors,
        HashSet<string> fixableIds,
        string outputPath,
        string repoRoot)
    {
        var idToClass = DescriptorCatalog.BuildIdToClassMap();

        // (1) Slim index.
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        File.WriteAllText(outputPath, IndexDocsRenderer.Render(descriptors, fixableIds, idToClass));
        Console.WriteLine($"Wrote {Path.GetRelativePath(repoRoot, outputPath)}");

        // (2) Per-rule pages.
        var rulesDir = RepoLayout.RulesDir(repoRoot);
        Directory.CreateDirectory(rulesDir);
        var expectedRuleFiles = new HashSet<string>(StringComparer.Ordinal);
        foreach (var d in descriptors)
        {
            if (!idToClass.TryGetValue(d.Id, out var className)) continue;
            var symbolic = SymbolicNaming.ToSymbolicName(className);
            var rulePath = RepoLayout.RulePath(repoRoot, d.Id, symbolic);
            expectedRuleFiles.Add(Path.GetFileName(rulePath));
            File.WriteAllText(rulePath, RulePageRenderer.Render(d, className, fixableIds));
        }
        // Clean up stale rule pages from prior renames (otherwise --check fails afterward).
        foreach (var file in Directory.EnumerateFiles(rulesDir, "*.md"))
        {
            if (!expectedRuleFiles.Contains(Path.GetFileName(file)))
            {
                File.Delete(file);
                Console.WriteLine($"Removed stale {Path.GetRelativePath(repoRoot, file)}");
            }
        }
        Console.WriteLine($"Wrote {descriptors.Count} per-rule pages under docs/rules/");

        // (3) SARIF v2.1.0 rule manifest.
        var sarifPath = RepoLayout.SarifPath(repoRoot);
        File.WriteAllText(sarifPath, SarifRenderer.Render(descriptors, idToClass));
        Console.WriteLine($"Wrote {Path.GetRelativePath(repoRoot, sarifPath)}");

        // (4) Editorconfig profiles.
        foreach (var (path, content) in EditorconfigRenderer.EnumerateProfiles(repoRoot, descriptors))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, content);
            Console.WriteLine($"Wrote {Path.GetRelativePath(repoRoot, path)}");
        }
        return 0;
    }
}
