// Copyright (c) Alexander Nachtmann
// SPDX-License-Identifier: Apache-2.0

namespace ANcpLua.Analyzers.DocsGenerator;

/// <summary>
///   Owns the package-identity constants and every path the generator reads or writes.
///   Centralising path math here keeps the renderer/rewriter classes free of any
///   filesystem layout knowledge — they ask <see cref="RepoLayout"/> for what they need
///   and stay testable as pure functions of <see cref="DiagnosticDescriptor"/> input.
/// </summary>
internal static class RepoLayout
{
    public const string PackageName = "ANcpLua.Analyzers";
    public const string ProjectRelativePath = "tools/ANcpLua.Analyzers.DocsGenerator";
    public const string SolutionFileName = "ANcpLua.Analyzers.slnx";

    public static string IndexPath(string repoRoot) =>
        Path.Combine(repoRoot, "docs", PackageName + ".md");

    public static string RulesDir(string repoRoot) =>
        Path.Combine(repoRoot, "docs", "rules");

    public static string RulePath(string repoRoot, string id, string symbolic) =>
        Path.Combine(RulesDir(repoRoot), $"{id}_{symbolic}.md");

    // Extension point: SARIF v2.1.0 manifest. Sibling machine-readable catalogs
    // (CodeQL pack, OWASP ASVS map, custom JSON for IDE rule pickers) plug in next to
    // this path the same way — add a SiblingPath helper here and a Renderer class.
    public static string SarifPath(string repoRoot) =>
        Path.Combine(repoRoot, "docs", PackageName + ".sarif");

    public static string EditorconfigDir(string repoRoot) =>
        Path.Combine(repoRoot, "docs", "editorconfig");

    public static string AnalyzersSourceDir(string repoRoot) =>
        Path.Combine(repoRoot, "src", PackageName, "Analyzers");

    /// <summary>
    ///   Walks up from the assembly directory looking for the solution file. The
    ///   generator is invoked from the AfterBuild target on the analyzer csproj (CWD =
    ///   repo root) and ad-hoc <c>dotnet run</c> (CWD = project dir), so anchoring on
    ///   the solution file makes both shapes work.
    /// </summary>
    public static string FindRepoRoot(string start)
    {
        for (var dir = new DirectoryInfo(start); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, SolutionFileName)))
                return dir.FullName;
        }

        throw new InvalidOperationException(
            $"Could not find repository root (no '{SolutionFileName}' in any parent of '{start}').");
    }
}
