using System.Reflection;
using System.Text.RegularExpressions;
using ANcpLua.Analyzers.DocsGenerator;
using ANcpLua.Roslyn.Utilities.Testing.MSBuild;
using Microsoft.CodeAnalysis;
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
            .Select(static t => (t, (DiagnosticAnalyzer)(Activator.CreateInstance(t)
                ?? throw new InvalidOperationException($"Cannot instantiate {t}"))));

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

    /// <summary>
    ///   Highest-leverage safety net for the hand-transcribed AL0xxx → AL1xxx rename map.
    ///   <c>--check</c> on a dev machine catches OUTPUT drift but only after someone runs
    ///   <c>./build.sh CheckDocs</c>; this test catches SOURCE drift (typo, duplicate,
    ///   leak into a sibling-package band) at CI on every PR. Mandatory.
    /// </summary>
    [Fact]
    public void AlIdMigrationCatalog_StructuralInvariants_Hold() {
        AlIdMigrationCatalog.Validate();
    }

    /// <summary>
    ///   The three <c>&lt;AlAnalysisMode&gt;</c> profiles ship inside the NuGet at
    ///   <c>buildTransitive/editorconfig/</c> and are appended to <c>$(EditorConfigFiles)</c> from a
    ///   consumer csproj. A sectioned config (<c>root = false</c> + <c>[*.{cs,vb}]</c>) has its globs
    ///   matched relative to the config file's OWN directory — inside ~/.nuget/packages/, which holds
    ///   no consumer source — so every section matches nothing and the profile is silently inert.
    ///   Shipped 2.1.1 had exactly that bug: AlAnalysisMode=Disabled left rules at warning.
    ///   Nothing else catches it: the file still reaches csc, and <c>--check</c> only compares the
    ///   generator against its own output, so both sides would agree on a broken shape.
    /// </summary>
    [Fact]
    public void EditorconfigProfiles_AreGlobalAnalyzerConfigs() {
        var descriptor = new DiagnosticDescriptor(
            "AL9999", "t", "m", "Usage", DiagnosticSeverity.Warning, true);

        foreach (var (path, content) in EditorconfigRenderer.EnumerateProfiles(
                     "/does-not-need-to-exist", [descriptor])) {
            var name = Path.GetFileName(path);
            content.Should().Contain("is_global = true",
                $"{name} must apply compilation-wide, not relative to its own directory");
            content.Should().MatchRegex(@"global_level = \d+",
                $"{name} needs an explicit global_level to order against the SDK's bundled AL config");
            content.Should().NotContain("root = false",
                $"{name} must not be a sectioned config — that scopes it to the NuGet folder");
            content.Should().NotContain("[*",
                $"{name} must not carry a section header — globs would match nothing");
        }
    }

    /// <summary>
    ///   An analyzer sees an MSBuild property only when a <c>CompilerVisibleProperty</c> item names it,
    ///   and NuGet imports only <c>buildTransitive/</c> from a package that has that folder. Up to 2.1.6
    ///   the items sat in a never-imported <c>build/</c> copy, so AL1011, AL1406/AL1407 and AL1601 read
    ///   empty values in every consumer while tests that inject the keys through editorconfig passed.
    /// </summary>
    [Fact]
#pragma warning disable RS1035 // test code reading the repository, not analyzer code running in the compiler
    public void EveryBuildPropertyReadByAnAnalyzer_IsCompilerVisibleInBuildTransitiveProps() {
        string root = RepositoryRoot.Locate("ANcpLua.Analyzers.slnx");
        var analyzerSource = Path.Combine(root, "src", "ANcpLua.Analyzers");
        var props = File.ReadAllText(Path.Combine(analyzerSource, "buildTransitive", "ANcpLua.Analyzers.props"));

        var readProperties = Directory.EnumerateFiles(analyzerSource, "*.cs", SearchOption.AllDirectories)
            .SelectMany(static file => BuildPropertyKey().Matches(File.ReadAllText(file)))
            .Select(static match => match.Groups[1].Value)
            .ToHashSet(StringComparer.Ordinal);

        readProperties.Should().NotBeEmpty();
        foreach (var property in readProperties) {
            props.Should().Contain($"<CompilerVisibleProperty Include=\"{property}\"",
                $"an analyzer reads build_property.{property}");
        }

        Directory.Exists(Path.Combine(analyzerSource, "build")).Should().BeFalse(
            "NuGet ignores build/ when buildTransitive/ exists; keep consumer props in buildTransitive/ only");
    }
#pragma warning restore RS1035

    [GeneratedRegex("\"build_property\\.([A-Za-z]+)\"")]
    private static partial Regex BuildPropertyKey();

    [Fact]
    public void AllAnalyzersDeclareDiagnosticIdConsts() {
        const BindingFlags AnyStatic = BindingFlags.Public | BindingFlags.NonPublic
                                                           | BindingFlags.Static | BindingFlags.FlattenHierarchy;
        foreach (var (type, analyzer) in GetAllAnalyzers()) {
            var supportedIds = analyzer.SupportedDiagnostics.Select(static d => d.Id).ToHashSet();

            var constValues = type.GetFields(AnyStatic)
                .Where(static f => f is { IsLiteral: true, FieldType.Name: "String" }
                                   && f.Name.StartsWith("DiagnosticId", StringComparison.Ordinal))
                .Select(static f => f.GetRawConstantValue() as string
                    ?? throw new InvalidOperationException($"DiagnosticId const {f.Name} is not a string"))
                .ToHashSet();

            foreach (var id in supportedIds) {
                constValues.Should().Contain(id,
                    $"{type.Name} supports {id} but has no const DiagnosticId* = \"{id}\"");
            }
        }
    }
}
