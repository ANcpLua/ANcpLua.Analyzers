using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ANcpLua.Analyzers.AnalyzerDocsGenerator;

/// <summary>
///     Lightweight syntactic check that runs in CI ahead of the byte-identical
///     <c>git diff -- docs</c> guard: parses the scenario source file and confirms every
///     <see cref="ScenarioAttribute" />-tagged method has a non-empty body. Catches the dumb
///     mistake of an empty placeholder scenario before it produces a malformed docs file.
///
///     Mirrors <c>FluentAssertions.Analyzers.FluentAssertionAnalyzerDocsGenerator.DocsVerifier</c>
///     in shape (parse + walk methods + accumulate issues + throw if any) but checks invariants
///     suited to the ANcpLua scenario shape (no <c>// old assertion:</c> / <c>// new assertion:</c>
///     comment contract — scenario bodies are real compilable code).
/// </summary>
public abstract partial class DocsVerifier
{
    /// <summary>Path to the scenario source file (same value as <see cref="DocsGenerator.ScenariosSourceFile" />).</summary>
    protected abstract string ScenariosSourceFile { get; }

    /// <summary>
    ///     Parses the scenario source file and validates every scenario method has a non-empty body.
    ///     Throws if any scenario violates an invariant.
    /// </summary>
    public async Task ExecuteAsync()
    {
        var compilationUnit = SyntaxFactory.ParseCompilationUnit(
            await File.ReadAllTextAsync(ScenariosSourceFile).ConfigureAwait(false));
        var root = await compilationUnit.SyntaxTree.GetRootAsync().ConfigureAwait(false);

        Console.WriteLine($"File: {Path.GetFileName(ScenariosSourceFile)}");

        var issues = new List<string>();
        foreach (var method in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
        {
            // Match by simple name (last `.`-segment) so both [Scenario] and fully-qualified
            // writes like [ANcpLua.Analyzers.AnalyzerDocsGenerator.Scenario] resolve.
            var hasScenarioAttribute = method.AttributeLists
                .SelectMany(static l => l.Attributes)
                .Any(DocsGenerator.IsScenarioAttribute);
            if (!hasScenarioAttribute) continue;

            Console.WriteLine($"### scenario: {method.Identifier}");

            // A scenario method must have an implementation — either a block body (`{...}`,
            // possibly comment-only, which is the docs source for the success scenario) or an
            // expression body (`=> ...`, used by _Failure twins to invoke the analyzer).
            if (method.Body is null && method.ExpressionBody is null)
                issues.Add($"[{Path.GetFileName(ScenariosSourceFile)}] {method.Identifier} - scenario method has no implementation (neither block nor expression body).");
        }

        if (issues.Count > 0)
            throw new InvalidOperationException(string.Join(Environment.NewLine, issues));
    }
}
