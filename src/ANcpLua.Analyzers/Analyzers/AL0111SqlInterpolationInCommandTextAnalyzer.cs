
namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0111: Flags interpolated strings assigned to CommandText — SQL injection risk.
/// </summary>
/// <remarks>
///     <para>
///         Assigning an interpolated string (<c>$"..."</c> or <c>$"""..."""</c>) to a property
///         named <c>CommandText</c> is a SQL injection vector. Values should be passed via
///         parameterized queries (<c>@param</c>, <c>$1</c>) instead of being interpolated
///         directly into the command string.
///     </para>
///     <para>
///         This analyzer detects simple assignment expressions where the left-hand side is a
///         member access ending in <c>CommandText</c> and the right-hand side is an
///         <see cref="Microsoft.CodeAnalysis.CSharp.Syntax.InterpolatedStringExpressionSyntax"/>.
///     </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0111SqlInterpolationInCommandTextAnalyzer : AlAnalyzer {
    /// <summary>The diagnostic identifier for AL0111.</summary>
    public const string DiagnosticId = "AL0111";

    private static readonly DiagnosticDescriptor Rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.Reliability,
        DiagnosticSeverity.Warning);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    /// <summary>Registers a syntax node action on simple assignment expressions.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterSyntaxNodeAction(AnalyzeAssignment, SyntaxKind.SimpleAssignmentExpression);

    private static void AnalyzeAssignment(SyntaxNodeAnalysisContext context) {
        var assignment = (AssignmentExpressionSyntax)context.Node;

        // Left side must be a member access ending in CommandText
        if (assignment.Left is not MemberAccessExpressionSyntax { Name.Identifier.Text: "CommandText" }) {
            return;
        }

        // Right side must be an interpolated string (covers both $"..." and $"""...""")
        if (assignment.Right is not InterpolatedStringExpressionSyntax interpolatedString) {
            return;
        }

        context.ReportDiagnostic(Rule, interpolatedString.GetLocation());
    }
}
