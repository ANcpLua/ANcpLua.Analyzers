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
        var analyzerTypes = typeof(ALAnalyzer).Assembly.GetTypes()
            .Where(t => typeof(DiagnosticAnalyzer).IsAssignableFrom(t) && !t.IsAbstract);

        foreach (var type in analyzerTypes) {
            // Naming convention: AL{XXXX}.*Analyzer
            Assert.Matches(@"^AL\d{4}.*Analyzer$", type.Name);

            var analyzer = (DiagnosticAnalyzer)Activator.CreateInstance(type)!;
            Assert.NotEmpty(analyzer.SupportedDiagnostics);

            foreach (var descriptor in analyzer.SupportedDiagnostics) {
                // Diagnostic ID starts with AL
                Assert.StartsWith("AL", descriptor.Id);

                // Help link is required
                Assert.False(string.IsNullOrEmpty(descriptor.HelpLinkUri),
                    $"{descriptor.Id} missing HelpLinkUri");

                // Title is required
                Assert.False(string.IsNullOrWhiteSpace(descriptor.Title.ToString()),
                    $"{descriptor.Id} has empty Title");
            }
        }
    }
}
