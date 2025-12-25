using ANcpLua.Analyzers.Core;
using Microsoft.CodeAnalysis.Text;

namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0002: Don't repeat negated patterns (not not not...).
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AL0002DontRepeatNegatedPatternAnalyzer : ALAnalyzer
{
    public const string DiagnosticId = DiagnosticIds.DontRepeatNegatedPattern;

    private static readonly LocalizableResourceString Title = new(
        nameof(Resources.AL0002AnalyzerTitle), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableResourceString MessageFormat = new(
        nameof(Resources.AL0002AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableResourceString Description = new(
        nameof(Resources.AL0002AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId, Title, MessageFormat, DiagnosticCategories.Design,
        DiagnosticSeverity.Warning, isEnabledByDefault: true, Description,
        HelpLinkBase + "AL0002.md");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    protected override void RegisterActions(AnalysisContext context)
    {
        context.RegisterSyntaxNodeAction(AnalyzeNotPattern, SyntaxKind.NotPattern);
    }

    private static void AnalyzeNotPattern(SyntaxNodeAnalysisContext context)
    {
        var syntax = (UnaryPatternSyntax)context.Node;

        // Sub pattern must be another negated pattern
        if (syntax.Pattern is not UnaryPatternSyntax)
            return;

        // Skip if parent is already a negated pattern (already handled)
        if (syntax.Parent is UnaryPatternSyntax)
            return;

        var innerNode = syntax.DescendantNodes()
            .FirstOrDefault(n => n is not UnaryPatternSyntax);

        if (innerNode is null)
            return;

        var firstLocation = syntax.SpanStart;
        var nonFirstLocation = innerNode.SpanStart;

        context.ReportDiagnostic(Rule,
            Location.Create(syntax.SyntaxTree, TextSpan.FromBounds(firstLocation, nonFirstLocation - 1)));
    }
}