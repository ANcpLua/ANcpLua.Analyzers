using System.Reflection;
using ANcpLua.Analyzers.AnalyzerDocsGenerator;

namespace ANcpLua.Analyzers.AnalyzerDocs;

/// <summary>
///     Concrete <see cref="DocsGenerator" /> for the AL00xx (Roslyn analyzer) scenario file.
///     Pinned to <see cref="Al0028UseIsEqualToDocs" /> as the proof-of-architecture entry; add a
///     sibling subclass per additional scenario class to broaden coverage.
/// </summary>
public sealed partial class AlAnalyzerDocsGenerator : DocsGenerator
{
    protected override Assembly ScenariosAssembly { get; } = typeof(Al0028UseIsEqualToDocs).Assembly;

    protected override string ScenariosSourceFile { get; } =
        Path.Combine(Environment.CurrentDirectory, "Al0028UseIsEqualToDocs.cs");
}
