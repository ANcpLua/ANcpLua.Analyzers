
namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL1216: Suggests using Guard.Positive() instead of if (x &lt;= 0) throw patterns.
/// </summary>
/// <remarks>
///     <list type="bullet">
///         <item><c>if (x &lt;= 0) throw new ArgumentOutOfRangeException(...)</c> to <c>Guard.Positive(x)</c></item>
///         <item><c>if (0 &gt;= x) throw new ArgumentOutOfRangeException(...)</c> to <c>Guard.Positive(x)</c></item>
///     </list>
///     <para>
///         IMPORTANT: Only matches <c>x &lt;= 0</c> pattern. Does NOT match <c>x &lt; 0</c> (that's AL1215 NotNegative).
///     </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al1216UseGuardPositiveAnalyzer : AlAnalyzer {
    /// <summary>The diagnostic identifier for AL1216.</summary>
    public const string DiagnosticId = "AL1216";

    private const string GuardMetadataName = "ANcpLua.Roslyn.Utilities.Guard";

    private static readonly DiagnosticDescriptor s_rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.RoslynUtilities,
        DiagnosticSeverities.Suggestion);

    private const string PropertyExpression = "Expression";

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_rule];

    /// <summary>Registers syntax actions for if statement analysis.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterCompilationStartAction(OnCompilationStart);

    private static void OnCompilationStart(CompilationStartAnalysisContext context) {
        // Guard.* lives in ANcpLua.Roslyn.Utilities.Guard. Only fire when that type is present and
        // callable from this compilation; otherwise the code fix would rewrite to a symbol the
        // consumer cannot resolve. Projects that do not reference ANcpLua.Roslyn.Utilities are unaffected.
        if (context.Compilation.GetTypeByMetadataName(GuardMetadataName) is not { } guardType) {
            return;
        }

        if (!context.Compilation.IsSymbolAccessibleWithin(guardType, context.Compilation.Assembly)) {
            return;
        }

        context.RegisterSyntaxNodeAction(AnalyzeIfStatement, SyntaxKind.IfStatement);
    }

    private static void AnalyzeIfStatement(SyntaxNodeAnalysisContext context) {
        var ifStatement = (IfStatementSyntax)context.Node;

        if (ifStatement.Else is not null) {
            return;
        }

        if (!TryParseLessThanOrEqualZeroCheck(ifStatement.Condition, out var expression)) {
            return;
        }

        if (TryGetThrowStatement(ifStatement.Statement) is not { } throwStmt) {
            return;
        }

        if (!IsArgumentOutOfRangeExceptionThrow(throwStmt, context.SemanticModel)) {
            return;
        }

        var expressionText = expression.WithoutTrivia().ToString();
        var properties = ImmutableDictionary.CreateBuilder<string, string?>();
        properties.Add(PropertyExpression, expressionText);

        context.ReportDiagnostic(Diagnostic.Create(
            s_rule,
            ifStatement.IfKeyword.GetLocation(),
            properties.ToImmutable(),
            expressionText));
    }

    private static bool TryParseLessThanOrEqualZeroCheck(ExpressionSyntax condition, out ExpressionSyntax expression) {
        expression = null!;

        return condition switch {
            BinaryExpressionSyntax { Left: var left, Right: var right } bin
                when bin.IsKind(SyntaxKind.LessThanOrEqualExpression)
                     && IsZeroLiteral(right) && TryGetCheckedExpression(left, out expression) => true,
            BinaryExpressionSyntax { Left: var left2, Right: var right2 } bin2
                when bin2.IsKind(SyntaxKind.GreaterThanOrEqualExpression)
                     && IsZeroLiteral(left2) && TryGetCheckedExpression(right2, out expression) => true,
            _ => false
        };
    }

    private static bool TryGetCheckedExpression(ExpressionSyntax expression, out ExpressionSyntax checkedExpression) {
        checkedExpression = expression switch {
            IdentifierNameSyntax => expression,
            MemberAccessExpressionSyntax { Name: IdentifierNameSyntax } => expression,
            _ => null!
        };

        return checkedExpression is not null;
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
