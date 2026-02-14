using Microsoft.CodeAnalysis.Text;
using System.Text.RegularExpressions;

namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0054-AL0056: Validates alignment between Descriptors.cs and documentation files.
///     Ensures all diagnostics are properly documented in diagnostics.md and AnalyzerReleases.*.md.
/// </summary>
/// <remarks>
///     <para>
///         This analyzer validates that analyzer projects maintain consistency between:
///         <list type="bullet">
///             <item>Descriptors.cs - The source of truth for diagnostic definitions</item>
///             <item>docs/diagnostics.md - User-facing documentation</item>
///             <item>AnalyzerReleases.Shipped.md / Unshipped.md - Release tracking</item>
///         </list>
///     </para>
///     <para>
///         To use this analyzer, add the following files as AdditionalFiles in your .csproj:
///         <code>
///         &lt;ItemGroup&gt;
///             &lt;AdditionalFiles Include="Analyzers/Descriptors.cs" /&gt;
///             &lt;AdditionalFiles Include="../docs/diagnostics.md" /&gt;
///             &lt;AdditionalFiles Include="AnalyzerReleases.Shipped.md" /&gt;
///             &lt;AdditionalFiles Include="AnalyzerReleases.Unshipped.md" /&gt;
///         &lt;/ItemGroup&gt;
///         </code>
///     </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0054ToAl0056DiagnosticsAlignmentAnalyzer : DiagnosticAnalyzer {
    /// <summary>The diagnostic identifier for AL0054.</summary>
    public const string DiagnosticIdAL0054 = "AL0054";
    /// <summary>The diagnostic identifier for AL0055.</summary>
    public const string DiagnosticIdAL0055 = "AL0055";
    /// <summary>The diagnostic identifier for AL0056.</summary>
    public const string DiagnosticIdAL0056 = "AL0056";

    private const string DescriptorsFileName = "Descriptors.cs";
    private const string DiagnosticsMdFileName = "diagnostics.md";
    private const string ShippedFileName = "AnalyzerReleases.Shipped.md";
    private const string UnshippedFileName = "AnalyzerReleases.Unshipped.md";

    private static readonly Regex DocHeadingRegex = new(
        @"^###\s+(?<id>[A-Z]+\d+)\s+-\s+(?<title>.+)$",
        RegexOptions.Compiled | RegexOptions.Multiline);

    private static readonly Regex DocSeverityRegex = new(
        @"^\*\*Severity:\*\*\s+(?<severity>\w+)",
        RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);

    private static readonly Regex RuleIdRegex = new(
        @"^[A-Z]+\d+$",
        RegexOptions.Compiled);

    private static readonly DiagnosticDescriptor RuleMissingDocs = new(
        DiagnosticIdAL0054,
        "Diagnostic missing from documentation",
        "{0}",
        DiagnosticCategories.VersionManagement,
        DiagnosticSeverity.Warning,
        true,
        "Diagnostics defined in Descriptors.cs should be documented in diagnostics.md.",
        AlAnalyzer.HelpLinkBase,
        WellKnownDiagnosticTags.CompilationEnd);

    private static readonly DiagnosticDescriptor RuleMissingRelease = new(
        DiagnosticIdAL0055,
        "Diagnostic missing from release notes",
        "{0}",
        DiagnosticCategories.VersionManagement,
        DiagnosticSeverity.Warning,
        true,
        "Diagnostics defined in Descriptors.cs should be tracked in AnalyzerReleases.*.md.",
        AlAnalyzer.HelpLinkBase,
        WellKnownDiagnosticTags.CompilationEnd);

    private static readonly DiagnosticDescriptor RuleMismatch = new(
        DiagnosticIdAL0056,
        "Diagnostic documentation mismatch",
        "{0}",
        DiagnosticCategories.VersionManagement,
        DiagnosticSeverity.Warning,
        true,
        "Diagnostic metadata should be consistent between Descriptors.cs and documentation.",
        AlAnalyzer.HelpLinkBase,
        WellKnownDiagnosticTags.CompilationEnd);

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [RuleMissingDocs, RuleMissingRelease, RuleMismatch];

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context) {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationAction(AnalyzeCompilation);
    }

    private static void AnalyzeCompilation(CompilationAnalysisContext context) {
        var descriptorsFile = FindFile(context.Options.AdditionalFiles, DescriptorsFileName);
        var docsFile = FindFile(context.Options.AdditionalFiles, DiagnosticsMdFileName);
        var shippedFile = FindFile(context.Options.AdditionalFiles, ShippedFileName);
        var unshippedFile = FindFile(context.Options.AdditionalFiles, UnshippedFileName);

        // Need at least Descriptors.cs and one documentation file
        if (descriptorsFile is null) {
            return;
        }

        var descriptors = ParseDescriptors(descriptorsFile, context.CancellationToken);
        if (descriptors.Count is 0) {
            return;
        }

        var docs = docsFile is not null
            ? ParseDocs(docsFile, context.CancellationToken)
            : new Dictionary<string, DocInfo>(StringComparer.Ordinal);

        var releases = ParseReleases(shippedFile, unshippedFile, context.CancellationToken);

        foreach (var descriptor in descriptors.Values) {
            // Check docs
            if (docsFile is not null) {
                if (!docs.TryGetValue(descriptor.Id, out var docInfo)) {
                    ReportDiagnostic(context, RuleMissingDocs, descriptorsFile,
                        $"{descriptor.Id} is missing from {DiagnosticsMdFileName}");
                } else {
                    if (!descriptor.Title.EqualsOrdinal(docInfo.Title)) {
                        ReportDiagnostic(context, RuleMismatch, descriptorsFile,
                            $"{descriptor.Id} title mismatch: Descriptors='{descriptor.Title}' Docs='{docInfo.Title}'");
                    }
                    if (!descriptor.Severity.EqualsIgnoreCase(docInfo.Severity)) {
                        ReportDiagnostic(context, RuleMismatch, descriptorsFile,
                            $"{descriptor.Id} severity mismatch: Descriptors='{descriptor.Severity}' Docs='{docInfo.Severity}'");
                    }
                }
            }

            // Check release notes
            if (shippedFile is not null || unshippedFile is not null) {
                if (!releases.TryGetValue(descriptor.Id, out var releaseInfo)) {
                    ReportDiagnostic(context, RuleMissingRelease, descriptorsFile,
                        $"{descriptor.Id} is missing from AnalyzerReleases.*.md");
                } else {
                    if (!descriptor.Category.EqualsOrdinal(releaseInfo.Category)) {
                        ReportDiagnostic(context, RuleMismatch, descriptorsFile,
                            $"{descriptor.Id} category mismatch: Descriptors='{descriptor.Category}' Release='{releaseInfo.Category}'");
                    }
                    if (!descriptor.Severity.EqualsIgnoreCase(releaseInfo.Severity)) {
                        ReportDiagnostic(context, RuleMismatch, descriptorsFile,
                            $"{descriptor.Id} severity mismatch: Descriptors='{descriptor.Severity}' Release='{releaseInfo.Severity}'");
                    }
                }
            }
        }

        // Check for orphaned docs/release entries
        foreach (var docId in docs.Keys) {
            if (!descriptors.ContainsKey(docId)) {
                ReportDiagnostic(context, RuleMismatch, docsFile!,
                    $"{docId} in {DiagnosticsMdFileName} has no corresponding descriptor");
            }
        }

        foreach (var releaseId in releases.Keys) {
            if (!descriptors.ContainsKey(releaseId)) {
                ReportDiagnostic(context, RuleMismatch, shippedFile ?? unshippedFile!,
                    $"{releaseId} in release notes has no corresponding descriptor");
            }
        }
    }

    private static AdditionalText? FindFile(ImmutableArray<AdditionalText> files, string fileName) {
        foreach (var file in files) {
            if (file.Path.EndsWithIgnoreCase(fileName)) {
                return file;
            }
        }
        return null;
    }

    private static void ReportDiagnostic(
        CompilationAnalysisContext context,
        DiagnosticDescriptor rule,
        AdditionalText file,
        string message) {
        if (file.GetText(context.CancellationToken) is { } text) {
            var location = Location.Create(file.Path, text.Lines[0].Span,
                new LinePositionSpan(new LinePosition(0, 0), new LinePosition(0, 0)));
            context.ReportDiagnostic(Diagnostic.Create(rule, location, message));
        } else {
            context.ReportDiagnostic(Diagnostic.Create(rule, Location.None, message));
        }
    }

    private static Dictionary<string, DescriptorInfo> ParseDescriptors(
        AdditionalText file,
        CancellationToken ct) {
        var result = new Dictionary<string, DescriptorInfo>(StringComparer.Ordinal);
        if (file.GetText(ct) is not { } text) {
            return result;
        }

        var tree = CSharpSyntaxTree.ParseText(text, cancellationToken: ct);
        var root = tree.GetRoot(ct);
        var constants = ExtractStringConstants(root);

        foreach (var field in root.DescendantNodes().OfType<FieldDeclarationSyntax>()) {
            if (!IsDiagnosticDescriptorType(field.Declaration.Type)) {
                continue;
            }

            foreach (var variable in field.Declaration.Variables) {
                if (GetArgumentList(variable.Initializer?.Value) is not { Arguments.Count: >= 5 } arguments) {
                    continue;
                }

                var args = arguments.Arguments;
                var id = GetStringLiteral(args[0].Expression);
                var title = GetStringLiteral(args[1].Expression);
                var category = ResolveString(args[3].Expression, constants);
                var severity = ResolveSeverity(args[4].Expression);

                if (id is null || title is null || category is null || severity is null) {
                    continue;
                }

                if (!RuleIdRegex.IsMatch(id)) {
                    continue;
                }

                // netstandard2.0 compatible: use ContainsKey + Add instead of TryAdd
                if (!result.ContainsKey(id)) {
                    result.Add(id, new DescriptorInfo(id, title, category, severity));
                }
            }
        }

        return result;
    }

    private static Dictionary<string, string> ExtractStringConstants(SyntaxNode root) {
        var constants = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var field in root.DescendantNodes().OfType<FieldDeclarationSyntax>()) {
            if (!field.Modifiers.Any(SyntaxKind.ConstKeyword)) {
                continue;
            }

            if (field.Declaration.Type is not PredefinedTypeSyntax { Keyword.RawKind: (int)SyntaxKind.StringKeyword }) {
                continue;
            }

            foreach (var variable in field.Declaration.Variables) {
                if (variable.Initializer?.Value is LiteralExpressionSyntax literal &&
                    literal.IsKind(SyntaxKind.StringLiteralExpression)) {
                    constants[variable.Identifier.Text] = literal.Token.ValueText;
                }
            }
        }
        return constants;
    }

    private static bool IsDiagnosticDescriptorType(TypeSyntax type) {
        return type switch {
            IdentifierNameSyntax { Identifier.Text: "DiagnosticDescriptor" } => true,
            QualifiedNameSyntax { Right.Identifier.Text: "DiagnosticDescriptor" } => true,
            _ => false
        };
    }

    private static ArgumentListSyntax? GetArgumentList(ExpressionSyntax? expression) {
        return expression switch {
            ObjectCreationExpressionSyntax creation => creation.ArgumentList,
            ImplicitObjectCreationExpressionSyntax implicitCreation => implicitCreation.ArgumentList,
            _ => null
        };
    }

    private static string? GetStringLiteral(ExpressionSyntax expression) {
        return expression is LiteralExpressionSyntax literal && literal.IsKind(SyntaxKind.StringLiteralExpression)
            ? literal.Token.ValueText
            : null;
    }

    private static string? ResolveString(ExpressionSyntax expression, Dictionary<string, string> constants) {
        if (expression is LiteralExpressionSyntax literal && literal.IsKind(SyntaxKind.StringLiteralExpression)) {
            return literal.Token.ValueText;
        }

        if (expression is IdentifierNameSyntax identifier &&
            constants.TryGetValue(identifier.Identifier.Text, out var value)) {
            return value;
        }

        if (expression is MemberAccessExpressionSyntax member &&
            constants.TryGetValue(member.Name.Identifier.Text, out var memberValue)) {
            return memberValue;
        }

        return null;
    }

    private static string? ResolveSeverity(ExpressionSyntax expression) {
        var name = expression switch {
            MemberAccessExpressionSyntax member => member.Name.Identifier.Text,
            IdentifierNameSyntax identifier => identifier.Identifier.Text,
            _ => null
        };

        return name switch {
            "Error" => "Error",
            "Warning" => "Warning",
            "Info" => "Info",
            "Hidden" => "Hidden",
            _ => null
        };
    }

    private static Dictionary<string, DocInfo> ParseDocs(AdditionalText file, CancellationToken ct) {
        var result = new Dictionary<string, DocInfo>(StringComparer.Ordinal);
        if (file.GetText(ct) is not { } text) {
            return result;
        }

        var content = text.ToString();
        var headings = DocHeadingRegex.Matches(content);

        for (var i = 0; i < headings.Count; i++) {
            var match = headings[i];
            var id = match.Groups["id"].Value.Trim();
            var title = match.Groups["title"].Value.Trim();

            if (!RuleIdRegex.IsMatch(id)) {
                continue;
            }

            var start = match.Index;
            var end = i + 1 < headings.Count ? headings[i + 1].Index : content.Length;
            var block = content.Substring(start, end - start);

            var severityMatch = DocSeverityRegex.Match(block);
            var severity = severityMatch.Success ? NormalizeSeverity(severityMatch.Groups["severity"].Value) : "Unknown";

            // netstandard2.0 compatible: use ContainsKey + Add instead of TryAdd
            if (!result.ContainsKey(id)) {
                result.Add(id, new DocInfo(title, severity));
            }
        }

        return result;
    }

    private static Dictionary<string, ReleaseInfo> ParseReleases(
        AdditionalText? shippedFile,
        AdditionalText? unshippedFile,
        CancellationToken ct) {
        var result = new Dictionary<string, ReleaseInfo>(StringComparer.Ordinal);

        if (shippedFile is not null) {
            ParseReleaseFile(shippedFile, result, ct);
        }
        if (unshippedFile is not null) {
            ParseReleaseFile(unshippedFile, result, ct);
        }

        return result;
    }

    private static void ParseReleaseFile(
        AdditionalText file,
        Dictionary<string, ReleaseInfo> result,
        CancellationToken ct) {
        if (file.GetText(ct) is not { } text) {
            return;
        }

        var section = ReleaseSection.None;
        var inTable = false;

        foreach (var line in text.Lines) {
            var content = line.ToString().Trim();

            if (content.StartsWithOrdinal("### ")) {
                section = content switch {
                    "### New Rules" => ReleaseSection.New,
                    "### Removed Rules" => ReleaseSection.Removed,
                    "### Changed Rules" => ReleaseSection.Changed,
                    _ => ReleaseSection.None
                };
                inTable = false;
                continue;
            }

            if (content.StartsWithOrdinal("Rule ID")) {
                inTable = true;
                continue;
            }

            if (string.IsNullOrWhiteSpace(content) || content.StartsWithOrdinal("-")) {
                inTable = false;
                continue;
            }

            if (!inTable || !content.ContainsOrdinal("|")) {
                continue;
            }

            // netstandard2.0 compatible: manual split and trim
            var parts = content.Split('|');
            if (parts.Length < 4) {
                continue;
            }

            var id = parts[0].Trim();
            if (!RuleIdRegex.IsMatch(id)) {
                continue;
            }

            if (section is ReleaseSection.Removed) {
                continue;
            }

            var category = parts[1].Trim();
            var severity = NormalizeSeverity(parts[2]);

            // netstandard2.0 compatible: use ContainsKey + Add instead of TryAdd
            if (!result.ContainsKey(id)) {
                result.Add(id, new ReleaseInfo(category, severity));
            }
        }
    }

    private static string NormalizeSeverity(string value) {
        return value.Trim() switch {
            "Error" => "Error",
            "Warning" => "Warning",
            "Info" => "Info",
            "Hidden" => "Hidden",
            "Suggestion" => "Warning",
            _ => value.Trim()
        };
    }

    private enum ReleaseSection { None, New, Removed, Changed }

    private sealed partial record DescriptorInfo(string Id, string Title, string Category, string Severity);
    private sealed partial record DocInfo(string Title, string Severity);
    private sealed partial record ReleaseInfo(string Category, string Severity);
}
