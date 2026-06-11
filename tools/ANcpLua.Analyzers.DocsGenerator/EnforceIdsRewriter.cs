// Copyright (c) Alexander Nachtmann
// SPDX-License-Identifier: Apache-2.0

using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ANcpLua.Analyzers.DocsGenerator;

// Aligns analyzer/code-fix source (class names, XML doc summaries, DiagnosticId-const docs)
// with the runtime DiagnosticDescriptor.Id each class registers. The runtime descriptor is the
// source of truth: RS2008 already locks the descriptor<->release-tracking contract, and this
// propagates that same authority into source.
// Multi-id analyzers (e.g. AL1003ToAL1004 documenting both IDs) are not "wrong" -- each row
// matches a registered descriptor, which is why the rewriter consults AllAnalyzerIds.
internal static class EnforceIdsRewriter
{
    private static readonly Regex ClassPrefixRegex = new(@"^(?:Al|AL)(\d{4})(.+)$");
    private static readonly Regex XmlDocIdRegex = new(@"(///\s*)AL\d{4}:");
    private static readonly Regex FieldDocIdRegex = new(@"\bfor AL\d{4}\b");

    public static int Run(string repoRoot, bool apply)
    {
        var analyzersDir = RepoLayout.AnalyzersSourceDir(repoRoot);

        var (analyzerIds, codeFixIds, allAnalyzerIds) = DescriptorCatalog.BuildClassMaps();

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
            var prefixMatch = ClassPrefixRegex.Match(className);
            if (prefixMatch.Success)
            {
                var expectedClassName = $"Al{realId[2..]}{prefixMatch.Groups[2].Value}";
                if (className != expectedClassName)
                    classRenames[className] = expectedClassName;
            }

            // (2) Class XML doc summary: rewrite "/// AL####:" tokens only when they
            //     don't appear in this class's SupportedDiagnostics set. Multi-diagnostic
            //     analyzers (e.g., AL1003ToAL1004 documents both IDs in the summary) are
            //     not "wrong" — each row is the doc for one of the registered descriptors.
            var validIdsForClass = isAnalyzer && allAnalyzerIds.TryGetValue(className, out var idSet)
                ? idSet
                : new HashSet<string>(StringComparer.Ordinal) { realId };
            var classTrivia = classNode.GetLeadingTrivia().ToFullString();
            var fixedClassTrivia = XmlDocIdRegex.Replace(
                classTrivia,
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
                    s => s.Replace(oldT, newT, StringComparison.Ordinal));
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
                    var fixedFieldTrivia = FieldDocIdRegex.Replace(
                        fieldTrivia,
                        $"for {realId}");
                    if (fixedFieldTrivia != fieldTrivia)
                    {
                        var oldT = fieldTrivia;
                        var newT = fixedFieldTrivia;
                        AddFix(perFileFixes, path,
                            $"DiagnosticId const doc -> for {realId}",
                            s => s.Replace(oldT, newT, StringComparison.Ordinal));
                    }

                    if (diagIdField.Declaration.Variables.First().Initializer?.Value is LiteralExpressionSyntax lit
                        && lit.IsKind(SyntaxKind.StringLiteralExpression))
                    {
                        var constId = lit.Token.ValueText;
                        if (constId != realId)
                        {
                            AddFix(perFileFixes, path,
                                $"DiagnosticId const value {constId} -> {realId}",
                                s => s.Replace($"\"{constId}\"", $"\"{realId}\"", StringComparison.Ordinal));
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
}
