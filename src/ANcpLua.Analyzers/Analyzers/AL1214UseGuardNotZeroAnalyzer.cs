
namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL1214: Suggests using Guard.NotZero() instead of if (x == 0) throw ArgumentOutOfRangeException patterns.
/// </summary>
/// <remarks>
///     <list type="bullet">
///         <item><c>if (x == 0) throw new ArgumentOutOfRangeException(...)</c> becomes <c>Guard.NotZero(x)</c></item>
///         <item><c>if (0 == x) throw new ArgumentOutOfRangeException(...)</c> becomes <c>Guard.NotZero(x)</c></item>
///     </list>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al1214UseGuardNotZeroAnalyzer : AlAnalyzer {
    /// <summary>The diagnostic identifier for AL1214.</summary>
    public const string DiagnosticId = "AL1214";

    private const string GuardMetadataName = "ANcpLua.Roslyn.Utilities.Guard";

    private static readonly DiagnosticDescriptor s_rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.RoslynUtilities,
        DiagnosticSeverities.Suggestion);

    /// <summary>Property key for the parameter identifier.</summary>
    public const string PropertyIdentifier = "Id";

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_rule];

    /// <summary>Registers syntax or operation actions for analysis.</summary>
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

        if (!TryParseZeroCheck(ifStatement.Condition, out var identifier)) {
            return;
        }

        if (TryGetThrowStatement(ifStatement.Statement) is not { } throwStmt) {
            return;
        }

        if (!IsArgumentOutOfRangeExceptionThrow(throwStmt, context.SemanticModel)) {
            return;
        }

        var properties = ImmutableDictionary.CreateBuilder<string, string?>();
        properties.Add(PropertyIdentifier, identifier);

        context.ReportDiagnostic(Diagnostic.Create(
            s_rule,
            ifStatement.IfKeyword.GetLocation(),
            properties.ToImmutable(),
            identifier));
    }

    private static bool TryParseZeroCheck(ExpressionSyntax condition, out string identifier) {
        identifier = "";

        switch (condition)
        {
            // Handle: x == 0 or 0 == x
            // Check for: x == 0
            case BinaryExpressionSyntax { RawKind: (int)SyntaxKind.EqualsExpression } binary when IsZeroLiteral(binary.Right) && binary.Left is IdentifierNameSyntax leftId:
                identifier = leftId.Identifier.Text;
                return true;
            // Check for: 0 == x
            case BinaryExpressionSyntax { RawKind: (int)SyntaxKind.EqualsExpression } binary when IsZeroLiteral(binary.Left) && binary.Right is IdentifierNameSyntax rightId:
                identifier = rightId.Identifier.Text;
                return true;
            // Handle: x is 0
            case IsPatternExpressionSyntax {
                Expression: IdentifierNameSyntax patternId,
                Pattern: ConstantPatternSyntax { Expression: LiteralExpressionSyntax literal }
            } when IsZeroLiteral(literal):
                identifier = patternId.Identifier.Text;
                return true;
            default:
                return false;
        }
    }

    private static bool IsZeroLiteral(ExpressionSyntax expression) {
        if (expression is not LiteralExpressionSyntax literal) {
            return false;
        }

        return literal.Token.Value switch {
            0 or 0L or 0UL or 0U => true,
            0.0 or 0.0f or 0.0m => true,
            _ => false
        };
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
