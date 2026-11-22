using ANcpLua.Analyzers.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using AnalyzerTestBase = ANcpLua.Roslyn.Utilities.Testing.AnalyzerTest<ANcpLua.Analyzers.Analyzers.Al1603ToAl1605DiagnosticsAlignmentAnalyzer>;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL1603-AL1605: alignment between Descriptors.cs, diagnostics.md and AnalyzerReleases.*.md.
/// </summary>
/// <remarks>
///     Expected diagnostics check ID and location only; message texts are not pinned here.
/// </remarks>
public sealed partial class Al1603ToAl1605DiagnosticsAlignmentTests : AnalyzerTestBase {
    private const string EmptyCode = "public class C { }";

    private const string DescriptorsCs = """
                                         using Microsoft.CodeAnalysis;

                                         public static class Descriptors {
                                             public static readonly DiagnosticDescriptor Rule = new(
                                                 "XX0001", "Some title", "Some message", "Usage", DiagnosticSeverity.Warning, true);
                                         }
                                         """;

    private const string AlignedDocs = """
                                       # Diagnostics

                                       ### XX0001 - Some title

                                       **Severity:** Warning
                                       """;

    private const string AlignedRelease = """
                                          ### New Rules

                                          Rule ID | Category | Severity | Notes
                                          XX0001 | Usage | Warning | Descriptors
                                          """;

    [Fact]
    public Task ShouldNotReportWhenDocsAndReleasesAreAligned() =>
        VerifyAsync(
            EmptyCode,
            [
                ("Descriptors.cs", DescriptorsCs),
                ("diagnostics.md", AlignedDocs),
                ("AnalyzerReleases.Unshipped.md", AlignedRelease)
            ],
            Array.Empty<DiagnosticResult>());

    [Fact]
    public Task ShouldNotReportWithoutDescriptorsFile() =>
        VerifyAsync(
            EmptyCode,
            [("diagnostics.md", "# Diagnostics"), ("AnalyzerReleases.Unshipped.md", "### New Rules")],
            Array.Empty<DiagnosticResult>());

    [Fact]
    public Task ShouldNotReportWhenDocsAndReleaseFilesAreAbsent() =>
        VerifyAsync(
            EmptyCode,
            [("Descriptors.cs", DescriptorsCs)],
            Array.Empty<DiagnosticResult>());

    [Fact]
    public Task ShouldReportAl1603WhenDescriptorMissingFromDocs() {
        var expected = new DiagnosticResult("AL1603", DiagnosticSeverity.Warning)
            .WithLocation("Descriptors.cs", 1, 1);

        return VerifyAsync(
            EmptyCode,
            [("Descriptors.cs", DescriptorsCs), ("diagnostics.md", "# Diagnostics")],
            [expected]);
    }

    [Fact]
    public Task ShouldReportAl1604WhenDescriptorMissingFromReleases() {
        var expected = new DiagnosticResult("AL1604", DiagnosticSeverity.Warning)
            .WithLocation("Descriptors.cs", 1, 1);

        return VerifyAsync(
            EmptyCode,
            [("Descriptors.cs", DescriptorsCs), ("AnalyzerReleases.Shipped.md", "## Release 1.0")],
            [expected]);
    }

    [Fact]
    public Task ShouldReportAl1605WhenDocsTitleDiffers() {
        const string Docs = """
                            ### XX0001 - Another title

                            **Severity:** Warning
                            """;

        var expected = new DiagnosticResult("AL1605", DiagnosticSeverity.Warning)
            .WithLocation("Descriptors.cs", 1, 1);

        return VerifyAsync(
            EmptyCode,
            [("Descriptors.cs", DescriptorsCs), ("diagnostics.md", Docs)],
            [expected]);
    }

    [Fact]
    public Task ShouldReportAl1605WhenReleaseCategoryDiffers() {
        const string Release = """
                               ### New Rules

                               Rule ID | Category | Severity | Notes
                               XX0001 | Design | Warning | Descriptors
                               """;

        var expected = new DiagnosticResult("AL1605", DiagnosticSeverity.Warning)
            .WithLocation("Descriptors.cs", 1, 1);

        return VerifyAsync(
            EmptyCode,
            [("Descriptors.cs", DescriptorsCs), ("AnalyzerReleases.Unshipped.md", Release)],
            [expected]);
    }

    [Fact]
    public Task ShouldReportAl1605WhenDocsListUnknownId() {
        const string Docs = """
                            ### XX0001 - Some title

                            **Severity:** Warning

                            ### XX0002 - Orphan

                            **Severity:** Warning
                            """;

        var expected = new DiagnosticResult("AL1605", DiagnosticSeverity.Warning)
            .WithLocation("diagnostics.md", 1, 1);

        return VerifyAsync(
            EmptyCode,
            [("Descriptors.cs", DescriptorsCs), ("diagnostics.md", Docs)],
            [expected]);
    }
}
