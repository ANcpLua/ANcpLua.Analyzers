using System.Reflection;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Convention validation tests that run against ALL analyzers in the assembly.
///     Catches naming violations, missing help links, and other convention issues at build time.
/// </summary>
public sealed partial class AnalyzerConventionTests {
    private static IEnumerable<(Type Type, DiagnosticAnalyzer Instance)> GetAllAnalyzers() =>
        typeof(AlAnalyzer).Assembly.GetTypes()
            .Where(static t => typeof(DiagnosticAnalyzer).IsAssignableFrom(t) && !t.IsAbstract)
            .Select(static t => (t, (DiagnosticAnalyzer)Activator.CreateInstance(t)!))
            .ToList();

    [Fact]
    public void AllAnalyzersFollowNamingConvention() {
        foreach (var (type, analyzer) in GetAllAnalyzers()) {
            type.Name.Should().MatchRegex(@"^Al\d{4}.*Analyzer$");
            analyzer.SupportedDiagnostics.Should().NotBeEmpty();

            foreach (var descriptor in analyzer.SupportedDiagnostics) {
                descriptor.Id.Should().StartWith("AL");
                descriptor.HelpLinkUri.Should().NotBeNullOrEmpty($"{descriptor.Id} missing HelpLinkUri");
                descriptor.Title.ToString().Should().NotBeNullOrWhiteSpace($"{descriptor.Id} has empty Title");
            }
        }
    }

    [Fact]
    public void AllDiagnosticIdsAreUnique() {
        var allIds = GetAllAnalyzers()
            .SelectMany(static a => a.Instance.SupportedDiagnostics.Select(static d => d.Id))
            .ToList();

        allIds.Should().OnlyHaveUniqueItems("each diagnostic ID must be unique across all analyzers");
    }

    [Fact]
    public void AllDiagnosticIdsMatchExpectedFormat() {
        foreach (var (_, analyzer) in GetAllAnalyzers()) {
            foreach (var descriptor in analyzer.SupportedDiagnostics) {
                descriptor.Id.Should().MatchRegex(@"^AL\d{4}$",
                    $"diagnostic ID '{descriptor.Id}' must match AL followed by exactly 4 digits");
            }
        }
    }

    [Fact]
    public void AllAnalyzersExposePublicDiagnosticIdConsts() {
        foreach (var (type, analyzer) in GetAllAnalyzers()) {
            var supportedIds = analyzer.SupportedDiagnostics.Select(static d => d.Id).ToHashSet();

            var constFields = type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                .Where(static f => f is { IsLiteral: true, FieldType.Name: "String" }
                                   && f.Name.StartsWith("DiagnosticId", StringComparison.Ordinal))
                .ToList();

            var constValues = constFields.Select(static f => (string)f.GetRawConstantValue()!).ToHashSet();

            foreach (var id in supportedIds) {
                constValues.Should().Contain(id,
                    $"{type.Name} supports {id} but has no public const DiagnosticId* = \"{id}\"");
            }
        }
    }
}
