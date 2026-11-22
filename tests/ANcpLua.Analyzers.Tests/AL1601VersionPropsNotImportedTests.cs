using ANcpLua.Analyzers.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using AnalyzerTestBase = ANcpLua.Roslyn.Utilities.Testing.AnalyzerTest<ANcpLua.Analyzers.Analyzers.Al1601VersionPropsNotImportedAnalyzer>;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL1601: Version.props not imported.
///     Warns when Directory.Build.props doesn't import Version.props.
/// </summary>
public sealed partial class Al1601VersionPropsNotImportedTests : AnalyzerTestBase {
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

        var expected = new DiagnosticResult("AL1601", DiagnosticSeverity.Warning)
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

        // Explicit empty diagnostics array to resolve overload ambiguity
        return VerifyAsync(
            EmptyCode,
            [("Directory.Build.props", DirectoryBuildProps)],
            Array.Empty<DiagnosticResult>());
    }

    [Fact]
    public Task ShouldNotReportWhenVersionPropsImportedWithPath() {
        const string DirectoryBuildProps = """
                                           <Project>
                                               <Import Project="../build/Version.props" />
                                           </Project>
                                           """;

        // Explicit empty diagnostics array to resolve overload ambiguity
        return VerifyAsync(
            EmptyCode,
            [("Directory.Build.props", DirectoryBuildProps)],
            Array.Empty<DiagnosticResult>());
    }

    [Fact]
    public Task ShouldNotReportWhenDirectoryPackagesPropsImportsVersionProps() {
        const string DirectoryBuildProps = """
                                           <Project>
                                               <PropertyGroup>
                                                   <SomeProperty>Value</SomeProperty>
                                               </PropertyGroup>
                                           </Project>
                                           """;

        const string DirectoryPackagesProps = """
                                              <Project>
                                                  <Import Project="Version.props" />
                                              </Project>
                                              """;

        return VerifyAsync(
            EmptyCode,
            [("Directory.Build.props", DirectoryBuildProps), ("Directory.Packages.props", DirectoryPackagesProps)],
            Array.Empty<DiagnosticResult>());
    }

    [Fact]
    public Task ShouldReportWhenDirectoryPackagesPropsHasNoImport() {
        const string DirectoryBuildProps = """
                                           <Project>
                                               <PropertyGroup>
                                                   <SomeProperty>Value</SomeProperty>
                                               </PropertyGroup>
                                           </Project>
                                           """;

        const string DirectoryPackagesProps = """
                                              <Project>
                                                  <ItemGroup>
                                                      <PackageVersion Include="SomePackage" Version="$(SomePackageVersion)" />
                                                  </ItemGroup>
                                              </Project>
                                              """;

        var expected = new DiagnosticResult("AL1601", DiagnosticSeverity.Warning)
            .WithLocation("Directory.Build.props", 1, 1);

        return VerifyAsync(
            EmptyCode,
            [("Directory.Build.props", DirectoryBuildProps), ("Directory.Packages.props", DirectoryPackagesProps)],
            [expected]);
    }

    [Fact]
    public Task ShouldNotReportInCpmNativeMode() {
        const string DirectoryBuildProps = """
                                           <Project>
                                               <PropertyGroup>
                                                   <SomeProperty>Value</SomeProperty>
                                               </PropertyGroup>
                                           </Project>
                                           """;

        const string DirectoryPackagesProps = """
                                              <Project>
                                                  <PropertyGroup>
                                                      <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
                                                  </PropertyGroup>
                                              </Project>
                                              """;

        return VerifyAsync(
            EmptyCode,
            [("Directory.Build.props", DirectoryBuildProps), ("Directory.Packages.props", DirectoryPackagesProps)],
            Array.Empty<DiagnosticResult>());
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

        // Explicit empty diagnostics array to resolve overload ambiguity
        return VerifyAsync(
            EmptyCode,
            [("SomeOther.props", OtherPropsFile)],
            Array.Empty<DiagnosticResult>());
    }
}
