using ANcpLua.Analyzers.Core;
using Microsoft.CodeAnalysis.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0019: Detects undefined version variables in Directory.Packages.props files.
///     When $(VariableName) is used but not defined in Version.props, this analyzer flags it.
/// </summary>
/// <remarks>
///     <para>
///         This analyzer completes the version management trio (AL0017, AL0018, AL0019):
///         - AL0017: Detects hardcoded versions (should use $(Variable))
///         - AL0018: Detects missing Version.props import
///         - AL0019: Detects undefined variables ($(Variable) not in Version.props)
///     </para>
///     <para>
///         The analyzer examines Directory.Packages.props and Version.props files added as
///         additional files to the compilation. It collects all property definitions from
///         Version.props and flags any $(VariableName) reference in Directory.Packages.props
///         Version attributes that isn't defined.
///     </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0019UndefinedVersionVariableAnalyzer : DiagnosticAnalyzer {
    public const string DiagnosticId = DiagnosticIds.UndefinedVersionVariable;

    /// <summary>Property key for the undefined variable name.</summary>
    public const string VariableNameKey = "VariableName";

    /// <summary>Property key for the package name.</summary>
    public const string PackageNameKey = "PackageName";

    private static readonly LocalizableResourceString Title = new(
        nameof(Resources.AL0019AnalyzerTitle), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableResourceString MessageFormat = new(
        nameof(Resources.AL0019AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableResourceString Description = new(
        nameof(Resources.AL0019AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId, Title, MessageFormat, DiagnosticCategories.VersionManagement,
        DiagnosticSeverity.Warning, true, Description,
        AlAnalyzer.HelpLinkBase,
        WellKnownDiagnosticTags.CompilationEnd);

    /// <summary>Pattern to extract MSBuild property name from $(VariableName) syntax.</summary>
    private static readonly Regex MsBuildPropertyPattern = new(@"^\$\(([^)]+)\)$", RegexOptions.Compiled);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    public override void Initialize(AnalysisContext context) {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationAction(AnalyzeCompilation);
    }

    private static void AnalyzeCompilation(CompilationAnalysisContext context) {
        // Find Version.props to collect defined properties
        var versionPropsFile = context.Options.AdditionalFiles
            .FirstOrDefault(static f => f.Path.EndsWith("Version.props", StringComparison.OrdinalIgnoreCase));

        var definedProperties = versionPropsFile is not null
            ? CollectDefinedProperties(versionPropsFile, context.CancellationToken)
            : [];

        // Find Directory.Packages.props to check for undefined variables
        var packagesPropsFiles = context.Options.AdditionalFiles
            .Where(static f => f.Path.EndsWith("Directory.Packages.props", StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var propsFile in packagesPropsFiles) {
            AnalyzePackagesPropsFile(context, propsFile, definedProperties);
        }
    }

    /// <summary>
    ///     Collects all property names defined in Version.props.
    /// </summary>
    private static HashSet<string> CollectDefinedProperties(AdditionalText versionPropsFile, CancellationToken cancellationToken) {
        var properties = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (versionPropsFile.GetText(cancellationToken) is not { } sourceText) {
            return properties;
        }

        try {
            var doc = XDocument.Parse(sourceText.ToString());

            // Find all elements inside PropertyGroup that define properties
            var propertyGroups = doc.Descendants()
                .Where(static e => e.Name.LocalName == "PropertyGroup");

            foreach (var propertyGroup in propertyGroups) {
                foreach (var element in propertyGroup.Elements()) {
                    // The element name IS the property name
                    properties.Add(element.Name.LocalName);
                }
            }
        } catch (Exception) {
            // XML parsing failed - return empty set
        }

        return properties;
    }

    private static void AnalyzePackagesPropsFile(
        CompilationAnalysisContext context,
        AdditionalText propsFile,
        HashSet<string> definedProperties) {
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

                // Check if this is a $(VariableName) reference
                var match = MsBuildPropertyPattern.Match(versionValue);
                if (!match.Success) {
                    continue; // Not a variable reference (AL0017 handles hardcoded versions)
                }

                var variableName = match.Groups[1].Value;

                // Skip if the variable is defined
                if (definedProperties.Contains(variableName)) {
                    continue;
                }

                // This is an undefined variable - report diagnostic
                var location = CreateLocation(propsFile, sourceText, versionAttr);

                var properties = ImmutableDictionary.CreateBuilder<string, string?>();
                properties.Add(VariableNameKey, variableName);
                properties.Add(PackageNameKey, packageName);

                var diagnostic = Diagnostic.Create(
                    Rule,
                    location,
                    properties.ToImmutable(),
                    packageName,
                    variableName);

                context.ReportDiagnostic(diagnostic);
            }
        } catch (Exception) {
            // XML parsing failed - silently ignore malformed files
        }
    }

    private static Location CreateLocation(AdditionalText propsFile, SourceText sourceText, XAttribute attribute) {
        System.Xml.IXmlLineInfo lineInfo = attribute;
        if (lineInfo.HasLineInfo()) {
            var linePosition = new LinePosition(lineInfo.LineNumber - 1, lineInfo.LinePosition - 1);
            var textSpan = sourceText.Lines[lineInfo.LineNumber - 1].Span;
            return Location.Create(propsFile.Path, textSpan,
                new LinePositionSpan(linePosition, linePosition));
        }

        return Location.None;
    }
}
