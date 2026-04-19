using Microsoft.CodeAnalysis.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0017: Detects hardcoded package versions in Directory.Packages.props files.
///     Package versions should use $(VariableName) from Version.props for centralized management.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0017HardcodedPackageVersionAnalyzer : DiagnosticAnalyzer {
    /// <summary>AL0017: Hardcoded package version in Directory.Packages.props.</summary>
    private const string DiagnosticId = "AL0017";

    private const string SuggestedVariableKey = "SuggestedVariable";
    private const string PackageNameKey = "PackageName";
    private const string HardcodedVersionKey = "HardcodedVersion";

    private static readonly LocalizableResourceString Title = new(
        nameof(Resources.AL0017AnalyzerTitle), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableResourceString MessageFormat = new(
        nameof(Resources.AL0017AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableResourceString Description = new(
        nameof(Resources.AL0017AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId, Title, MessageFormat, DiagnosticCategories.VersionManagement,
        DiagnosticSeverity.Warning, true, Description,
        AlAnalyzer.HelpLink(DiagnosticId),
        WellKnownDiagnosticTags.CompilationEnd);

    private static readonly Dictionary<string, string> PackageToVariableMap = new(StringComparer.OrdinalIgnoreCase) {
        ["Microsoft.CodeAnalysis.CSharp"] = "RoslynVersion",
        ["Microsoft.CodeAnalysis.CSharp.Workspaces"] = "RoslynVersion",
        ["Microsoft.CodeAnalysis.Common"] = "RoslynVersion",
        ["Microsoft.CodeAnalysis.Analyzers"] = "RoslynAnalyzersVersion",

        // Testing - xUnit v3
        ["xunit.v3"] = "XunitV3Version",
        ["xunit.v3.mtp-v2"] = "XunitV3Version",
        ["xunit.v3.mtp-v1"] = "XunitV3Version",
        ["xunit.v3.mtp-off"] = "XunitV3Version",

        // Testing - Assertions
        ["AwesomeAssertions"] = "AwesomeAssertionsVersion",
        ["AwesomeAssertions.Analyzers"] = "AwesomeAssertionsAnalyzersVersion",

        // Testing - Loggers
        ["GitHubActionsTestLogger"] = "GitHubActionsTestLoggerVersion",

        // Microsoft Testing Platform (MTP) Extensions
        ["Microsoft.Testing.Extensions.CodeCoverage"] = "CodeCoverageVersion",
        ["Microsoft.Testing.Extensions.TrxReport"] = "MTPExtensionsVersion",
        ["Microsoft.Testing.Extensions.CrashDump"] = "MTPExtensionsVersion",
        ["Microsoft.Testing.Extensions.HangDump"] = "MTPExtensionsVersion",
        ["Microsoft.Testing.Extensions.Retry"] = "MTPExtensionsVersion",
        ["Microsoft.NET.Test.Sdk"] = "TestSdkVersion",
        ["Microsoft.Testing.Platform.MSBuild"] = "MTPExtensionsVersion",

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
        ["Microsoft.Extensions.Logging.Abstractions"] = "AspNetCoreVersion",
        ["Microsoft.Extensions.DependencyInjection"] = "AspNetCoreVersion",
        ["Microsoft.Extensions.DependencyInjection.Abstractions"] = "AspNetCoreVersion",
        ["Microsoft.Extensions.Configuration"] = "AspNetCoreVersion",
        ["Microsoft.Extensions.Configuration.Abstractions"] = "AspNetCoreVersion",
        ["Microsoft.Extensions.Options"] = "AspNetCoreVersion",
        ["Microsoft.AspNetCore.Mvc.Testing"] = "AspNetCoreVersion",
        ["Microsoft.AspNetCore.OpenApi"] = "AspNetCoreVersion",
        ["Microsoft.AspNetCore.Diagnostics.Middleware"] = "MicrosoftExtensionsVersion",
        ["Microsoft.Extensions.Http.Resilience"] = "MicrosoftExtensionsVersion",
        ["Microsoft.Extensions.ServiceDiscovery"] = "MicrosoftExtensionsVersion",
        ["Microsoft.Extensions.Telemetry"] = "MicrosoftExtensionsVersion",
        ["Microsoft.Bcl.AsyncInterfaces"] = "MicrosoftBclAsyncInterfacesVersion",
        ["Microsoft.Bcl.HashCode"] = "BclHashCodeVersion",

        // OpenTelemetry
        ["OpenTelemetry"] = "OpenTelemetryVersion",
        ["OpenTelemetry.Api"] = "OpenTelemetryVersion",
        ["OpenTelemetry.Exporter.Console"] = "OpenTelemetryVersion",
        ["OpenTelemetry.Exporter.OpenTelemetryProtocol"] = "OpenTelemetryVersion",
        ["OpenTelemetry.Extensions.Hosting"] = "OpenTelemetryVersion",
        ["OpenTelemetry.Instrumentation.AspNetCore"] = "OpenTelemetryVersion",
        ["OpenTelemetry.Instrumentation.Http"] = "OpenTelemetryVersion",
        ["OpenTelemetry.Instrumentation.Runtime"] = "OpenTelemetryVersion",
        ["OpenTelemetry.Instrumentation.SqlClient"] = "OpenTelemetryVersion",

        // Build Tools & SDK Analyzers
        ["Basic.Reference.Assemblies.Net100"] = "BasicReferenceAssembliesVersion",
        ["Basic.Reference.Assemblies.Net90"] = "BasicReferenceAssembliesVersion",
        ["Basic.Reference.Assemblies.Net80"] = "BasicReferenceAssembliesVersion",
        ["Basic.Reference.Assemblies.NetStandard20"] = "BasicReferenceAssembliesVersion",
        ["MSBuild.StructuredLogger"] = "MSBuildStructuredLoggerVersion",
        ["NuGet.Protocol"] = "NuGetVersion",
        ["NuGet.Packaging"] = "NuGetVersion",
        ["Microsoft.SourceLink.GitHub"] = "MicrosoftSourceLinkVersion",
        ["Microsoft.Sbom.Targets"] = "SbomTargetsVersion",
        ["Microsoft.CodeAnalysis.BannedApiAnalyzers"] = "BannedApiAnalyzersVersion",
        ["JonSkeet.RoslynAnalyzers"] = "JonSkeetAnalyzersVersion",

        // ANcpLua Ecosystem
        ["ANcpLua.Analyzers"] = "ANcpLuaAnalyzersVersion",
        ["ANcpLua.Roslyn.Utilities"] = "ANcpLuaRoslynUtilitiesVersion",
        ["ANcpLua.Roslyn.Utilities.Sources"] = "ANcpLuaRoslynUtilitiesSourcesVersion",
        ["ANcpLua.Roslyn.Utilities.Testing"] = "ANcpLuaRoslynUtilitiesTestingVersion",

        // Other
        ["JetBrains.Annotations"] = "JetBrainsAnnotationsVersion",
        ["System.Threading.Tasks.Extensions"] = "TasksExtensionsVersion"
    };

    private static readonly Regex MsBuildPropertyPattern = MyRegex();

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context) {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationAction(AnalyzeCompilation);
    }

    private static void AnalyzeCompilation(CompilationAnalysisContext context) {
        var propsFiles = context.Options.AdditionalFiles
            .Where(static f => f.Path.EndsWithIgnoreCase("Directory.Packages.props"))
            .ToList();

        foreach (var propsFile in propsFiles) {
            if (IsCpmNativeMode(propsFile, context.CancellationToken)) {
                continue;
            }

            AnalyzePropsFile(context, propsFile);
        }
    }

    private static bool IsCpmNativeMode(AdditionalText propsFile, CancellationToken cancellationToken) {
        if (propsFile.GetText(cancellationToken) is not { } sourceText) {
            return false;
        }

        try {
            var doc = XDocument.Parse(sourceText.ToString());

            return doc.Descendants()
                .Any(static e =>
                    e.Name.LocalName == "ManagePackageVersionsCentrally" &&
                    string.Equals(e.Value.Trim(), "true", StringComparison.OrdinalIgnoreCase));
        } catch (Exception) {
            return false;
        }
    }

    private static void AnalyzePropsFile(CompilationAnalysisContext context, AdditionalText propsFile) {
        if (propsFile.GetText(context.CancellationToken) is not { } sourceText) {
            return;
        }

        var content = sourceText.ToString();

        try {
            var doc = XDocument.Parse(content);
            var packageVersions = doc.Descendants()
                .Where(static (XElement e) => e.Name.LocalName == "PackageVersion");

            foreach (var pkg in packageVersions) {
                var includeAttr = pkg.Attribute("Include");
                var versionAttr = pkg.Attribute("Version");

                if (includeAttr is null || versionAttr is null) {
                    continue;
                }

                var packageName = includeAttr.Value;
                var versionValue = versionAttr.Value;

                if (MsBuildPropertyPattern.IsMatch(versionValue)) {
                    continue;
                }

                var suggestedVariable = GetSuggestedVariableName(packageName);

                IXmlLineInfo lineInfo = versionAttr;
                var location = Location.None;

                if (lineInfo.HasLineInfo()) {
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
            return;
        }
    }

    private static string GetSuggestedVariableName(string packageName) {
        if (PackageToVariableMap.TryGetValue(packageName, out var variable)) {
            return variable;
        }

        var cleanName = packageName
            .ReplaceOrdinal(".", string.Empty)
            .ReplaceOrdinal("-", string.Empty)
            .ReplaceOrdinal("_", string.Empty);
        return cleanName + "Version";
    }

#if NET7_0_OR_GREATER
    [GeneratedRegex(@"^\$\(.+\)$", RegexOptions.Compiled)]
    private static partial Regex MyRegex();
#else
    private static Regex MyRegex() => new(@"^\$\(.+\)$", RegexOptions.Compiled);
#endif
}
