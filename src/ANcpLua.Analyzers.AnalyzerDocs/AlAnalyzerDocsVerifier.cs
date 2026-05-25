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
    // Null-forgiving operator is safe: Assembly.Location is guaranteed to include a directory
    // for loaded assemblies in this runtime/hosting context. The assembly is loaded from disk,
    // ensuring Path.GetDirectoryName returns a non-null directory path.
    protected override string ScenariosSourceFile { get; } =
        Path.Combine(
            Path.GetDirectoryName(typeof(Al1200UseIsEqualToDocs).Assembly.Location)!,
            "Al1200UseIsEqualToDocs.cs");
}
