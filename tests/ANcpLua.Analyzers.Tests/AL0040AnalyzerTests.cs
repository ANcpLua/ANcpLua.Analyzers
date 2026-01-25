using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL0040: Use attribute argument extraction extensions.
///     Note: This analyzer targets Roslyn-aware codebases (source generators, analyzers).
///     These tests are skipped because Microsoft.CodeAnalysis types aren't in default test references.
///     The analyzer is validated via the SourceGenerators3 sample project.
/// </summary>
public sealed partial class Al0040AnalyzerTests : AnalyzerTest<Al0040UseAttributeExtensionsAnalyzer> {
    // AL0040 analyzer requires Microsoft.CodeAnalysis types (AttributeData, TypedConstant).
    // These are not available in the default test framework reference assemblies.
    // The analyzer is validated through real-world usage in Roslyn projects like SourceGenerators3.
}
