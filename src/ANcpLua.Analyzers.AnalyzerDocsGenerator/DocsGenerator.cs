using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ANcpLua.Analyzers.AnalyzerDocsGenerator;

/// <summary>
///     Reflects over a docs scenarios assembly, parses each scenario class's source file with
///     Roslyn, walks methods tagged with <see cref="ScenarioAttribute" />, and emits one markdown
///     file per scenario class — matching the architecture of
///     <c>FluentAssertions.Analyzers.FluentAssertionAnalyzerDocsGenerator.DocsGenerator</c>.
///
///     For each scenario method, looks up a <c>{name}_Failure</c> twin; invokes it to capture the
///     analyzer's diagnostic message via <see cref="AnalyzerDocsUtils.RunAndCaptureDiagnosticAsync{T}" />,
///     and inlines those messages into the generated <c>#### Failure messages</c> block so the docs
///     always reflect the analyzer's current diagnostic text.
/// </summary>
public abstract partial class DocsGenerator
{
    private const string DocsSuffix = "Docs";
    private const string FailureSuffix = "_Failure";

    /// <summary>The compiled assembly that holds the scenario classes (for reflection-based invocation).</summary>
    protected abstract Assembly ScenariosAssembly { get; }

    /// <summary>Path to the scenario source file (parsed for code-extraction; comment markers drive layout).</summary>
    protected abstract string ScenariosSourceFile { get; }

    /// <summary>
    ///     Parses <see cref="ScenariosSourceFile" />, generates markdown, and writes it to
    ///     <c>docs/{ClassName-without-Docs-suffix}.md</c> at the repo root.
    /// </summary>
    public async Task ExecuteAsync()
    {
        Console.WriteLine($"Input file: {ScenariosSourceFile}");

        var compilationUnit = SyntaxFactory.ParseCompilationUnit(
            await File.ReadAllTextAsync(ScenariosSourceFile).ConfigureAwait(false));
        var tree = compilationUnit.SyntaxTree;
        var root = await tree.GetRootAsync().ConfigureAwait(false);

        Console.WriteLine($"File: {Path.GetFileName(ScenariosSourceFile)}");

        var fileBaseName = Path.GetFileNameWithoutExtension(ScenariosSourceFile);
        var docsName = fileBaseName.EndsWithOrdinal(DocsSuffix)
            ? fileBaseName[..^DocsSuffix.Length] + ".md"
            : fileBaseName + ".md";

        var classDef = root.DescendantNodes().OfType<ClassDeclarationSyntax>().FirstOrDefault()
            ?? throw new InvalidOperationException(
                $"No class declaration found in {ScenariosSourceFile} — every scenarios source file must declare exactly one Docs class.");

        var namespaceName = ResolveNamespace(root);
        var classFullName = string.IsNullOrEmpty(namespaceName)
            ? classDef.Identifier.Text
            : $"{namespaceName}.{classDef.Identifier.Text}";

        var classType = ScenariosAssembly.GetType(classFullName)
            ?? throw new InvalidOperationException($"Could not find {classFullName} in {ScenariosAssembly.FullName}.");
        var classInstance = Activator.CreateInstance(classType)
            ?? throw new InvalidOperationException($"Could not instantiate {classFullName}.");

        var methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>().ToList();
        var methodsMap = methods.ToDictionary(static m => m.Identifier.Text);

        var docs = new StringBuilder();
        var toc = new StringBuilder();
        var scenarios = new StringBuilder();

        docs.AppendLine("<!--");
        docs.AppendLine("This is a generated file, please edit src/ANcpLua.Analyzers.AnalyzerDocs/<ScenarioClass>.cs");
        docs.AppendLine("and re-run scripts/generate-docs.ps1 to refresh.");
        docs.AppendLine("-->");
        docs.AppendLine();

        var subject = fileBaseName.EndsWithOrdinal(DocsSuffix)
            ? fileBaseName[..^DocsSuffix.Length]
            : fileBaseName;
        docs.AppendLine($"# {subject} Analyzer Docs");
        docs.AppendLine();

        scenarios.AppendLine("## Scenarios");
        scenarios.AppendLine();

        foreach (var method in methods.Where(IsScenarioMethod))
        {
            // Success scenarios MUST have a block body — that body IS the markdown source.
            // Expression-bodied success scenarios would have nothing to extract, so warn + skip.
            if (method.Body is null)
            {
                Console.WriteLine($"  skipping '{method.Identifier}' — success scenarios need a block body (its contents become the markdown).");
                continue;
            }

            var bodyLines = method.Body.ToFullString().Split(Environment.NewLine)[1..^2];
            if (bodyLines.Length is 0) continue;

            var paddingToRemove = bodyLines[0].IndexOfOrdinal(bodyLines[0].TrimStart());
            var normalizedBody = string.Join(
                Environment.NewLine,
                bodyLines.Select(l => l.Length > paddingToRemove ? l.Substring(paddingToRemove) : l));

            scenarios.AppendLine($"### scenario: {method.Identifier}");
            scenarios.AppendLine();
            scenarios.AppendLine("```cs");
            scenarios.AppendLine(normalizedBody);
            scenarios.AppendLine("```");
            scenarios.AppendLine();

            var lastNonEmpty = bodyLines.LastOrDefault(static l => !string.IsNullOrWhiteSpace(l))?.Trim() ?? string.Empty;
            toc.AppendLine($"- [{method.Identifier}](#scenario-{method.Identifier.Text.ToLowerInvariant()}) - `{lastNonEmpty}`");

            if (methodsMap.TryGetValue($"{method.Identifier.Text}{FailureSuffix}", out var failureMethod))
                await AppendFailureScenarioAsync(scenarios, failureMethod, classType, classInstance).ConfigureAwait(false);
        }

        docs.AppendLine(toc.ToString());
        docs.AppendLine();
        docs.AppendLine(scenarios.ToString());

        var docsPath = Path.Combine(ResolveDocsRoot(), docsName);
        Directory.CreateDirectory(Path.GetDirectoryName(docsPath)!);
        await File.WriteAllTextAsync(docsPath, docs.ToString()).ConfigureAwait(false);
        Console.WriteLine($"Wrote: {Path.GetFullPath(docsPath)}");
    }

    private static bool IsScenarioMethod(MethodDeclarationSyntax method) =>
        !method.Identifier.Text.EndsWithOrdinal(FailureSuffix)
        && method.AttributeLists.SelectMany(static l => l.Attributes).Any(IsScenarioAttribute);

    /// <summary>
    ///     Matches the attribute by simple name (last segment after <c>.</c>) so both
    ///     <c>[Scenario]</c> and fully-qualified writes like
    ///     <c>[ANcpLua.Analyzers.AnalyzerDocsGenerator.Scenario]</c> resolve.
    /// </summary>
    internal static bool IsScenarioAttribute(AttributeSyntax attribute)
    {
        var simpleName = attribute.Name.ToString().Split('.')[^1];
        return simpleName is "Scenario" or "ScenarioAttribute";
    }

    private static async Task AppendFailureScenarioAsync(
        StringBuilder scenarios,
        MethodDeclarationSyntax failureMethod,
        Type classType,
        object classInstance)
    {
        var diagnosticMessage = await InvokeAndCaptureMessageAsync(
            classInstance,
            classType.GetMethod(failureMethod.Identifier.Text)
                ?? throw new InvalidOperationException(
                    $"Could not find runtime method {failureMethod.Identifier.Text} on {classType.FullName}."))
            .ConfigureAwait(false);

        if (string.IsNullOrEmpty(diagnosticMessage)) return;

        scenarios.AppendLine("#### Diagnostic");
        scenarios.AppendLine();
        scenarios.AppendLine("```text");
        foreach (var line in diagnosticMessage.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            scenarios.AppendLine(line.TrimEnd());
        scenarios.AppendLine("```");
        scenarios.AppendLine();
    }

    private static async Task<string> InvokeAndCaptureMessageAsync(object instance, MethodInfo method)
    {
        try
        {
            var result = method.Invoke(instance, parameters: null);
            if (result is Task task) await task.ConfigureAwait(false);
            return string.Empty;
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            return AnalyzerDocsUtils.ReplaceStackTrace(ex.InnerException.Message);
        }
        catch (Exception ex)
        {
            return AnalyzerDocsUtils.ReplaceStackTrace(ex.Message);
        }
    }

    /// <summary>
    ///     Resolves the absolute path of the repo's <c>docs/</c> folder by walking up from CWD
    ///     looking for <c>.git</c> or <c>.github</c>. Falls back to the legacy CWD-relative path
    ///     (<c>../../docs</c>) only if neither marker is found — keeps the script-driven invocation
    ///     working while making ad-hoc <c>dotnet run --project</c> from any CWD also work.
    /// </summary>
    private static string ResolveDocsRoot()
    {
        var repoRoot = FindRepoRoot(Environment.CurrentDirectory);
        return repoRoot is not null
            ? Path.Combine(repoRoot, "docs")
            : Path.Combine(Environment.CurrentDirectory, "..", "..", "docs");
    }

    private static string? FindRepoRoot(string startPath)
    {
        var dir = new DirectoryInfo(startPath);
        while (dir is not null)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, ".git"))
                || Directory.Exists(Path.Combine(dir.FullName, ".github")))
                return dir.FullName;
            dir = dir.Parent;
        }
        return null;
    }

    private static string ResolveNamespace(SyntaxNode root) =>
        root.DescendantNodes().OfType<FileScopedNamespaceDeclarationSyntax>().FirstOrDefault()?.Name.ToString()
        ?? root.DescendantNodes().OfType<NamespaceDeclarationSyntax>().FirstOrDefault()?.Name.ToString()
        ?? string.Empty;
}
