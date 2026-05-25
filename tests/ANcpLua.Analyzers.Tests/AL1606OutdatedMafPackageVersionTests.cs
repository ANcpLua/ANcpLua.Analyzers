using System.Reflection;
using ANcpLua.Analyzers.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using AnalyzerTestBase = ANcpLua.Roslyn.Utilities.Testing.AnalyzerTest<ANcpLua.Analyzers.Analyzers.Al1606OutdatedMafPackageVersionAnalyzer>;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL1606: Outdated MAF ecosystem package version.
/// </summary>
public sealed partial class Al1606OutdatedMafPackageVersionTests : AnalyzerTestBase {
    private const string EmptyCode = "public class C { }";

    private static readonly MethodInfo s_isVersionBelowMinimumMethod =
        typeof(Al1606OutdatedMafPackageVersionAnalyzer)
            .GetMethod("IsVersionBelowMinimum", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("Cannot resolve Al1606OutdatedMafPackageVersionAnalyzer.IsVersionBelowMinimum.");

    [Fact]
    public Task ShouldReportOutdatedRcVersion() {
        const string directoryPackagesProps = """
<Project>
<ItemGroup>
<PackageVersion Include="Microsoft.Agents.AI.Anthropic" Version="1.0.0-rc2" />
</ItemGroup>
</Project>
""";

        var expected = new DiagnosticResult("AL1606", DiagnosticSeverity.Warning)
            .WithLocation("Directory.Packages.props", 3, 57)
            .WithArguments("Microsoft.Agents.AI.Anthropic", "1.0.0-rc2", "1.0.0-rc6");

        return VerifyAsync(EmptyCode, [("Directory.Packages.props", directoryPackagesProps)], [expected]);
    }

    [Fact]
    public Task ShouldNotReportWhenVersionMeetsRequirement() {
        const string directoryPackagesProps = """
<Project>
<ItemGroup>
<PackageVersion Include="Microsoft.Agents.AI.Anthropic" Version="1.0.0-rc6" />
</ItemGroup>
</Project>
""";

        return VerifyAsync(
            EmptyCode,
            [("Directory.Packages.props", directoryPackagesProps)],
            Array.Empty<DiagnosticResult>());
    }

    [Fact]
    public void ShouldCompareNumericPrereleaseSegmentsSemantically() {
        var lower = (bool)s_isVersionBelowMinimumMethod.Invoke(null, ["1.0.0-preview.2", "1.0.0-preview.10"])!;
        var greater = (bool)s_isVersionBelowMinimumMethod.Invoke(null, ["1.0.0-preview.10", "1.0.0-preview.2"])!;

        Assert.True(lower);
        Assert.False(greater);
    }
}
