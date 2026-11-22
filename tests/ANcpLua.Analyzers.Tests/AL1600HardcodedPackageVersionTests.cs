using ANcpLua.Analyzers.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using AnalyzerTestBase = ANcpLua.Roslyn.Utilities.Testing.AnalyzerTest<ANcpLua.Analyzers.Analyzers.Al1600HardcodedPackageVersionAnalyzer>;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL1600: Hardcoded package version in Directory.Packages.props.
/// </summary>
/// <remarks>
///     AL1600 parses the props file without line info, so its diagnostics currently carry no
///     location. These tests pin that behaviour.
/// </remarks>
public sealed partial class Al1600HardcodedPackageVersionTests : AnalyzerTestBase {
    private const string EmptyCode = "public class C { }";

    [Fact]
    public Task ShouldReportHardcodedVersionWithMappedVariable() {
        const string DirectoryPackagesProps = """
                                              <Project>
                                                  <ItemGroup>
                                                      <PackageVersion Include="xunit.v3" Version="3.0.0" />
                                                  </ItemGroup>
                                              </Project>
                                              """;

        var expected = new DiagnosticResult("AL1600", DiagnosticSeverity.Warning)
            .WithArguments("xunit.v3", "3.0.0", "XunitV3Version");

        return VerifyAsync(
            EmptyCode,
            [("Directory.Packages.props", DirectoryPackagesProps)],
            [expected]);
    }

    [Fact]
    public Task ShouldReportHardcodedVersionWithDerivedVariable() {
        const string DirectoryPackagesProps = """
                                              <Project>
                                                  <ItemGroup>
                                                      <PackageVersion Include="Contoso.Widgets-Core_X" Version="1.2.3" />
                                                  </ItemGroup>
                                              </Project>
                                              """;

        var expected = new DiagnosticResult("AL1600", DiagnosticSeverity.Warning)
            .WithArguments("Contoso.Widgets-Core_X", "1.2.3", "ContosoWidgetsCoreXVersion");

        return VerifyAsync(
            EmptyCode,
            [("Directory.Packages.props", DirectoryPackagesProps)],
            [expected]);
    }

    [Fact]
    public Task ShouldNotReportVariableVersion() {
        const string DirectoryPackagesProps = """
                                              <Project>
                                                  <ItemGroup>
                                                      <PackageVersion Include="xunit.v3" Version="$(XunitV3Version)" />
                                                  </ItemGroup>
                                              </Project>
                                              """;

        return VerifyAsync(
            EmptyCode,
            [("Directory.Packages.props", DirectoryPackagesProps)],
            Array.Empty<DiagnosticResult>());
    }

    [Fact]
    public Task ShouldNotReportInCpmNativeMode() {
        const string DirectoryPackagesProps = """
                                              <Project>
                                                  <PropertyGroup>
                                                      <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
                                                  </PropertyGroup>
                                                  <ItemGroup>
                                                      <PackageVersion Include="xunit.v3" Version="3.0.0" />
                                                  </ItemGroup>
                                              </Project>
                                              """;

        return VerifyAsync(
            EmptyCode,
            [("Directory.Packages.props", DirectoryPackagesProps)],
            Array.Empty<DiagnosticResult>());
    }

    [Fact]
    public Task ShouldNotReportOutsideDirectoryPackagesProps() {
        const string OtherPropsFile = """
                                      <Project>
                                          <ItemGroup>
                                              <PackageVersion Include="xunit.v3" Version="3.0.0" />
                                          </ItemGroup>
                                      </Project>
                                      """;

        return VerifyAsync(
            EmptyCode,
            [("Directory.Build.props", OtherPropsFile)],
            Array.Empty<DiagnosticResult>());
    }

    [Fact]
    public Task ShouldNotReportPackageVersionWithoutVersionAttribute() {
        const string DirectoryPackagesProps = """
                                              <Project>
                                                  <ItemGroup>
                                                      <PackageVersion Include="xunit.v3" />
                                                  </ItemGroup>
                                              </Project>
                                              """;

        return VerifyAsync(
            EmptyCode,
            [("Directory.Packages.props", DirectoryPackagesProps)],
            Array.Empty<DiagnosticResult>());
    }
}
