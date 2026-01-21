using ANcpLua.Analyzers.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using AnalyzerTestBase = ANcpLua.Roslyn.Utilities.Testing.AnalyzerTest<ANcpLua.Analyzers.Analyzers.Al0019UndefinedVersionVariableAnalyzer>;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL0019: Undefined version variable.
///     Warns when $(VariableName) is used in Directory.Packages.props but not defined in Version.props.
/// </summary>
public sealed partial class Al0019AnalyzerTests : AnalyzerTestBase {
    private const string EmptyCode = "public class C { }";

    [Fact]
    public Task ShouldReportWhenVariableNotDefined() {
        const string VersionProps = """
                                    <Project>
                                        <PropertyGroup>
                                            <RoslynVersion>4.0.0</RoslynVersion>
                                        </PropertyGroup>
                                    </Project>
                                    """;

        const string DirectoryPackagesProps = """
                                              <Project>
                                                  <ItemGroup>
                                                      <PackageVersion Include="SomePackage" Version="$(UndefinedVersion)" />
                                                  </ItemGroup>
                                              </Project>
                                              """;

        var expected = new DiagnosticResult(Al0019UndefinedVersionVariableAnalyzer.DiagnosticId, DiagnosticSeverity.Warning)
            .WithLocation("Directory.Packages.props", 4, 56)
            .WithArguments("SomePackage", "UndefinedVersion");

        return VerifyAsync(
            EmptyCode,
            [("Version.props", VersionProps), ("Directory.Packages.props", DirectoryPackagesProps)],
            [expected]);
    }

    [Fact]
    public Task ShouldNotReportWhenVariableIsDefined() {
        const string VersionProps = """
                                    <Project>
                                        <PropertyGroup>
                                            <RoslynVersion>4.0.0</RoslynVersion>
                                        </PropertyGroup>
                                    </Project>
                                    """;

        const string DirectoryPackagesProps = """
                                              <Project>
                                                  <ItemGroup>
                                                      <PackageVersion Include="Microsoft.CodeAnalysis.CSharp" Version="$(RoslynVersion)" />
                                                  </ItemGroup>
                                              </Project>
                                              """;

        return VerifyAsync(
            EmptyCode,
            [("Version.props", VersionProps), ("Directory.Packages.props", DirectoryPackagesProps)]);
    }

    [Fact]
    public Task ShouldNotReportForHardcodedVersions() {
        const string VersionProps = """
                                    <Project>
                                        <PropertyGroup>
                                            <RoslynVersion>4.0.0</RoslynVersion>
                                        </PropertyGroup>
                                    </Project>
                                    """;

        const string DirectoryPackagesProps = """
                                              <Project>
                                                  <ItemGroup>
                                                      <PackageVersion Include="SomePackage" Version="1.0.0" />
                                                  </ItemGroup>
                                              </Project>
                                              """;

        // AL0017 handles hardcoded versions, not AL0019
        return VerifyAsync(
            EmptyCode,
            [("Version.props", VersionProps), ("Directory.Packages.props", DirectoryPackagesProps)]);
    }

    [Fact]
    public Task ShouldReportMultipleUndefinedVariables() {
        const string VersionProps = """
                                    <Project>
                                        <PropertyGroup>
                                            <RoslynVersion>4.0.0</RoslynVersion>
                                        </PropertyGroup>
                                    </Project>
                                    """;

        const string DirectoryPackagesProps = """
                                              <Project>
                                                  <ItemGroup>
                                                      <PackageVersion Include="Package1" Version="$(Undefined1)" />
                                                      <PackageVersion Include="Package2" Version="$(Undefined2)" />
                                                  </ItemGroup>
                                              </Project>
                                              """;

        var expected1 = new DiagnosticResult(Al0019UndefinedVersionVariableAnalyzer.DiagnosticId, DiagnosticSeverity.Warning)
            .WithLocation("Directory.Packages.props", 4, 51)
            .WithArguments("Package1", "Undefined1");

        var expected2 = new DiagnosticResult(Al0019UndefinedVersionVariableAnalyzer.DiagnosticId, DiagnosticSeverity.Warning)
            .WithLocation("Directory.Packages.props", 5, 51)
            .WithArguments("Package2", "Undefined2");

        return VerifyAsync(
            EmptyCode,
            [("Version.props", VersionProps), ("Directory.Packages.props", DirectoryPackagesProps)],
            [expected1, expected2]);
    }

    [Fact]
    public Task ShouldHandleCaseInsensitiveVariableNames() {
        const string VersionProps = """
                                    <Project>
                                        <PropertyGroup>
                                            <RoslynVersion>4.0.0</RoslynVersion>
                                        </PropertyGroup>
                                    </Project>
                                    """;

        const string DirectoryPackagesProps = """
                                              <Project>
                                                  <ItemGroup>
                                                      <PackageVersion Include="Microsoft.CodeAnalysis.CSharp" Version="$(roslynversion)" />
                                                  </ItemGroup>
                                              </Project>
                                              """;

        // MSBuild properties are case-insensitive
        return VerifyAsync(
            EmptyCode,
            [("Version.props", VersionProps), ("Directory.Packages.props", DirectoryPackagesProps)]);
    }

    [Fact]
    public Task ShouldHandleMissingVersionProps() {
        const string DirectoryPackagesProps = """
                                              <Project>
                                                  <ItemGroup>
                                                      <PackageVersion Include="SomePackage" Version="$(AnyVariable)" />
                                                  </ItemGroup>
                                              </Project>
                                              """;

        // When Version.props is missing, all variables are undefined
        var expected = new DiagnosticResult(Al0019UndefinedVersionVariableAnalyzer.DiagnosticId, DiagnosticSeverity.Warning)
            .WithLocation("Directory.Packages.props", 4, 52)
            .WithArguments("SomePackage", "AnyVariable");

        return VerifyAsync(
            EmptyCode,
            [("Directory.Packages.props", DirectoryPackagesProps)],
            [expected]);
    }

    [Fact]
    public Task ShouldHandleNestedPropertyGroups() {
        const string VersionProps = """
                                    <Project>
                                        <PropertyGroup Label="Roslyn">
                                            <RoslynVersion>4.0.0</RoslynVersion>
                                        </PropertyGroup>
                                        <PropertyGroup Label="Testing">
                                            <XunitVersion>2.0.0</XunitVersion>
                                        </PropertyGroup>
                                    </Project>
                                    """;

        const string DirectoryPackagesProps = """
                                              <Project>
                                                  <ItemGroup>
                                                      <PackageVersion Include="Microsoft.CodeAnalysis.CSharp" Version="$(RoslynVersion)" />
                                                      <PackageVersion Include="xunit" Version="$(XunitVersion)" />
                                                  </ItemGroup>
                                              </Project>
                                              """;

        return VerifyAsync(
            EmptyCode,
            [("Version.props", VersionProps), ("Directory.Packages.props", DirectoryPackagesProps)]);
    }

    [Fact]
    public Task ShouldNotReportForPackagesPropsOtherFiles() {
        const string OtherPropsFile = """
                                      <Project>
                                          <ItemGroup>
                                              <PackageVersion Include="SomePackage" Version="$(UndefinedVersion)" />
                                          </ItemGroup>
                                      </Project>
                                      """;

        // Only Directory.Packages.props is analyzed
        return VerifyAsync(
            EmptyCode,
            [("SomeOther.props", OtherPropsFile)]);
    }
}
