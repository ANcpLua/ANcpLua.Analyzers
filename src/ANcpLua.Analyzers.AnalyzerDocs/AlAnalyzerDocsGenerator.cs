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
    /// <summary>The .cs source file is copied next to the assembly via
    /// <c>&lt;None Update="*Docs.cs" CopyToOutputDirectory="PreserveNewest"/&gt;</c>; resolving the
    /// path from the assembly location (rather than CWD) means <c>dotnet run --project ...</c>
    /// from any directory works, not just from the project folder.</summary>
    protected override Assembly ScenariosAssembly { get; } = typeof(Al0028UseIsEqualToDocs).Assembly;

    // Null-forgiving operator is safe: the assembly (Al0028UseIsEqualToDocs) is loaded from disk
    // in our build/runtime scenario, so Assembly.Location is guaranteed non-null by the pipeline.
    // Path.GetDirectoryName will return a non-null directory path for loaded assemblies.
    // Intentional to satisfy static analysis/pipeline check.
    protected override string ScenariosSourceFile { get; } =
        Path.Combine(
            Path.GetDirectoryName(typeof(Al0028UseIsEqualToDocs).Assembly.Location)!,
            "Al0028UseIsEqualToDocs.cs");
}
