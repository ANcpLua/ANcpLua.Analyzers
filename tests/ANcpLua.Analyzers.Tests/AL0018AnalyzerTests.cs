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
    public Task ShouldReportWhenVersionPropsNotImported() {
        const string DirectoryBuildProps = """
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
                    ("Directory.Build.props", DirectoryBuildProps)
                }
            }
        };

        test.ExpectedDiagnostics.Add(
            new DiagnosticResult(Al0018VersionPropsNotImportedAnalyzer.DiagnosticId, DiagnosticSeverity.Warning)
                .WithLocation("Directory.Build.props", 1, 1));

        return test.RunAsync(TestContext.Current.CancellationToken);
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

        var test = new CSharpAnalyzerTest<Al0018VersionPropsNotImportedAnalyzer, DefaultVerifier> {
            TestCode = EmptyCode,
            TestState = {
                AdditionalFiles = {
                    ("Directory.Build.props", DirectoryBuildProps)
                }
            }
        };

        // No diagnostics expected
        return test.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public Task ShouldNotReportWhenVersionPropsImportedWithPath() {
        const string DirectoryBuildProps = """
                                           <Project>
                                               <Import Project="../build/Version.props" />
                                           </Project>
                                           """;

        var test = new CSharpAnalyzerTest<Al0018VersionPropsNotImportedAnalyzer, DefaultVerifier> {
            TestCode = EmptyCode,
            TestState = {
                AdditionalFiles = {
                    ("Directory.Build.props", DirectoryBuildProps)
                }
            }
        };

        // No diagnostics expected - Version.props is in the path
        return test.RunAsync(TestContext.Current.CancellationToken);
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

        var test = new CSharpAnalyzerTest<Al0018VersionPropsNotImportedAnalyzer, DefaultVerifier> {
            TestCode = EmptyCode,
            TestState = {
                AdditionalFiles = {
                    ("SomeOther.props", OtherPropsFile)
                }
            }
        };

        // No diagnostics expected - only Directory.Build.props is checked
        return test.RunAsync(TestContext.Current.CancellationToken);
    }
}
