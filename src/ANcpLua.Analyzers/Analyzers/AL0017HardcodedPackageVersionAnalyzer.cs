using ANcpLua.Analyzers.Core;
using Microsoft.CodeAnalysis.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0017: Detects hardcoded package versions in Directory.Packages.props files.
///     Package versions should use $(VariableName) from Version.props for centralized management.
/// </summary>
/// <remarks>
///     <para>
///         Central Package Management (CPM) works best when versions are defined as MSBuild
///         properties in a separate file (typically Version.props) and referenced via
///         <c>$(PropertyName)</c> syntax. Hardcoding versions directly in Directory.Packages.props
///         defeats the purpose of centralized management and makes coordinated version updates
///         across related packages more error-prone.
///     </para>
///     <para>
///         The analyzer examines Directory.Packages.props files added as additional files
///         to the compilation. It flags any <c>&lt;PackageVersion&gt;</c> element where the
///         <c>Version</c> attribute contains a literal version string rather than an MSBuild
///         property reference.
///     </para>
///     <para>
///         The analyzer includes a mapping of common packages to their expected variable names
///         (e.g., Microsoft.CodeAnalysis.CSharp should use <c>$(RoslynVersion)</c>). For
///         unknown packages, it generates a suggested variable name based on the package name.
///     </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AL0017HardcodedPackageVersionAnalyzer : DiagnosticAnalyzer {
    public const string DiagnosticId = DiagnosticIds.HardcodedPackageVersion;

    /// <summary>Property key for the suggested variable name.</summary>
    public const string SuggestedVariableKey = "SuggestedVariable";

    /// <summary>Property key for the package name.</summary>
    public const string PackageNameKey = "PackageName";

    /// <summary>Property key for the hardcoded version.</summary>
    public const string HardcodedVersionKey = "HardcodedVersion";

    private static readonly LocalizableResourceString Title = new(
        nameof(Resources.AL0017AnalyzerTitle), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableResourceString MessageFormat = new(
        nameof(Resources.AL0017AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableResourceString Description = new(
        nameof(Resources.AL0017AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId, Title, MessageFormat, DiagnosticCategories.VersionManagement,
        DiagnosticSeverity.Warning, true, Description,
        ALAnalyzer.HelpLinkBase,
        WellKnownDiagnosticTags.CompilationEnd);

    /// <summary>
    ///     Maps common package name patterns to their expected version variable names.
    ///     Used to suggest the correct $(VariableName) for the code fix.
    /// </summary>
    private static readonly Dictionary<string, string> PackageToVariableMap = new(StringComparer.OrdinalIgnoreCase) {
        // Roslyn
        ["Microsoft.CodeAnalysis.CSharp"] = "RoslynVersion",
        ["Microsoft.CodeAnalysis.CSharp.Workspaces"] = "RoslynVersion",
        ["Microsoft.CodeAnalysis.Common"] = "RoslynVersion",
        ["Microsoft.CodeAnalysis.Analyzers"] = "RoslynAnalyzersVersion",
        // Testing
        ["xunit.v3"] = "XunitV3Version",
        ["xunit.v3.mtp-v2"] = "XunitV3Version",
        ["xunit.v3.mtp-v1"] = "XunitV3Version",
        ["AwesomeAssertions"] = "AwesomeAssertionsVersion",
        ["AwesomeAssertions.Analyzers"] = "AwesomeAssertionsAnalyzersVersion",
        ["GitHubActionsTestLogger"] = "GitHubActionsTestLoggerVersion",
        // Analyzer Testing
        ["Microsoft.CodeAnalysis.Analyzer.Testing"] = "AnalyzerTestingVersion",
        ["Microsoft.CodeAnalysis.CSharp.Analyzer.Testing"] = "AnalyzerTestingVersion",
        ["Microsoft.CodeAnalysis.CSharp.CodeFix.Testing"] = "AnalyzerTestingVersion",
        ["Microsoft.CodeAnalysis.CSharp.CodeRefactoring.Testing"] = "AnalyzerTestingVersion",
        ["Microsoft.CodeAnalysis.CSharp.SourceGenerators.Testing"] = "AnalyzerTestingVersion",
        // Meziantou
        ["Meziantou.Framework"] = "MeziantouFrameworkVersion",
        ["Meziantou.Framework.FullPath"] = "MeziantouFullPathVersion",
        ["Meziantou.Framework.TemporaryDirectory"] = "MeziantouTemporaryDirectoryVersion",
        ["Meziantou.Framework.Threading"] = "MeziantouThreadingVersion",
        ["Meziantou.Framework.DependencyScanning"] = "MeziantouDependencyScanningVersion",
        ["Meziantou.Analyzer"] = "MeziantouAnalyzerVersion",
        ["Meziantou.Xunit.v3.ParallelTestFramework"] = "MeziantouParallelTestFrameworkVersion",
        // Microsoft.Extensions
        ["Microsoft.Extensions.Hosting.Abstractions"] = "AspNetCoreVersion",
        ["Microsoft.Extensions.Logging"] = "AspNetCoreVersion",
        ["Microsoft.AspNetCore.Mvc.Testing"] = "AspNetCoreVersion",
        ["Microsoft.AspNetCore.OpenApi"] = "AspNetCoreVersion",
        ["Microsoft.AspNetCore.Diagnostics.Middleware"] = "MicrosoftExtensionsVersion",
        ["Microsoft.Extensions.Http.Resilience"] = "MicrosoftExtensionsVersion",
        ["Microsoft.Extensions.ServiceDiscovery"] = "MicrosoftExtensionsVersion",
        ["Microsoft.Extensions.Telemetry"] = "MicrosoftExtensionsVersion",
        ["Microsoft.Bcl.AsyncInterfaces"] = "MicrosoftBclAsyncInterfacesVersion",
        ["Microsoft.Bcl.HashCode"] = "BclHashCodeVersion",
        // OpenTelemetry
        ["OpenTelemetry.Exporter.Console"] = "OpenTelemetryVersion",
        ["OpenTelemetry.Exporter.OpenTelemetryProtocol"] = "OpenTelemetryVersion",
        ["OpenTelemetry.Extensions.Hosting"] = "OpenTelemetryVersion",
        ["OpenTelemetry.Instrumentation.AspNetCore"] = "OpenTelemetryVersion",
        ["OpenTelemetry.Instrumentation.Http"] = "OpenTelemetryVersion",
        ["OpenTelemetry.Instrumentation.Runtime"] = "OpenTelemetryVersion",
        // Build Tools
        ["Basic.Reference.Assemblies.Net100"] = "BasicReferenceAssembliesVersion",
        ["Basic.Reference.Assemblies.NetStandard20"] = "BasicReferenceAssembliesVersion",
        ["MSBuild.StructuredLogger"] = "MSBuildStructuredLoggerVersion",
        ["NuGet.Protocol"] = "NuGetVersion",
        ["NuGet.Packaging"] = "NuGetVersion",
        ["Microsoft.SourceLink.GitHub"] = "MicrosoftSourceLinkVersion",
        // Other
        ["JetBrains.Annotations"] = "JetBrainsAnnotationsVersion"
    };

    /// <summary>Pattern to detect MSBuild property references like $(VariableName).</summary>
    private static readonly Regex MsBuildPropertyPattern = new(@"^\$\(.+\)$", RegexOptions.Compiled);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    public override void Initialize(AnalysisContext context) {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationAction(AnalyzeCompilation);
    }

    private static void AnalyzeCompilation(CompilationAnalysisContext context) {
        // Find Directory.Packages.props in AdditionalFiles
        var propsFiles = context.Options.AdditionalFiles
            .Where(static f => f.Path.EndsWith("Directory.Packages.props", StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var propsFile in propsFiles) {
            AnalyzePropsFile(context, propsFile);
        }
    }

    private static void AnalyzePropsFile(CompilationAnalysisContext context, AdditionalText propsFile) {
        var sourceText = propsFile.GetText(context.CancellationToken);
        if (sourceText == null) {
            return;
        }

        var content = sourceText.ToString();

        try {
            var doc = XDocument.Parse(content);
            var packageVersions = doc.Descendants()
                .Where(static e => e.Name.LocalName == "PackageVersion");

            foreach (var pkg in packageVersions) {
                var includeAttr = pkg.Attribute("Include");
                var versionAttr = pkg.Attribute("Version");

                if (includeAttr == null || versionAttr == null) {
                    continue;
                }

                var packageName = includeAttr.Value;
                var versionValue = versionAttr.Value;

                // Skip if already using $(VariableName) syntax
                if (MsBuildPropertyPattern.IsMatch(versionValue)) {
                    continue;
                }

                // This is a hardcoded version - report diagnostic
                var suggestedVariable = GetSuggestedVariableName(packageName);

                // Create location from the XML position
                IXmlLineInfo lineInfo = versionAttr;
                var location = Location.None;

                if (lineInfo.HasLineInfo()) {
                    // Find the position in source text
                    var linePosition = new LinePosition(lineInfo.LineNumber - 1, lineInfo.LinePosition - 1);
                    var textSpan = sourceText.Lines[lineInfo.LineNumber - 1].Span;
                    location = Location.Create(propsFile.Path, textSpan,
                        new LinePositionSpan(linePosition, linePosition));
                }

                var properties = ImmutableDictionary.CreateBuilder<string, string?>();
                properties.Add(SuggestedVariableKey, suggestedVariable);
                properties.Add(PackageNameKey, packageName);
                properties.Add(HardcodedVersionKey, versionValue);

                var diagnostic = Diagnostic.Create(
                    Rule,
                    location,
                    properties.ToImmutable(),
                    packageName,
                    versionValue,
                    suggestedVariable);

                context.ReportDiagnostic(diagnostic);
            }
        } catch (Exception) {
            // XML parsing failed - silently ignore malformed files
        }
    }

    /// <summary>
    ///     Gets the suggested version variable name for a package.
    ///     Returns a generated name if the package is not in the known map.
    /// </summary>
    private static string GetSuggestedVariableName(string packageName) {
        if (PackageToVariableMap.TryGetValue(packageName, out var variable)) {
            return variable;
        }

        // Generate a variable name from the package name
        // e.g., "Some.Package.Name" -> "SomePackageNameVersion"
        var cleanName = packageName
            .Replace(".", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .Replace("_", string.Empty, StringComparison.Ordinal);
        return cleanName + "Version";
    }
}
