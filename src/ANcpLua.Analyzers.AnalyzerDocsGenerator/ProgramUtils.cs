namespace ANcpLua.Analyzers.AnalyzerDocsGenerator;

/// <summary>
///     Entry-point dispatcher for scenario projects' <c>Program.cs</c>. Mirrors
///     <c>FluentAssertions.Analyzers.FluentAssertionAnalyzerDocsGenerator.ProgramUtils</c>:
///     <c>dotnet run -- generate</c> regenerates the docs, <c>dotnet run -- verify</c> runs
///     the syntactic invariant checks (the byte-identical guard lives in
///     <c>scripts/generate-docs.ps1 -ValidateNoChanges</c> + the CI step).
/// </summary>
public static partial class ProgramUtils
{
    public static Task RunMainAsync<TDocsGenerator, TDocsVerifier>(string[] args)
        where TDocsGenerator : DocsGenerator, new()
        where TDocsVerifier : DocsVerifier, new() => args switch
    {
        ["generate"] => new TDocsGenerator().ExecuteAsync(),
        ["verify"] => new TDocsVerifier().ExecuteAsync(),
        _ => throw new ArgumentException(
            "Invalid arguments — use 'generate' to regenerate docs/*.md or 'verify' to run invariant checks.",
            nameof(args))
    };
}
