// Copyright (c) Alexander Nachtmann
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Immutable;
using System.Reflection;
using System.Text;
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
        if (!File.Exists(outputPath))
        {
            Console.Error.WriteLine($"Missing generated docs: {outputPath}");
            return 1;
        }

        if (!string.Equals(File.ReadAllText(outputPath), RenderMarkdown(descriptors, fixableIds), StringComparison.Ordinal))
        {
            Console.Error.WriteLine($"Generated docs are stale: {outputPath}");
            return 1;
        }

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

        Console.WriteLine($"Generated docs are up to date: {Path.GetRelativePath(repoRoot, outputPath)}");
        Console.WriteLine("Editorconfig profiles are up to date.");
        return 0;
    }

    private static int Generate(
        IReadOnlyList<DiagnosticDescriptor> descriptors,
        HashSet<string> fixableIds,
        string outputPath,
        string repoRoot)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        File.WriteAllText(outputPath, RenderMarkdown(descriptors, fixableIds));
        Console.WriteLine($"Wrote {Path.GetRelativePath(repoRoot, outputPath)}");

        foreach (var (path, content) in EnumerateEditorconfigProfiles(repoRoot, descriptors))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, content);
            Console.WriteLine($"Wrote {Path.GetRelativePath(repoRoot, path)}");
        }
        return 0;
    }

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

    private static string RenderMarkdown(IReadOnlyList<DiagnosticDescriptor> descriptors, HashSet<string> fixableIds)
    {
        var sb = new StringBuilder();
        WriteHeader(sb);
        sb.AppendLine();
        WriteDiagnostics(sb, descriptors, fixableIds);
        sb.AppendLine();
        WriteRuleReference(sb, descriptors, fixableIds);
        sb.AppendLine();
        WriteGeneratedFile(sb);
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
        HashSet<string> fixableIds)
    {
        sb.AppendLine("## Diagnostics");
        sb.AppendLine();
        sb.AppendLine("| ID | Severity | Title | Code fix | Description |");
        sb.AppendLine("| -- | -- | -- | -- | -- |");
        foreach (var d in descriptors)
        {
            var fix = fixableIds.Contains(d.Id) ? "Yes" : "No";
            sb.AppendLine($"| {d.Id} | {d.DefaultSeverity} | {Escape(d.Title.ToString())} | {fix} | {Escape(d.Description.ToString())} |");
        }
    }

    private static void WriteRuleReference(
        StringBuilder sb,
        IReadOnlyList<DiagnosticDescriptor> descriptors,
        HashSet<string> fixableIds)
    {
        sb.AppendLine("## Rule Reference");
        sb.AppendLine();
        sb.AppendLine("Each rule below has a stable GitHub anchor (`#al1000`, `#al1001`, …) that every `DiagnosticDescriptor.HelpLinkUri` resolves to. IDE \"Show error help\" links deep-link straight to the matching sub-section.");
        sb.AppendLine();
        foreach (var d in descriptors)
        {
            sb.AppendLine($"### {d.Id}");
            sb.AppendLine();
            sb.AppendLine($"**{Escape(d.Title.ToString())}** — *{d.DefaultSeverity}, category `{d.Category}`*");
            sb.AppendLine();
            sb.AppendLine(Escape(d.Description.ToString()));
            sb.AppendLine();
            sb.AppendLine($"Code fix: {(fixableIds.Contains(d.Id) ? "Yes" : "No")}.");
            sb.AppendLine();
        }
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
