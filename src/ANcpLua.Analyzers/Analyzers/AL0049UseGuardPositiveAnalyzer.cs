
namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0049: Suggests using Guard.Positive() instead of if (x &lt;= 0) throw patterns.
/// </summary>
/// <remarks>
///     <list type="bullet">
///         <item><c>if (x &lt;= 0) throw new ArgumentOutOfRangeException(...)</c> to <c>Guard.Positive(x)</c></item>
///         <item><c>if (0 &gt;= x) throw new ArgumentOutOfRangeException(...)</c> to <c>Guard.Positive(x)</c></item>
///     </list>
///     <para>
///         IMPORTANT: Only matches <c>x &lt;= 0</c> pattern. Does NOT match <c>x &lt; 0</c> (that's AL0048 NotNegative).
///     </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0049UseGuardPositiveAnalyzer : AlAnalyzer {
    /// <summary>The diagnostic identifier for AL0049.</summary>
    public const string DiagnosticId = "AL0049";

    private static readonly DiagnosticDescriptor s_rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.RoslynUtilities,
        DiagnosticSeverities.Suggestion);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_rule];

    /// <summary>Registers syntax actions for if statement analysis.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterSyntaxNodeAction(AnalyzeIfStatement, SyntaxKind.IfStatement);

    private static void AnalyzeIfStatement(SyntaxNodeAnalysisContext context) {
        var ifStatement = (IfStatementSyntax)context.Node;

        if (ifStatement.Else is not null) {
            return;
        }

        if (!TryParseLessThanOrEqualZeroCheck(ifStatement.Condition, out var identifier)) {
            return;
        }

        if (TryGetThrowStatement(ifStatement.Statement) is not { } throwStmt) {
            return;
        }

        if (!IsArgumentOutOfRangeExceptionThrow(throwStmt, context.SemanticModel)) {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(s_rule, ifStatement.IfKeyword.GetLocation(), identifier));
    }

    private static bool TryParseLessThanOrEqualZeroCheck(ExpressionSyntax condition, out string identifier) {
        identifier = "";

        return condition switch {
            BinaryExpressionSyntax { Left: var left, Right: var right } bin
                when bin.IsKind(SyntaxKind.LessThanOrEqualExpression)
                     && IsZeroLiteral(right) && TryGetIdentifier(left, out identifier) => true,
            BinaryExpressionSyntax { Left: var left2, Right: var right2 } bin2
                when bin2.IsKind(SyntaxKind.GreaterThanOrEqualExpression)
                     && IsZeroLiteral(left2) && TryGetIdentifier(right2, out identifier) => true,
            _ => false
        };
    }

    private static bool IsZeroLiteral(ExpressionSyntax expression) =>
        expression switch {
            LiteralExpressionSyntax lit when lit.IsKind(SyntaxKind.NumericLiteralExpression) =>
                lit.Token.Value is 0 or 0L or 0.0 or 0.0f or 0m or (short)0 or (byte)0,
            PrefixUnaryExpressionSyntax { Operand: LiteralExpressionSyntax innerLit } prefix
                when prefix.IsKind(SyntaxKind.UnaryMinusExpression)
                     && innerLit.IsKind(SyntaxKind.NumericLiteralExpression) =>
                innerLit.Token.Value is 0 or 0L or 0.0 or 0.0f or 0m,
            _ => false
        };

    private static bool TryGetIdentifier(ExpressionSyntax expression, out string identifier) {
        identifier = expression switch {
            IdentifierNameSyntax id => id.Identifier.Text,
            MemberAccessExpressionSyntax { Name: IdentifierNameSyntax memberId } => memberId.Identifier.Text,
            _ => ""
        };
        return identifier.Length > 0;
    }

    private static ThrowStatementSyntax? TryGetThrowStatement(StatementSyntax statement) =>
        statement switch {
            ThrowStatementSyntax t => t,
            BlockSyntax { Statements: [ThrowStatementSyntax t] } => t,
            _ => null
        };

    private static bool IsArgumentOutOfRangeExceptionThrow(ThrowStatementSyntax throwStmt, SemanticModel model) {
        if (throwStmt.Expression is not ObjectCreationExpressionSyntax creation) {
            return false;
        }

        var typeName = ModelExtensions.GetTypeInfo(model, creation.Type).Type?.ToDisplayString()
                       ?? creation.Type.ToString();
        return typeName is "System.ArgumentOutOfRangeException" or "ArgumentOutOfRangeException";
    }
}
