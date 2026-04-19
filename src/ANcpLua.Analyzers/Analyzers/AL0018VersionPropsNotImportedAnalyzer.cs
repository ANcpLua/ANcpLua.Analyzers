using Microsoft.CodeAnalysis.Text;
using System.Xml.Linq;

namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0018: Detects when Version.props is not imported in Directory.Build.props or Directory.Packages.props.
///     Version.props should be imported for centralized version management.
/// </summary>
/// <remarks>
///     <para>
///         Central Package Management (CPM) works best when versions are defined as MSBuild
///         properties in a separate file (typically Version.props). This file should be
///         imported in Directory.Build.props or Directory.Packages.props so all projects
///         in the solution can reference the version variables.
///     </para>
///     <para>
///         The analyzer supports two valid import locations (per the layering pattern):
///         - Directory.Build.props: Traditional location for MSBuild property imports
///         - Directory.Packages.props: Valid for CPM scenarios where version variables
///         are only used by PackageVersion items
///     </para>
///     <para>
///         The analyzer examines both files when added as additional files to the
///         compilation. It only flags Directory.Build.props if neither file imports
///         Version.props.
///     </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0018VersionPropsNotImportedAnalyzer : DiagnosticAnalyzer {
    /// <summary>AL0018: Version.props not imported.</summary>
    private const string DiagnosticId = "AL0018";

    /// <summary>Filename for Version.props.</summary>
    private const string VersionPropsFileName = "Version.props";
    /// <summary>Filename for Directory.Build.props.</summary>
    private const string DirectoryBuildPropsFileName = "Directory.Build.props";
    /// <summary>Filename for Directory.Packages.props.</summary>
    private const string DirectoryPackagesPropsFileName = "Directory.Packages.props";

    private static readonly LocalizableResourceString Title = new(
        nameof(Resources.AL0018AnalyzerTitle), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableResourceString MessageFormat = new(
        nameof(Resources.AL0018AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableResourceString Description = new(
        nameof(Resources.AL0018AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId, Title, MessageFormat, DiagnosticCategories.VersionManagement,
        DiagnosticSeverity.Warning, true, Description,
        AlAnalyzer.HelpLink(DiagnosticId),
        WellKnownDiagnosticTags.CompilationEnd);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    /// <summary>Initializes the analyzer and registers compilation-level actions.</summary>
    public override void Initialize(AnalysisContext context) {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationAction(AnalyzeCompilation);
    }

    private static void AnalyzeCompilation(CompilationAnalysisContext context) {
        // Check for CPM via MSBuild property (exposed through AnalyzerConfigOptions)
        // This is more reliable than checking AdditionalFiles since Directory.Packages.props
        // is not typically added as an additional file
        if (IsCpmEnabledViaMsBuildProperty(context)) {
            return;
        }

        // Find Directory.Build.props and Directory.Packages.props in AdditionalFiles
        var directoryBuildPropsFiles = context.Options.AdditionalFiles
            .Where(static f => f.Path.EndsWithIgnoreCase(DirectoryBuildPropsFileName))
            .ToList();

        var directoryPackagesPropsFiles = context.Options.AdditionalFiles
            .Where(static f => f.Path.EndsWithIgnoreCase(DirectoryPackagesPropsFileName))
            .ToList();

        // Skip if using CPM native mode (ManagePackageVersionsCentrally=true)
        // In CPM native mode, Version.props is not needed - versions are defined directly in Directory.Packages.props
        if (directoryPackagesPropsFiles.Any(f => IsCpmNativeMode(f, context.CancellationToken))) {
            return;
        }

        // Check if Version.props is imported in Directory.Packages.props
        // This is valid per the layering pattern
        var hasImportInPackagesProps = directoryPackagesPropsFiles
            .Any(f => HasVersionPropsImport(f, context.CancellationToken));

        foreach (var propsFile in directoryBuildPropsFiles) {
            AnalyzePropsFile(context, propsFile, hasImportInPackagesProps);
        }
    }

    /// <summary>
    ///     Checks if CPM is enabled via MSBuild property exposed through AnalyzerConfigOptions.
    ///     This is more reliable than checking AdditionalFiles.
    /// </summary>
    private static bool IsCpmEnabledViaMsBuildProperty(CompilationAnalysisContext context) {
        // Check global analyzer config options for build_property.ManagePackageVersionsCentrally
        var globalOptions = context.Options.AnalyzerConfigOptionsProvider.GlobalOptions;

        if (globalOptions.TryGetValue("build_property.ManagePackageVersionsCentrally", out var cpmValue) &&
            string.Equals(cpmValue, "true", StringComparison.OrdinalIgnoreCase)) {
            return true;
        }

        return false;
    }

    /// <summary>
    ///     Checks if the props file uses CPM native mode (ManagePackageVersionsCentrally=true).
    ///     When CPM native mode is active, Version.props import is not required.
    /// </summary>
    private static bool IsCpmNativeMode(AdditionalText propsFile, CancellationToken cancellationToken) {
        if (propsFile.GetText(cancellationToken) is not { } sourceText) {
            return false;
        }

        try {
            var doc = XDocument.Parse(sourceText.ToString());

            // Check for <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
            return doc.Descendants()
                .Any(static e =>
                    e.Name.LocalName == "ManagePackageVersionsCentrally" &&
                    string.Equals(e.Value.Trim(), "true", StringComparison.OrdinalIgnoreCase));
        } catch (Exception) {
            return false;
        }
    }

    /// <summary>
    ///     Checks if a props file imports Version.props.
    /// </summary>
    private static bool HasVersionPropsImport(AdditionalText propsFile, CancellationToken cancellationToken) {
        if (propsFile.GetText(cancellationToken) is not { } sourceText) {
            return false;
        }

        try {
            var doc = XDocument.Parse(sourceText.ToString());

            return doc.Descendants()
                .Where(static e => e.Name.LocalName == "Import")
                .Any(static import =>
                    import.Attribute("Project") is { Value: { } projectValue } &&
                    projectValue.ContainsIgnoreCase(VersionPropsFileName));
        } catch (Exception) {
            return false;
        }
    }

    private static void AnalyzePropsFile(CompilationAnalysisContext context, AdditionalText propsFile, bool hasImportInPackagesProps) {
        // If Version.props is already imported in Directory.Packages.props, don't flag
        if (hasImportInPackagesProps) {
            return;
        }

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
                    projectValue.ContainsIgnoreCase(VersionPropsFileName));

            if (!hasVersionPropsImport) {
                // Report diagnostic at the start of the file
                var location = Location.Create(propsFile.Path, sourceText.Lines[0].Span,
                    new LinePositionSpan(
                        new LinePosition(0, 0),
                        new LinePosition(0, 0)));

                var diagnostic = Diagnostic.Create(Rule, location);
                context.ReportDiagnostic(diagnostic);
            }
        } catch (Exception) {
            return;
        }
    }
}
