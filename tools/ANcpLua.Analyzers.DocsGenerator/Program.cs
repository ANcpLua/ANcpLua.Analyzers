// Copyright (c) Alexander Nachtmann
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Immutable;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using ANcpLua.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

return DocsGenerator.Run(args);

file static class DocsGenerator
{
    private const string PackageName = "ANcpLua.Analyzers";
    private const string ProjectRelativePath = "tools/ANcpLua.Analyzers.DocsGenerator";
    private const string SolutionFileName = "ANcpLua.Analyzers.slnx";

    public static int Run(string[] args)
    {
        var repoRoot = FindRepoRoot(AppContext.BaseDirectory);
        var outputPath = Path.Combine(repoRoot, "docs", PackageName + ".md");
        var mode = ParseMode(args);

        if (mode is Mode.EnforceIdsCheck or Mode.EnforceIdsApply)
            return EnforceIds(repoRoot, apply: mode == Mode.EnforceIdsApply);

        var descriptors = GetDescriptors();
        var fixableIds = GetFixableDiagnosticIds();

        return mode switch
        {
            Mode.Audit => Audit(descriptors, fixableIds),
            Mode.Check => Check(descriptors, fixableIds, outputPath, repoRoot),
            _ => Generate(descriptors, fixableIds, outputPath, repoRoot),
        };
    }

    private static int Audit(IReadOnlyList<DiagnosticDescriptor> descriptors, HashSet<string> fixableIds)
    {
        Console.WriteLine($"{PackageName} catalog audit");
        Console.WriteLine($"  Total descriptors: {descriptors.Count}");
        Console.WriteLine($"  With code fix:     {descriptors.Count(d => fixableIds.Contains(d.Id))}");
        foreach (var g in descriptors.GroupBy(d => d.DefaultSeverity).OrderByDescending(g => g.Key))
            Console.WriteLine($"  Severity {g.Key,-10}  {g.Count()}");
        return 0;
    }

    private static int Check(
        IReadOnlyList<DiagnosticDescriptor> descriptors,
        HashSet<string> fixableIds,
        string outputPath,
        string repoRoot)
    {
        var idToClass = BuildIdToClassMap();

        // (1) Slim index file.
        if (!File.Exists(outputPath))
        {
            Console.Error.WriteLine($"Missing generated docs: {outputPath}");
            return 1;
        }
        if (!string.Equals(File.ReadAllText(outputPath), RenderIndex(descriptors, fixableIds, idToClass), StringComparison.Ordinal))
        {
            Console.Error.WriteLine($"Index docs are stale: {Path.GetRelativePath(repoRoot, outputPath)}");
            return 1;
        }

        // (2) Per-rule pages under docs/rules/.
        var rulesDir = Path.Combine(repoRoot, "docs", "rules");
        var expectedRuleFiles = new HashSet<string>(StringComparer.Ordinal);
        foreach (var d in descriptors)
        {
            if (!idToClass.TryGetValue(d.Id, out var className))
            {
                Console.Error.WriteLine($"Descriptor {d.Id} has no owning DiagnosticAnalyzer class — cannot place per-rule page.");
                return 1;
            }
            var symbolic = ToSymbolicName(className);
            var rulePath = Path.Combine(rulesDir, $"{d.Id}_{symbolic}.md");
            expectedRuleFiles.Add(Path.GetFileName(rulePath));

            if (!File.Exists(rulePath))
            {
                Console.Error.WriteLine($"Missing per-rule page: {Path.GetRelativePath(repoRoot, rulePath)}");
                return 1;
            }
            if (!string.Equals(File.ReadAllText(rulePath), RenderRulePage(d, className, fixableIds), StringComparison.Ordinal))
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
        foreach (var (path, expected) in EnumerateEditorconfigProfiles(repoRoot, descriptors))
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
        var sarifPath = SarifPath(repoRoot);
        if (!File.Exists(sarifPath))
        {
            Console.Error.WriteLine($"Missing SARIF manifest: {Path.GetRelativePath(repoRoot, sarifPath)}");
            return 1;
        }
        if (!string.Equals(File.ReadAllText(sarifPath), RenderSarif(descriptors, idToClass), StringComparison.Ordinal))
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
        IReadOnlyList<DiagnosticDescriptor> descriptors,
        HashSet<string> fixableIds,
        string outputPath,
        string repoRoot)
    {
        var idToClass = BuildIdToClassMap();

        // Slim index.
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        File.WriteAllText(outputPath, RenderIndex(descriptors, fixableIds, idToClass));
        Console.WriteLine($"Wrote {Path.GetRelativePath(repoRoot, outputPath)}");

        // Per-rule pages.
        var rulesDir = Path.Combine(repoRoot, "docs", "rules");
        Directory.CreateDirectory(rulesDir);
        var expectedRuleFiles = new HashSet<string>(StringComparer.Ordinal);
        foreach (var d in descriptors)
        {
            if (!idToClass.TryGetValue(d.Id, out var className)) continue;
            var symbolic = ToSymbolicName(className);
            var rulePath = Path.Combine(rulesDir, $"{d.Id}_{symbolic}.md");
            expectedRuleFiles.Add(Path.GetFileName(rulePath));
            File.WriteAllText(rulePath, RenderRulePage(d, className, fixableIds));
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

        // SARIF v2.1.0 rule manifest.
        var sarifPath = SarifPath(repoRoot);
        File.WriteAllText(sarifPath, RenderSarif(descriptors, idToClass));
        Console.WriteLine($"Wrote {Path.GetRelativePath(repoRoot, sarifPath)}");

        // Editorconfig profiles (unchanged).
        foreach (var (path, content) in EnumerateEditorconfigProfiles(repoRoot, descriptors))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, content);
            Console.WriteLine($"Wrote {Path.GetRelativePath(repoRoot, path)}");
        }
        return 0;
    }

    private static string SarifPath(string repoRoot) =>
        Path.Combine(repoRoot, "docs", PackageName + ".sarif");

    /// <summary>
    ///   Emits a SARIF v2.1.0 rule manifest describing every <see cref="DiagnosticDescriptor"/>
    ///   this package ships. Each descriptor maps to one <c>reportingDescriptor</c> entry
    ///   inside <c>runs[0].tool.driver.rules</c>. The file is run-results-free —
    ///   <c>runs[0].results</c> is empty — because this is a *rule catalog* for tool
    ///   interop (Sonar bridges, GitHub Advanced Security uploads, IDE rule catalogs),
    ///   not an analyzer execution result. Indent + sort-by-id keeps the output
    ///   deterministic for <c>--check</c> drift detection.
    ///
    ///   Spec: https://docs.oasis-open.org/sarif/sarif/v2.1.0/sarif-v2.1.0.html
    /// </summary>
    private static string RenderSarif(
        IReadOnlyList<DiagnosticDescriptor> descriptors,
        Dictionary<string, string> idToClass)
    {
        var rulesArray = new JsonArray();
        foreach (var d in descriptors)
        {
            var ruleName = idToClass.TryGetValue(d.Id, out var className)
                ? ToSymbolicName(className)
                : d.Id;

            var rule = new JsonObject
            {
                ["id"] = d.Id,
                ["name"] = ruleName,
                ["shortDescription"] = new JsonObject { ["text"] = d.Title.ToString() },
                ["fullDescription"] = new JsonObject { ["text"] = d.Description.ToString() },
                ["helpUri"] = d.HelpLinkUri,
            };

            var defaultConfig = new JsonObject { ["level"] = SarifLevel(d.DefaultSeverity) };
            if (!d.IsEnabledByDefault)
                defaultConfig["enabled"] = false;
            rule["defaultConfiguration"] = defaultConfig;

            rule["properties"] = new JsonObject { ["category"] = d.Category };
            rulesArray.Add(rule);
        }

        var doc = new JsonObject
        {
            ["$schema"] = "https://json.schemastore.org/sarif-2.1.0.json",
            ["version"] = "2.1.0",
            ["runs"] = new JsonArray(
                new JsonObject
                {
                    ["tool"] = new JsonObject
                    {
                        ["driver"] = new JsonObject
                        {
                            ["name"] = PackageName,
                            ["informationUri"] = "https://github.com/ANcpLua/ANcpLua.Analyzers",
                            ["rules"] = rulesArray,
                        },
                    },
                    ["results"] = new JsonArray(),
                }),
        };

        var json = doc.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
        return json.ReplaceLineEndings("\n") + "\n";
    }

    private static string SarifLevel(DiagnosticSeverity severity) => severity switch
    {
        DiagnosticSeverity.Error => "error",
        DiagnosticSeverity.Warning => "warning",
        DiagnosticSeverity.Info => "note",
        DiagnosticSeverity.Hidden => "none",
        _ => "none",
    };

    private static Mode ParseMode(string[] args)
    {
        var flat = args
            .SelectMany(a => a.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries))
            .ToArray();

        var enforce = flat.Any(a => IsFlag(a, "enforce-ids"));
        var apply = flat.Any(a => IsFlag(a, "apply"));
        if (enforce) return apply ? Mode.EnforceIdsApply : Mode.EnforceIdsCheck;

        foreach (var arg in flat)
        {
            if (IsFlag(arg, "audit")) return Mode.Audit;
            if (IsFlag(arg, "check") || Eq(arg, "validate")) return Mode.Check;
        }
        return Mode.Generate;

        static bool IsFlag(string arg, string name) => Eq(arg, name) || Eq(arg, "--" + name);
        static bool Eq(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
    }

    // ─── Markdown rendering ─────────────────────────────────────────────────

    private static string RenderIndex(
        IReadOnlyList<DiagnosticDescriptor> descriptors,
        HashSet<string> fixableIds,
        Dictionary<string, string> idToClass)
    {
        var sb = new StringBuilder();
        WriteHeader(sb);
        sb.AppendLine();
        WriteDiagnostics(sb, descriptors, fixableIds, idToClass);
        sb.AppendLine();
        WriteRelatedDocs(sb);
        sb.AppendLine();
        WriteGeneratedFile(sb);
        return sb.ToString().ReplaceLineEndings("\n");
    }

    /// <summary>
    ///   Emits one markdown page per rule, keyed by id + symbolic name. Each page is
    ///   small (header + property table + description + see-also) so IDE Quick-Fix
    ///   "Show error help" links resolve onto a focused page rather than the
    ///   multi-thousand-line aggregate.
    /// </summary>
    private static string RenderRulePage(
        DiagnosticDescriptor descriptor,
        string className,
        HashSet<string> fixableIds)
    {
        var sb = new StringBuilder();
        var title = Escape(descriptor.Title.ToString());
        var description = Escape(descriptor.Description.ToString());
        var codeFix = fixableIds.Contains(descriptor.Id) ? "Yes" : "No";
        var sourceBasename = FileBasenameForClass(className);

        sb.AppendLine($"# {descriptor.Id}: {title}");
        sb.AppendLine();
        sb.AppendLine($"<!-- <auto-generated /> This file is generated by {ProjectRelativePath}. -->");
        sb.AppendLine();
        sb.AppendLine("| Property | Value |");
        sb.AppendLine("| -- | -- |");
        sb.AppendLine($"| Severity | {descriptor.DefaultSeverity} |");
        sb.AppendLine($"| Category | `{descriptor.Category}` |");
        sb.AppendLine($"| Code fix | {codeFix} |");
        sb.AppendLine($"| Analyzer | `{className}` |");
        sb.AppendLine($"| Enabled by default | {(descriptor.IsEnabledByDefault ? "Yes" : "No")} |");
        sb.AppendLine();
        sb.AppendLine("## Description");
        sb.AppendLine();
        sb.AppendLine(description);
        sb.AppendLine();
        sb.AppendLine("## See also");
        sb.AppendLine();
        sb.AppendLine($"- [Rule index](../{PackageName}.md)");
        sb.AppendLine($"- [Source: `{sourceBasename}.cs`](../../src/{PackageName}/Analyzers/{sourceBasename}.cs)");
        return sb.ToString().ReplaceLineEndings("\n");
    }

    private static void WriteHeader(StringBuilder sb)
    {
        sb.AppendLine($"# {PackageName}");
        sb.AppendLine();
        sb.AppendLine($"<!-- <auto-generated /> This file is generated by {ProjectRelativePath}. -->");
        sb.AppendLine();
        sb.AppendLine("Roslyn analyzers + code fixes covering modern C# correctness pitfalls, ASP.NET Core / Aspire patterns, Roslyn-utility hygiene, async/threading reliability, AOT/trim safety, package versioning, style, and agent-tool governance. Auto-injected by `ANcpLua.NET.Sdk` into every consuming project.");
        sb.AppendLine();
        sb.AppendLine("## Package family");
        sb.AppendLine();
        sb.AppendLine("- **[ANcpLua.Analyzers](https://github.com/ANcpLua/ANcpLua.Analyzers)** — this package; the `AL00xx`–`AL18xx` Roslyn diagnostics.");
        sb.AppendLine("- **[ANcpLua.NET.Sdk](https://github.com/ANcpLua/ANcpLua.NET.Sdk)** — MSBuild SDK that auto-injects this analyzer + the bundled `editorconfig` severity profile.");
        sb.AppendLine("- **[ANcpLua.Roslyn.Utilities](https://github.com/ANcpLua/ANcpLua.Roslyn.Utilities)** — shared Roslyn helpers + the `Guard.*` API the `AL12xx` band promotes.");
    }

    private static void WriteDiagnostics(
        StringBuilder sb,
        IReadOnlyList<DiagnosticDescriptor> descriptors,
        HashSet<string> fixableIds,
        Dictionary<string, string> idToClass)
    {
        sb.AppendLine("## Diagnostics");
        sb.AppendLine();
        sb.AppendLine("Each ID links to a per-rule page under [`docs/rules/`](rules/) with severity, category, code-fix status, and description. The descriptor's `HelpLinkUri` resolves to the same page, so IDE Quick-Fix \"Show error help\" lands on the focused rule, not on this index.");
        sb.AppendLine();
        sb.AppendLine("| ID | Severity | Title | Code fix |");
        sb.AppendLine("| -- | -- | -- | -- |");
        foreach (var d in descriptors)
        {
            var fix = fixableIds.Contains(d.Id) ? "Yes" : "No";
            var link = idToClass.TryGetValue(d.Id, out var className)
                ? $"[{d.Id}](rules/{d.Id}_{ToSymbolicName(className)}.md)"
                : d.Id;
            sb.AppendLine($"| {link} | {d.DefaultSeverity} | {Escape(d.Title.ToString())} | {fix} |");
        }
    }

    private static void WriteRelatedDocs(StringBuilder sb)
    {
        sb.AppendLine("## Consumer-side severity profile (`AlAnalysisMode`)");
        sb.AppendLine();
        sb.AppendLine("Set `<AlAnalysisMode>` in your csproj to switch the whole `AL00xx`–`AL18xx` band in one line instead of dropping editorconfig files:");
        sb.AppendLine();
        sb.AppendLine("```xml");
        sb.AppendLine("<PropertyGroup>");
        sb.AppendLine("  <AlAnalysisMode>AllAsErrors</AlAnalysisMode>");
        sb.AppendLine("</PropertyGroup>");
        sb.AppendLine("```");
        sb.AppendLine();
        sb.AppendLine("| Value | Behavior |");
        sb.AppendLine("| -- | -- |");
        sb.AppendLine("| `Default` | Every rule at its descriptor-declared default severity. Useful to override an ambient stricter config (incl. ANcpLua.NET.Sdk's bundled profile). |");
        sb.AppendLine("| `AllAsErrors` | Every AL rule promoted to error. Use for strict CI. |");
        sb.AppendLine("| `Disabled` | Every AL rule silenced. |");
        sb.AppendLine("| _(unset)_ | No editorconfig injection. Inside an ANcpLua.NET.Sdk consumer, the SDK's bundled editorconfig still applies; outside it, descriptor severities apply. |");
        sb.AppendLine();
        sb.AppendLine("The property is exposed via the analyzer NuGet's `buildTransitive/ANcpLua.Analyzers.props`, which appends the matching editorconfig from `buildTransitive/editorconfig/` to `$(EditorConfigFiles)` on consumer restore. The name is deliberately not bare `<AnalysisMode>` — that property is owned by `Microsoft.CodeAnalysis.NetAnalyzers` and clashing would force consumers into one-or-the-other choices.");
        sb.AppendLine();
        sb.AppendLine("## See also");
        sb.AppendLine();
        sb.AppendLine("- [Per-rule pages](rules/) — one markdown file per `AL00xx`–`AL18xx` rule with severity, category, code-fix status, and description.");
        sb.AppendLine("- [Editorconfig profiles](editorconfig/) — three drop-in severity profiles: `Default`, `AllRulesAsErrors`, `AllRulesDisabled`. Same content ships inside the NuGet under `buildTransitive/editorconfig/`.");
        sb.AppendLine($"- [SARIF rule manifest]({PackageName}.sarif) — SARIF v2.1.0 catalog of every `AL00xx`–`AL18xx` rule (id, name, severity, category, helpUri). Consume from Sonar bridges, GitHub Advanced Security uploads, or IDE rule-catalog tools.");
        sb.AppendLine("- [`AnalyzerReleases.Unshipped.md`](../src/ANcpLua.Analyzers/AnalyzerReleases.Unshipped.md) — release-tracking manifest with `ClassName` attribution per Microsoft NetAnalyzers convention.");
    }

    private static void WriteGeneratedFile(StringBuilder sb)
    {
        sb.AppendLine("## Generated File");
        sb.AppendLine();
        sb.AppendLine("Regenerate with:");
        sb.AppendLine();
        sb.AppendLine("```bash");
        sb.AppendLine($"dotnet run --project {ProjectRelativePath}              # rewrite docs + editorconfig profiles");
        sb.AppendLine($"dotnet run --project {ProjectRelativePath} -- --check   # CI guard; fails if either is stale");
        sb.AppendLine($"dotnet run --project {ProjectRelativePath} -- --audit   # print catalog statistics");
        sb.AppendLine("```");
    }

    // ─── Symbolic-name + on-disk file mapping ───────────────────────────────

    /// <summary>
    ///   Strips the <c>Analyzer</c> suffix and any <c>Al\d{4}</c> prefix off the
    ///   class name; the remainder is the symbolic part used in per-rule docs
    ///   filenames <c>docs/rules/{id}_{symbolic}.md</c> and in the help-link URL.
    ///   Normalizes embedded uppercase <c>AL\d{4}</c> to Pascal-case <c>Al\d{4}</c>
    ///   first so the output matches <c>RuleDocs.SymbolicNameFromFile</c> on multi-id
    ///   classes (e.g., <c>AL1003ToAL1004SpanComparison</c> in a file basename vs
    ///   <c>Al1003ToAl1004SpanComparison</c> in a reflected class name).
    /// </summary>
    private static string ToSymbolicName(string className)
    {
        var name = Regex.Replace(className, @"AL(\d{4})", "Al$1");
        if (name.EndsWith("Analyzer", StringComparison.Ordinal))
            name = name[..^"Analyzer".Length];
        var prefix = Regex.Match(name, @"^Al\d{4}");
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
    private static string FileBasenameForClass(string className) =>
        Regex.Replace(className, @"Al(\d{4})", "AL$1");

    /// <summary>
    ///   Walks every concrete <see cref="DiagnosticAnalyzer"/> in the analyzer assembly
    ///   and builds <c>Id → ClassName</c>. Analyzers that register multiple ids point
    ///   all of those ids at the same class.
    /// </summary>
    private static Dictionary<string, string> BuildIdToClassMap()
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

    // ─── Editorconfig profile emission ──────────────────────────────────────

    private static IEnumerable<(string AbsolutePath, string Content)> EnumerateEditorconfigProfiles(
        string repoRoot,
        IReadOnlyList<DiagnosticDescriptor> descriptors)
    {
        var dir = Path.Combine(repoRoot, "docs", "editorconfig");

        yield return (Path.Combine(dir, "Default.editorconfig"),
            RenderEditorconfig(descriptors, "Each AL rule at its descriptor-declared default severity.",
                d => SeverityToken(d.DefaultSeverity)));

        yield return (Path.Combine(dir, "AllRulesAsErrors.editorconfig"),
            RenderEditorconfig(descriptors, "Every AL rule promoted to error. Use for strict CI.",
                _ => "error"));

        yield return (Path.Combine(dir, "AllRulesDisabled.editorconfig"),
            RenderEditorconfig(descriptors, "Every AL rule silenced. Use to opt the whole band out.",
                _ => "none"));
    }

    private static string RenderEditorconfig(
        IReadOnlyList<DiagnosticDescriptor> descriptors,
        string headerComment,
        Func<DiagnosticDescriptor, string> severityFor)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# <auto-generated /> Edits made by hand will be overwritten by");
        sb.AppendLine($"# {ProjectRelativePath}. Re-run `dotnet run --project {ProjectRelativePath}`");
        sb.AppendLine($"# after changing analyzer descriptors. See docs/{PackageName}.md for rule details.");
        sb.AppendLine($"# {headerComment}");
        sb.AppendLine();
        sb.AppendLine("root = false");
        sb.AppendLine();
        sb.AppendLine("[*.{cs,vb}]");
        foreach (var d in descriptors)
            sb.AppendLine($"dotnet_diagnostic.{d.Id}.severity = {severityFor(d)}");
        return sb.ToString().ReplaceLineEndings("\n");
    }

    private static string SeverityToken(DiagnosticSeverity severity) => severity switch
    {
        DiagnosticSeverity.Error => "error",
        DiagnosticSeverity.Warning => "warning",
        DiagnosticSeverity.Info => "suggestion",
        DiagnosticSeverity.Hidden => "silent",
        _ => "default",
    };

    // ─── Descriptor + code-fix reflection ───────────────────────────────────

    private static IReadOnlyList<DiagnosticDescriptor> GetDescriptors()
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

    private static HashSet<string> GetFixableDiagnosticIds()
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

    // ─── --enforce-ids: source ↔ descriptor consistency ─────────────────────

    /// <summary>
    ///   Walks every analyzer / code-fix source file under
    ///   <c>src/ANcpLua.Analyzers/Analyzers/</c> and aligns class names, XML doc
    ///   summaries, and <c>DiagnosticId</c>-const docs with the runtime
    ///   <c>DiagnosticDescriptor.Id</c> each class registers. The runtime descriptor
    ///   is the source of truth — RS2008 already locks the descriptor↔release-tracking
    ///   contract, and this tool propagates that same authority into source.
    /// </summary>
    private static int EnforceIds(string repoRoot, bool apply)
    {
        var analyzersDir = Path.Combine(repoRoot,
            "src", "ANcpLua.Analyzers", "Analyzers");

        var (analyzerIds, codeFixIds, allAnalyzerIds) = BuildClassMaps();

        var perFileFixes = new Dictionary<string, List<(string Description, Func<string, string> Apply)>>(
            StringComparer.Ordinal);
        var classRenames = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var path in Directory.EnumerateFiles(analyzersDir, "*.cs", SearchOption.TopDirectoryOnly))
        {
            var src = File.ReadAllText(path);
            var tree = CSharpSyntaxTree.ParseText(src);
            var classNode = tree.GetCompilationUnitRoot()
                .DescendantNodes().OfType<ClassDeclarationSyntax>().FirstOrDefault();
            if (classNode is null) continue;

            var className = classNode.Identifier.Text;

            string? realId = null;
            var isAnalyzer = false;
            if (analyzerIds.TryGetValue(className, out var id))
            {
                realId = id;
                isAnalyzer = true;
            }
            else if (codeFixIds.TryGetValue(className, out id))
            {
                realId = id;
            }
            else continue;

            // (1) Class rename when name carries an Al/AL numeric prefix that doesn't match realId.
            var prefixMatch = Regex.Match(className, @"^(?:Al|AL)(\d{4})(.+)$");
            if (prefixMatch.Success)
            {
                var expectedClassName = $"Al{realId[2..]}{prefixMatch.Groups[2].Value}";
                if (className != expectedClassName)
                    classRenames[className] = expectedClassName;
            }

            // (2) Class XML doc summary: rewrite "/// AL00XX:" tokens only when they
            //     don't appear in this class's SupportedDiagnostics set. Multi-diagnostic
            //     analyzers (e.g., AL1003ToAL1004 documents both IDs in the summary) are
            //     not "wrong" — each row is the doc for one of the registered descriptors.
            var validIdsForClass = isAnalyzer && allAnalyzerIds.TryGetValue(className, out var idSet)
                ? idSet
                : new HashSet<string>(StringComparer.Ordinal) { realId };
            var classTrivia = classNode.GetLeadingTrivia().ToFullString();
            var fixedClassTrivia = Regex.Replace(
                classTrivia,
                @"(///\s*)AL\d{4}:",
                m =>
                {
                    var found = m.Value.TrimStart('/', ' ').TrimEnd(':');
                    return validIdsForClass.Contains(found)
                        ? m.Value
                        : m.Groups[1].Value + realId + ":";
                });
            if (fixedClassTrivia != classTrivia)
            {
                var oldT = classTrivia;
                var newT = fixedClassTrivia;
                AddFix(perFileFixes, path,
                    $"class XML doc summary -> {realId}:",
                    s => s.Replace(oldT, newT));
            }

            // (3) DiagnosticId const docs + value (analyzer-only).
            if (isAnalyzer)
            {
                var diagIdField = classNode.Members.OfType<FieldDeclarationSyntax>()
                    .FirstOrDefault(f => f.Declaration.Variables
                        .Any(v => v.Identifier.Text == "DiagnosticId"));
                if (diagIdField is not null)
                {
                    var fieldTrivia = diagIdField.GetLeadingTrivia().ToFullString();
                    var fixedFieldTrivia = Regex.Replace(
                        fieldTrivia,
                        @"\bfor AL\d{4}\b",
                        $"for {realId}");
                    if (fixedFieldTrivia != fieldTrivia)
                    {
                        var oldT = fieldTrivia;
                        var newT = fixedFieldTrivia;
                        AddFix(perFileFixes, path,
                            $"DiagnosticId const doc -> for {realId}",
                            s => s.Replace(oldT, newT));
                    }

                    if (diagIdField.Declaration.Variables.First().Initializer?.Value is LiteralExpressionSyntax lit
                        && lit.IsKind(SyntaxKind.StringLiteralExpression))
                    {
                        var constId = lit.Token.ValueText;
                        if (constId != realId)
                        {
                            AddFix(perFileFixes, path,
                                $"DiagnosticId const value {constId} -> {realId}",
                                s => s.Replace($"\"{constId}\"", $"\"{realId}\""));
                        }
                    }
                }
            }
        }

        var totalRenames = classRenames.Count;
        var totalPerFile = perFileFixes.Values.Sum(l => l.Count);
        var totalIssues = totalRenames + totalPerFile;

        if (apply)
        {
            foreach (var path in Directory.EnumerateFiles(analyzersDir, "*.cs", SearchOption.TopDirectoryOnly))
            {
                var src = File.ReadAllText(path);
                var original = src;
                if (perFileFixes.TryGetValue(path, out var fixes))
                    foreach (var (_, applyFn) in fixes)
                        src = applyFn(src);
                foreach (var (oldName, newName) in classRenames)
                    src = Regex.Replace(src, @"\b" + Regex.Escape(oldName) + @"\b", newName);
                if (src != original)
                    File.WriteAllText(path, src);
            }
            Console.WriteLine(
                $"--enforce-ids --apply: {totalRenames} class renames + {totalPerFile} per-file fixes.");
            return 0;
        }

        foreach (var (oldName, newName) in classRenames.OrderBy(kv => kv.Key, StringComparer.Ordinal))
            Console.WriteLine($"  class rename: {oldName} -> {newName}");
        foreach (var (path, fixes) in perFileFixes.OrderBy(kv => kv.Key, StringComparer.Ordinal))
        {
            var rel = Path.GetRelativePath(repoRoot, path);
            foreach (var (desc, _) in fixes)
                Console.WriteLine($"  {rel}: {desc}");
        }
        Console.WriteLine(
            $"--enforce-ids: {totalIssues} mismatches ({totalRenames} class renames, {totalPerFile} per-file fixes).");
        return totalIssues == 0 ? 0 : 1;
    }

    private static (
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

    private static void AddFix(
        Dictionary<string, List<(string Description, Func<string, string> Apply)>> bucket,
        string path,
        string description,
        Func<string, string> apply)
    {
        if (!bucket.TryGetValue(path, out var list))
        {
            list = [];
            bucket[path] = list;
        }
        list.Add((description, apply));
    }

    // ─── Helpers ────────────────────────────────────────────────────────────

    private static string FindRepoRoot(string start)
    {
        for (var dir = new DirectoryInfo(start); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, SolutionFileName)))
                return dir.FullName;
        }
        throw new InvalidOperationException(
            $"Could not find repository root (no '{SolutionFileName}' in any parent of '{start}').");
    }

    private static string Escape(string value) =>
        value.Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal)
            .Replace("|", "\\|", StringComparison.Ordinal);
}

file enum Mode { Generate, Check, Audit, EnforceIdsCheck, EnforceIdsApply }
