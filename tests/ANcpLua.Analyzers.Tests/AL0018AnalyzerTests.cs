using ANcpLua.Analyzers.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using AnalyzerTestBase = ANcpLua.Roslyn.Utilities.Testing.AnalyzerTest<ANcpLua.Analyzers.Analyzers.Al0018VersionPropsNotImportedAnalyzer>;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL0018: Version.props not imported.
///     Warns when Directory.Build.props doesn't import Version.props.
/// </summary>
public sealed partial class Al0018AnalyzerTests : AnalyzerTestBase {
    private const string EmptyCode = "public class C { }";

    [Fact]
    public Task ShouldReportWhenVersionPropsNotImported() {
        const string DirectoryBuildProps = """
                                           <Project>
                                               <PropertyGroup>
                                                   <SomeProperty>Value</SomeProperty>
                                               </PropertyGroup>
                                           </Project>
                                           """;

        var expected = new DiagnosticResult(Al0018VersionPropsNotImportedAnalyzer.DiagnosticId, DiagnosticSeverity.Warning)
            .WithLocation("Directory.Build.props", 1, 1);

        return VerifyAsync(
            EmptyCode,
            [("Directory.Build.props", DirectoryBuildProps)],
            [expected]);
    }

    [Fact]
    public Task ShouldNotReportWhenVersionPropsImported() {
        const string DirectoryBuildProps = """
                                           <Project>
                                               <Import Project="Version.props" Condition="Exists('Version.props')" />
                                               <PropertyGroup>
                                                   <SomeProperty>Value</SomeProperty>
                                               </PropertyGroup>
                                           </Project>
                                           """;

        return VerifyAsync(
            EmptyCode,
            [("Directory.Build.props", DirectoryBuildProps)]);
    }

    [Fact]
    public Task ShouldNotReportWhenVersionPropsImportedWithPath() {
        const string DirectoryBuildProps = """
                                           <Project>
                                               <Import Project="../build/Version.props" />
                                           </Project>
                                           """;

        return VerifyAsync(
            EmptyCode,
            [("Directory.Build.props", DirectoryBuildProps)]);
    }

    [Fact]
    public Task ShouldNotReportForOtherPropsFiles() {
        const string OtherPropsFile = """
                                      <Project>
                                          <PropertyGroup>
                                              <SomeProperty>Value</SomeProperty>
                                          </PropertyGroup>
                                      </Project>
                                      """;

        return VerifyAsync(
            EmptyCode,
            [("SomeOther.props", OtherPropsFile)]);
    }
}
