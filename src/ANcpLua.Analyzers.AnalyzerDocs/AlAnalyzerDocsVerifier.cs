using ANcpLua.Analyzers.AnalyzerDocsGenerator;

namespace ANcpLua.Analyzers.AnalyzerDocs;

/// <summary>
///     Concrete <see cref="DocsVerifier" /> pinned to the same scenarios source file as
///     <see cref="AlAnalyzerDocsGenerator" />. The byte-identical guard lives in
///     <c>scripts/generate-docs.ps1 -ValidateNoChanges</c> (CI runs that script).
/// </summary>
public sealed partial class AlAnalyzerDocsVerifier : DocsVerifier
{
    /// <summary>Resolved from the assembly location (matches <see cref="AlAnalyzerDocsGenerator" />)
    /// so the path is stable regardless of CWD.</summary>
    protected override string ScenariosSourceFile { get; } =
        Path.Combine(
            Path.GetDirectoryName(typeof(Al0028UseIsEqualToDocs).Assembly.Location)!,
            "Al0028UseIsEqualToDocs.cs");
}
