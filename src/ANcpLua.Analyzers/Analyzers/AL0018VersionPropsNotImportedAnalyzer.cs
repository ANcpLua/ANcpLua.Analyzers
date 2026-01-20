using ANcpLua.Analyzers.Core;
using System.Xml.Linq;

namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0018: Detects when Version.props is not imported in Directory.Build.props.
///     Version.props should be imported for centralized version management.
/// </summary>
/// <remarks>
///     <para>
///         Central Package Management (CPM) works best when versions are defined as MSBuild
///         properties in a separate file (typically Version.props). This file should be
///         imported in Directory.Build.props so all projects in the solution can reference
///         the version variables.
///     </para>
///     <para>
///         The analyzer examines Directory.Build.props files added as additional files
///         to the compilation. It flags files that don't contain an Import element
///         referencing Version.props.
///     </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0018VersionPropsNotImportedAnalyzer : DiagnosticAnalyzer {
    public const string DiagnosticId = DiagnosticIds.VersionPropsNotImported;

    private const string VersionPropsFileName = "Version.props";
    private const string DirectoryBuildPropsFileName = "Directory.Build.props";

    private static readonly LocalizableResourceString Title = new(
        nameof(Resources.AL0018AnalyzerTitle), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableResourceString MessageFormat = new(
        nameof(Resources.AL0018AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableResourceString Description = new(
        nameof(Resources.AL0018AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId, Title, MessageFormat, DiagnosticCategories.VersionManagement,
        DiagnosticSeverity.Warning, true, Description,
        AlAnalyzer.HelpLinkBase,
        WellKnownDiagnosticTags.CompilationEnd);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    public override void Initialize(AnalysisContext context) {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationAction(AnalyzeCompilation);
    }

    private static void AnalyzeCompilation(CompilationAnalysisContext context) {
        // Find Directory.Build.props in AdditionalFiles
        var directoryBuildPropsFiles = context.Options.AdditionalFiles
            .Where(static f => f.Path.EndsWith(DirectoryBuildPropsFileName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var propsFile in directoryBuildPropsFiles) {
            AnalyzePropsFile(context, propsFile);
        }
    }

    private static void AnalyzePropsFile(CompilationAnalysisContext context, AdditionalText propsFile) {
        if (propsFile.GetText(context.CancellationToken) is not { } sourceText) {
            return;
        }

        var content = sourceText.ToString();

        try {
            var doc = XDocument.Parse(content);

            // Look for Import elements that reference Version.props
            var hasVersionPropsImport = doc.Descendants()
                .Where(static e => e.Name.LocalName == "Import")
                .Any(static import =>
                    import.Attribute("Project") is { Value: { } projectValue } &&
                    projectValue.Contains(VersionPropsFileName, StringComparison.OrdinalIgnoreCase));

            if (!hasVersionPropsImport) {
                // Report diagnostic at the start of the file
                var location = Location.Create(propsFile.Path, sourceText.Lines[0].Span,
                    new Microsoft.CodeAnalysis.Text.LinePositionSpan(
                        new Microsoft.CodeAnalysis.Text.LinePosition(0, 0),
                        new Microsoft.CodeAnalysis.Text.LinePosition(0, 0)));

                var diagnostic = Diagnostic.Create(Rule, location);
                context.ReportDiagnostic(diagnostic);
            }
        } catch (Exception) {
            // XML parsing failed - silently ignore malformed files
        }
    }
}
