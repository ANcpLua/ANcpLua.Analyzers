using ANcpLua.Analyzers.Core;
using Microsoft.CodeAnalysis.Text;

namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0002: Don't repeat negated patterns (not not not...).
/// </summary>
/// <remarks>
///     <para>
///         Repeated negation patterns like <c>not not x</c> or <c>not not not y</c>
///         are confusing and hard to reason about. Each additional negation flips
///         the boolean logic, making code review and maintenance error-prone.
///     </para>
///     <para>
///         The analyzer reports only the outermost negation sequence and highlights
///         the chain of <c>not</c> keywords, allowing a code fix to simplify the
///         expression to either a single <c>not</c> or no negation at all.
///     </para>
///     <para>
///         A single negation (<c>not null</c>) is valid and not flagged. Only
///         consecutive negations are considered problematic.
///     </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0002DontRepeatNegatedPatternAnalyzer : AlAnalyzer {
    /// <summary>AL0002: Don't repeat negated patterns.</summary>
    public const string DiagnosticId = DiagnosticIds.DontRepeatNegatedPattern;

    private static readonly LocalizableResourceString Title = new(
        nameof(Resources.AL0002AnalyzerTitle), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableResourceString MessageFormat = new(
        nameof(Resources.AL0002AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableResourceString Description = new(
        nameof(Resources.AL0002AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId, Title, MessageFormat, DiagnosticCategories.Design,
        DiagnosticSeverity.Warning, true, Description,
        HelpLinkBase);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    /// <summary>Registers syntax node actions to analyze negated patterns.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterSyntaxNodeAction(AnalyzeNotPattern, SyntaxKind.NotPattern);

    private static void AnalyzeNotPattern(SyntaxNodeAnalysisContext context) {
        var syntax = (UnaryPatternSyntax)context.Node;

        if (syntax.Pattern is not UnaryPatternSyntax) {
            return;
        }

        if (syntax.Parent is UnaryPatternSyntax) {
            return;
        }

        if (syntax.DescendantNodes().FirstOrDefault(static n => n is not UnaryPatternSyntax) is not { } innerNode) {
            return;
        }

        var firstLocation = syntax.SpanStart;
        var nonFirstLocation = innerNode.SpanStart;

        var spanEnd = Math.Max(firstLocation + 1, nonFirstLocation - 1);

        context.ReportDiagnostic(Rule,
            Location.Create(syntax.SyntaxTree, TextSpan.FromBounds(firstLocation, spanEnd)));
    }
}
