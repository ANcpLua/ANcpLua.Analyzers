using System.Text;
using Basic.Reference.Assemblies;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ANcpLua.Analyzers.AnalyzerDocsGenerator;

/// <summary>
///     Shared helpers for the docs scenario projects:
///     - <see cref="ReplaceStackTrace" /> normalizes platform-specific paths and line endings so the
///       captured diagnostic text in <c>docs/*.md</c> is byte-identical across Linux/macOS/Windows CI.
///     - <see cref="RunAndCaptureDiagnosticAsync{TAnalyzer}" /> compiles a snippet, runs the analyzer,
///       and throws an exception whose Message holds <c>{id}: {message}</c> per diagnostic — the
///       <see cref="DocsGenerator" /> catches the exception and inlines those lines into the
///       <c>#### Failure messages</c> block (mirror of FluentAssertions's flow, where the
///       <c>XunitException.Message</c> from a failed assertion was the docs source of truth).
/// </summary>
public static partial class AnalyzerDocsUtils
{
    private const string UnixDirectorySeparator = "/";
    private const string UnixNewLine = "\n";

    /// <summary>
    ///     Rewrites OS-specific path separators and line endings to their Unix forms so that
    ///     macOS, Windows, and Linux CI all produce the same byte sequence in <c>docs/*.md</c>.
    /// </summary>
    public static string ReplaceStackTrace(string messageIncludingStacktrace) =>
        messageIncludingStacktrace
            .ReplaceOrdinal(Path.DirectorySeparatorChar.ToString(), UnixDirectorySeparator)!
            .ReplaceOrdinal(Environment.NewLine, UnixNewLine)!;

    /// <summary>
    ///     Compiles <paramref name="source" /> as a netstandard2.0 library against
    ///     <see cref="Net100.References" /> and runs <typeparamref name="TAnalyzer" /> over it.
    ///     If the analyzer reports any diagnostics, throws an exception whose Message holds one
    ///     <c>{id}: {message}</c> line per diagnostic. The DocsGenerator catches that exception and
    ///     inlines the lines into the markdown, so the docs always reflect the analyzer's
    ///     current diagnostic text rather than a hand-curated copy.
    ///
    ///     Compilation errors in <paramref name="source" /> are surfaced too — a typoed polyfill
    ///     that prevented the analyzer from firing would otherwise silently produce a docs file
    ///     with no diagnostic, which is the exact silent-failure mode that breaks docs/source drift
    ///     guarantees.
    /// </summary>
    public static async Task RunAndCaptureDiagnosticAsync<TAnalyzer>(string source)
        where TAnalyzer : DiagnosticAnalyzer, new()
    {
        var compilation = CSharpCompilation.Create(
            assemblyName: "ANcpLua.Analyzers.AnalyzerDocs.Scenario",
            syntaxTrees: new[] { CSharpSyntaxTree.ParseText(source) },
            references: Net100.References.All,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var compilationErrors = compilation.GetDiagnostics()
            .Where(static d => d.Severity is DiagnosticSeverity.Error)
            .ToImmutableArray();

        var withAnalyzers = compilation.WithAnalyzers(
            ImmutableArray.Create<DiagnosticAnalyzer>(new TAnalyzer()));

        var analyzerDiagnostics = await withAnalyzers.GetAnalyzerDiagnosticsAsync().ConfigureAwait(false);

        if (compilationErrors.IsEmpty && analyzerDiagnostics.IsDefaultOrEmpty)
            return;

        var sb = new StringBuilder();

        // Stable ordering: span start → severity (Error before Warning before Info) → Id → message.
        // Defensive against multi-rule analyzers, multiple diagnostics at one span, and any
        // future change to GetAnalyzerDiagnosticsAsync's internal ordering.
        var ordered = compilationErrors
            .Concat(analyzerDiagnostics)
            .OrderBy(static d => d.Location.SourceSpan.Start)
            .ThenByDescending(static d => d.Severity)
            .ThenBy(static d => d.Id, StringComparer.Ordinal)
            .ThenBy(static d => d.GetMessage(), StringComparer.Ordinal);

        foreach (var diagnostic in ordered)
            sb.AppendLine($"{diagnostic.Id}: {diagnostic.GetMessage()}");

        throw new InvalidOperationException(sb.ToString().TrimEnd());
    }
}
