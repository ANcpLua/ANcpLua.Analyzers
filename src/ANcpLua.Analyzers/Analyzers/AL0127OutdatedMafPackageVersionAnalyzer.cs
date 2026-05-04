using Microsoft.CodeAnalysis.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0127: Detects outdated Microsoft Agent Framework (MAF) and related package versions
///     in Directory.Packages.props and Version.props files.
///     Package versions must meet minimum thresholds (e.g., MAF GA 1.0.0, MEAI 10.0.0).
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0127OutdatedMafPackageVersionAnalyzer : DiagnosticAnalyzer {
    /// <summary>AL0127: Outdated MAF ecosystem package version.</summary>
    private const string DiagnosticId = "AL0127";

    private const string PackageNameKey = "PackageName";
    private const string CurrentVersionKey = "CurrentVersion";
    private const string MinimumVersionKey = "MinimumVersion";

    private static readonly LocalizableResourceString s_title = new(
        nameof(Resources.AL0127AnalyzerTitle), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableResourceString s_messageFormat = new(
        nameof(Resources.AL0127AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableResourceString s_description = new(
        nameof(Resources.AL0127AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

    private static readonly DiagnosticDescriptor s_rule = new(
        DiagnosticId, s_title, s_messageFormat, DiagnosticCategories.VersionManagement,
        DiagnosticSeverity.Warning, true, s_description,
        AlAnalyzer.HelpLink(DiagnosticId),
        WellKnownDiagnosticTags.CompilationEnd);

    /// <summary>Pattern to detect MSBuild property references like $(VariableName).</summary>
    private static readonly Regex s_msBuildPropertyPattern = MsBuildPropertyRegex();

    /// <summary>
    ///     Package version requirements for MAF ecosystem packages.
    ///     Key: package name (case-insensitive). Value: minimum required version and reason.
    /// </summary>
    private static readonly Dictionary<string, VersionRequirement> s_packageRequirements = new(StringComparer.OrdinalIgnoreCase) {
        // MAF Core — GA 1.0.0 (was rc5)
        ["Microsoft.Agents.AI"] = new("1.0.0", "GA — https://nuget.org/packages/Microsoft.Agents.AI"),
        ["Microsoft.Agents.AI.Abstractions"] = new("1.0.0", "GA — https://nuget.org/packages/Microsoft.Agents.AI.Abstractions"),
        ["Microsoft.Agents.AI.OpenAI"] = new("1.0.0", "GA — https://nuget.org/packages/Microsoft.Agents.AI.OpenAI"),
        ["Microsoft.Agents.AI.Workflows"] = new("1.0.0", "GA — https://nuget.org/packages/Microsoft.Agents.AI.Workflows"),
        ["Microsoft.Agents.AI.Workflows.Generators"] = new("1.0.0", "GA — https://nuget.org/packages/Microsoft.Agents.AI.Workflows.Generators"),
        ["Microsoft.Agents.AI.Foundry"] = new("1.0.0", "GA — https://nuget.org/packages/Microsoft.Agents.AI.Foundry"),

        // MAF RC6 (was rc5)
        ["Microsoft.Agents.AI.Anthropic"] = new("1.0.0-rc6", "rc6 — https://nuget.org/packages/Microsoft.Agents.AI.Anthropic"),
        ["Microsoft.Agents.AI.Declarative"] = new("1.0.0-rc6", "rc6 — https://nuget.org/packages/Microsoft.Agents.AI.Declarative"),
        ["Microsoft.Agents.AI.Purview"] = new("1.0.0-rc6", "rc6 — https://nuget.org/packages/Microsoft.Agents.AI.Purview"),
        ["Microsoft.Agents.AI.Workflows.Declarative"] = new("1.0.0-rc6", "rc6 — https://nuget.org/packages/Microsoft.Agents.AI.Workflows.Declarative"),
        ["Microsoft.Agents.AI.Workflows.Declarative.Foundry"] = new("1.0.0-rc6", "rc6 — https://nuget.org/packages/Microsoft.Agents.AI.Workflows.Declarative.Foundry"),

        // MAF Preview (was 260330.1)
        ["Microsoft.Agents.AI.A2A"] = new("1.0.0-preview.260402.1", "https://nuget.org/packages/Microsoft.Agents.AI.A2A"),
        ["Microsoft.Agents.AI.AGUI"] = new("1.0.0-preview.260402.1", "https://nuget.org/packages/Microsoft.Agents.AI.AGUI"),
        ["Microsoft.Agents.AI.AzureAI.Persistent"] = new("1.0.0-preview.260402.1", "https://nuget.org/packages/Microsoft.Agents.AI.AzureAI.Persistent"),
        ["Microsoft.Agents.AI.CopilotStudio"] = new("1.0.0-preview.260402.1", "https://nuget.org/packages/Microsoft.Agents.AI.CopilotStudio"),
        ["Microsoft.Agents.AI.DevUI"] = new("1.0.0-preview.260402.1", "https://nuget.org/packages/Microsoft.Agents.AI.DevUI"),
        ["Microsoft.Agents.AI.DurableTask"] = new("1.0.0-preview.260402.1", "https://nuget.org/packages/Microsoft.Agents.AI.DurableTask"),
        ["Microsoft.Agents.AI.Hosting"] = new("1.0.0-preview.260402.1", "https://nuget.org/packages/Microsoft.Agents.AI.Hosting"),
        ["Microsoft.Agents.AI.Hosting.A2A"] = new("1.0.0-preview.260402.1", "https://nuget.org/packages/Microsoft.Agents.AI.Hosting.A2A"),
        ["Microsoft.Agents.AI.Hosting.A2A.AspNetCore"] = new("1.0.0-preview.260402.1", "https://nuget.org/packages/Microsoft.Agents.AI.Hosting.A2A.AspNetCore"),
        ["Microsoft.Agents.AI.Hosting.AGUI.AspNetCore"] = new("1.0.0-preview.260402.1", "https://nuget.org/packages/Microsoft.Agents.AI.Hosting.AGUI.AspNetCore"),
        ["Microsoft.Agents.AI.Hosting.AzureFunctions"] = new("1.0.0-preview.260402.1", "https://nuget.org/packages/Microsoft.Agents.AI.Hosting.AzureFunctions"),
        ["Microsoft.Agents.AI.GitHub.Copilot"] = new("1.0.0-preview.260402.1", "https://nuget.org/packages/Microsoft.Agents.AI.GitHub.Copilot"),
        ["Microsoft.Agents.AI.CosmosNoSql"] = new("1.0.0-preview.260402.1", "https://nuget.org/packages/Microsoft.Agents.AI.CosmosNoSql"),

        // AgentFrameworkToolkit — GA 1.0.0 (was rc5)
        ["AgentFrameworkToolkit.Anthropic"] = new("1.0.0", "GA"),
        ["AgentFrameworkToolkit.AzureOpenAI"] = new("1.0.0", "GA"),
        ["AgentFrameworkToolkit.Google"] = new("1.0.0", "GA"),
        ["AgentFrameworkToolkit.Tools"] = new("1.0.0", "GA"),
        ["AgentFrameworkToolkit.Tools.ModelContextProtocol"] = new("1.0.0", "GA"),
        ["AgentSkillsDotNet"] = new("1.0.0", "GA"),

        // Microsoft.Extensions.AI — 10.0.0+ (for AIFunctionFactoryOptions)
        ["Microsoft.Extensions.AI"] = new("10.0.0", "AIFunctionFactoryOptions requires 10.0.0+"),
        ["Microsoft.Extensions.AI.Abstractions"] = new("10.0.0", "AIFunctionFactoryOptions requires 10.0.0+"),
        ["Microsoft.Extensions.AI.OpenAI"] = new("10.0.0", "MEAI OpenAI requires 10.0.0+"),

        // ModelContextProtocol — 1.2.0 (was 1.1.0)
        ["ModelContextProtocol.Core"] = new("1.2.0", "https://nuget.org/packages/ModelContextProtocol.Core"),
        ["ModelContextProtocol"] = new("1.2.0", "https://nuget.org/packages/ModelContextProtocol"),
        ["ModelContextProtocol.AspNetCore"] = new("1.2.0", "https://nuget.org/packages/ModelContextProtocol.AspNetCore"),
    };

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_rule];

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context) {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationAction(AnalyzeCompilation);
    }

    private static void AnalyzeCompilation(CompilationAnalysisContext context) {
        // Collect property values from Version.props for variable resolution
        var versionPropsFile = context.Options.AdditionalFiles
            .FirstOrDefault(static f => f.Path.EndsWithIgnoreCase("Version.props"));

        var propertyValues = versionPropsFile is not null
            ? CollectPropertyValues(versionPropsFile, context.CancellationToken)
            : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var packagesPropsFiles = context.Options.AdditionalFiles
            .Where(static f => f.Path.EndsWithIgnoreCase("Directory.Packages.props"))
            .ToList();

        foreach (var propsFile in packagesPropsFiles) {
            AnalyzeDirectoryPackagesProps(context, propsFile, propertyValues);
        }
    }

    /// <summary>
    ///     Collects property names and their values from Version.props.
    ///     Used to resolve $(VariableName) references in Directory.Packages.props.
    /// </summary>
    private static Dictionary<string, string> CollectPropertyValues(
        AdditionalText versionPropsFile,
        CancellationToken cancellationToken) {
        var properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (versionPropsFile.GetText(cancellationToken) is not { } sourceText) {
            return properties;
        }

        try {
            var doc = XDocument.Parse(sourceText.ToString());
            var propertyGroups = doc.Descendants()
                .Where(static e => e.Name.LocalName == "PropertyGroup");

            foreach (var propertyGroup in propertyGroups) {
                foreach (var element in propertyGroup.Elements()) {
                    var value = element.Value.Trim();
                    if (value.Length > 0) {
                        properties[element.Name.LocalName] = value;
                    }
                }
            }
        } catch (Exception) {
            return properties;
        }

        return properties;
    }

    /// <summary>
    ///     Analyzes Directory.Packages.props for outdated MAF package versions.
    ///     Handles both hardcoded versions and $(Variable) references resolved from Version.props.
    /// </summary>
    private static void AnalyzeDirectoryPackagesProps(
        CompilationAnalysisContext context,
        AdditionalText propsFile,
        IReadOnlyDictionary<string, string> propertyValues) {
        if (propsFile.GetText(context.CancellationToken) is not { } sourceText) {
            return;
        }

        var content = sourceText.ToString();

        try {
            var doc = XDocument.Parse(content, LoadOptions.SetLineInfo);
            var packageVersions = doc.Descendants()
                .Where(static e => e.Name.LocalName == "PackageVersion");

            foreach (var pkg in packageVersions) {
                var includeAttr = pkg.Attribute("Include");
                var versionAttr = pkg.Attribute("Version");

                if (includeAttr is null || versionAttr is null) {
                    continue;
                }

                var packageName = includeAttr.Value;
                var versionValue = versionAttr.Value;

                if (!s_packageRequirements.TryGetValue(packageName, out var requirement)) {
                    continue;
                }

                // Resolve version: either hardcoded or from $(Variable)
                if (ResolveVersion(versionValue, propertyValues) is not { } resolvedVersion) {
                    continue;
                }

                if (IsVersionBelowMinimum(resolvedVersion, requirement.MinimumVersion)) {
                    var location = CreateLocation(propsFile, sourceText, versionAttr);
                    ReportOutdatedVersion(context, location, packageName, resolvedVersion, requirement);
                }
            }
        } catch (Exception) {
            return;
        }
    }

    /// <summary>
    ///     Resolves a version value, handling both hardcoded versions and $(Variable) references.
    /// </summary>
    /// <returns>The resolved version string, or <c>null</c> if a variable could not be resolved.</returns>
    private static string? ResolveVersion(string versionValue, IReadOnlyDictionary<string, string> propertyValues) {
        var match = s_msBuildPropertyPattern.Match(versionValue);

        if (!match.Success) {
            return versionValue; // Hardcoded version
        }

        var variableName = match.Groups[1].Value;
        return propertyValues.TryGetValue(variableName, out var resolved) ? resolved : null;
    }

    /// <summary>
    ///     Compares two version strings using semantic versioning rules.
    ///     A stable version (1.0.0) is always >= any prerelease of the same stable part (1.0.0-rc5).
    /// </summary>
    /// <returns><c>true</c> if <paramref name="currentVersion"/> is below <paramref name="minimumVersion"/>.</returns>
    private static bool IsVersionBelowMinimum(string currentVersion, string minimumVersion) {
        var current = ParseVersion(currentVersion);
        var minimum = ParseVersion(minimumVersion);

        if (current is null || minimum is null) {
            return false; // Unparseable versions are not flagged
        }

        // Compare major.minor.patch first
        var stableComparison = CompareStableParts(current.Value, minimum.Value);

        return stableComparison switch {
            < 0 => true,  // Current stable part is lower
            > 0 => false, // Current stable part is higher
            // Stable parts are equal - compare prerelease
            _ => ComparePrereleaseLabels(current.Value.Prerelease, minimum.Value.Prerelease)
        };
    }

    private static int CompareStableParts(ParsedVersion current, ParsedVersion minimum) {
        var majorDiff = current.Major.CompareTo(minimum.Major);
        if (majorDiff is not 0) {
            return majorDiff;
        }

        var minorDiff = current.Minor.CompareTo(minimum.Minor);
        if (minorDiff is not 0) {
            return minorDiff;
        }

        return current.Patch.CompareTo(minimum.Patch);
    }

    /// <summary>
    ///     Compares prerelease labels following SemVer 2.0 rules:
    ///     - No prerelease (stable) > any prerelease
    ///     - Prerelease labels are compared ordinally (rc5 vs rc6)
    /// </summary>
    /// <returns><c>true</c> if current is below minimum (i.e., outdated).</returns>
    private static bool ComparePrereleaseLabels(string? currentPrerelease, string? minimumPrerelease) {
        switch (currentPrerelease)
        {
            // Both stable - equal
            case null when minimumPrerelease is null:
            // Current is stable, minimum is prerelease - current is >= minimum
            case null:
                return false;
        }

        // Current is prerelease, minimum is stable - current is < minimum
        if (minimumPrerelease is null) {
            return true;
        }

        // Both are prerelease - compare ordinally (case-insensitive)
        return string.Compare(currentPrerelease, minimumPrerelease, StringComparison.OrdinalIgnoreCase) < 0;
    }

    private static ParsedVersion? ParseVersion(string version) {
        var normalized = version.Trim();

        // Strip leading 'v' or 'V'
        if (normalized.Length > 0 && (normalized[0] == 'v' || normalized[0] == 'V')) {
            normalized = normalized.Substring(1);
        }

        if (normalized.Length is 0) {
            return null;
        }

        // Split off prerelease label
        string? prerelease = null;
        var hyphenIndex = normalized.IndexOfOrdinal("-");
        if (hyphenIndex >= 0) {
            prerelease = normalized.Substring(hyphenIndex + 1);
            normalized = normalized.Substring(0, hyphenIndex);
        }

        // Parse major.minor.patch
        var parts = normalized.Split('.');

        if (!int.TryParse(parts[0], out var major)) {
            return null;
        }

        var minor = 0;
        if (parts.Length >= 2 && !int.TryParse(parts[1], out minor)) {
            return null;
        }

        var patch = 0;
        if (parts.Length >= 3 && !int.TryParse(parts[2], out patch)) {
            return null;
        }

        return new ParsedVersion(major, minor, patch, prerelease);
    }

    private static Location CreateLocation(AdditionalText propsFile, SourceText sourceText, XAttribute attribute) {
        IXmlLineInfo lineInfo = attribute;
        if (lineInfo.HasLineInfo()) {
            var linePosition = new LinePosition(lineInfo.LineNumber - 1, lineInfo.LinePosition - 1);
            var textSpan = sourceText.Lines[lineInfo.LineNumber - 1].Span;
            return Location.Create(propsFile.Path, textSpan,
                new LinePositionSpan(linePosition, linePosition));
        }

        return Location.None;
    }

    private static void ReportOutdatedVersion(
        CompilationAnalysisContext context,
        Location location,
        string packageName,
        string currentVersion,
        VersionRequirement requirement) {
        var properties = ImmutableDictionary.CreateBuilder<string, string?>();
        properties.Add(PackageNameKey, packageName);
        properties.Add(CurrentVersionKey, currentVersion);
        properties.Add(MinimumVersionKey, requirement.MinimumVersion);

        var diagnostic = Diagnostic.Create(
            s_rule,
            location,
            properties.ToImmutable(),
            packageName,
            currentVersion,
            requirement.MinimumVersion);

        context.ReportDiagnostic(diagnostic);
    }

    /// <summary>Parsed semantic version with optional prerelease label.</summary>
    private readonly partial record struct ParsedVersion(int Major, int Minor, int Patch, string? Prerelease);

    /// <summary>Minimum version requirement for a tracked package.</summary>
    private sealed partial record VersionRequirement(string MinimumVersion, string Reason);

#if NET7_0_OR_GREATER
    [GeneratedRegex(@"^\$\(([^)]+)\)$", RegexOptions.Compiled)]
    private static partial Regex MsBuildPropertyRegex();
#else
    private static Regex MsBuildPropertyRegex() => new(@"^\$\(([^)]+)\)$", RegexOptions.Compiled);
#endif
}
