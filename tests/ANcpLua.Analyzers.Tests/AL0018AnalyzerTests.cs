using ANcpLua.Analyzers.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL0018: Version.props not imported.
///     Warns when Directory.Build.props doesn't import Version.props.
/// </summary>
public sealed partial class Al0018AnalyzerTests {
    private const string EmptyCode = "public class C { }";

    [Fact]
    public async Task ShouldReportWhenVersionPropsNotImported() {
        var directoryBuildProps = """
            <Project>
                <PropertyGroup>
                    <SomeProperty>Value</SomeProperty>
                </PropertyGroup>
            </Project>
            """;

        var test = new CSharpAnalyzerTest<Al0018VersionPropsNotImportedAnalyzer, DefaultVerifier> {
            TestCode = EmptyCode,
            TestState = {
                AdditionalFiles = {
                    ("Directory.Build.props", directoryBuildProps)
                }
            }
        };

        test.ExpectedDiagnostics.Add(
            new DiagnosticResult(Al0018VersionPropsNotImportedAnalyzer.DiagnosticId, DiagnosticSeverity.Warning)
                .WithLocation("Directory.Build.props", 1, 1));

        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ShouldNotReportWhenVersionPropsImported() {
        var directoryBuildProps = """
            <Project>
                <Import Project="Version.props" Condition="Exists('Version.props')" />
                <PropertyGroup>
                    <SomeProperty>Value</SomeProperty>
                </PropertyGroup>
            </Project>
            """;

        var test = new CSharpAnalyzerTest<Al0018VersionPropsNotImportedAnalyzer, DefaultVerifier> {
            TestCode = EmptyCode,
            TestState = {
                AdditionalFiles = {
                    ("Directory.Build.props", directoryBuildProps)
                }
            }
        };

        // No diagnostics expected
        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ShouldNotReportWhenVersionPropsImportedWithPath() {
        var directoryBuildProps = """
            <Project>
                <Import Project="../build/Version.props" />
            </Project>
            """;

        var test = new CSharpAnalyzerTest<Al0018VersionPropsNotImportedAnalyzer, DefaultVerifier> {
            TestCode = EmptyCode,
            TestState = {
                AdditionalFiles = {
                    ("Directory.Build.props", directoryBuildProps)
                }
            }
        };

        // No diagnostics expected - Version.props is in the path
        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ShouldNotReportForOtherPropsFiles() {
        var otherPropsFile = """
            <Project>
                <PropertyGroup>
                    <SomeProperty>Value</SomeProperty>
                </PropertyGroup>
            </Project>
            """;

        var test = new CSharpAnalyzerTest<Al0018VersionPropsNotImportedAnalyzer, DefaultVerifier> {
            TestCode = EmptyCode,
            TestState = {
                AdditionalFiles = {
                    ("SomeOther.props", otherPropsFile)
                }
            }
        };

        // No diagnostics expected - only Directory.Build.props is checked
        await test.RunAsync(TestContext.Current.CancellationToken);
    }
}
