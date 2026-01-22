using ANcpLua.Analyzers.Core;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Convention validation tests that run against ALL analyzers in the assembly.
///     Catches naming violations, missing help links, and other convention issues at build time.
/// </summary>
public sealed class AnalyzerConventionTests {
    [Fact]
    public void AllAnalyzersFollowConventions() {
        var analyzerTypes = typeof(AlAnalyzer).Assembly.GetTypes()
            .Where(static t => typeof(DiagnosticAnalyzer).IsAssignableFrom(t) && !t.IsAbstract);

        foreach (var type in analyzerTypes) {
            type.Name.Should().MatchRegex(@"^Al\d{4}.*Analyzer$");

            var analyzer = (DiagnosticAnalyzer)Activator.CreateInstance(type)!;
            analyzer.SupportedDiagnostics.Should().NotBeEmpty();

            foreach (var descriptor in analyzer.SupportedDiagnostics) {
                descriptor.Id.Should().StartWith("AL");

                descriptor.HelpLinkUri.Should().NotBeNullOrEmpty($"{descriptor.Id} missing HelpLinkUri");

                descriptor.Title.ToString().Should().NotBeNullOrWhiteSpace($"{descriptor.Id} has empty Title");
            }
        }
    }
}
