using ANcpLua.Analyzers.Core;

namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0046: Suggests using Guard.NotNullOrWhiteSpace() instead of if (string.IsNullOrWhiteSpace(x)) throw patterns.
/// </summary>
/// <remarks>
///     <list type="bullet">
///         <item><c>if (string.IsNullOrWhiteSpace(value)) throw new ArgumentNullException(nameof(value))</c> becomes <c>Guard.NotNullOrWhiteSpace(value)</c></item>
///         <item><c>if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException(...)</c> becomes <c>Guard.NotNullOrWhiteSpace(value)</c></item>
///     </list>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0046UseGuardNotNullOrWhiteSpaceAnalyzer : AlAnalyzer {
    /// <summary>The diagnostic identifier for AL0046.</summary>
    public const string DiagnosticId = "AL0046";

    private static readonly DiagnosticDescriptor Rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.RoslynUtilities,
        DiagnosticSeverities.Suggestion);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    /// <summary>Registers syntax or operation actions for analysis.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterSyntaxNodeAction(AnalyzeIfStatement, SyntaxKind.IfStatement);

    private static void AnalyzeIfStatement(SyntaxNodeAnalysisContext context) {
        var ifStatement = (IfStatementSyntax)context.Node;

        // Skip if there's an else clause (can't convert to guard)
        if (ifStatement.Else is not null) {
            return;
        }

        // Check if condition is string.IsNullOrWhiteSpace(x)
        if (!TryParseIsNullOrWhiteSpaceCheck(ifStatement.Condition, out var parameterName)) {
            return;
        }

        // Check if the then-branch throws ArgumentNullException or ArgumentException
        var throwStmt = TryGetThrowStatement(ifStatement.Statement);
        if (!IsArgumentNullOrArgumentExceptionThrow(throwStmt, context.SemanticModel)) {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(Rule, ifStatement.GetLocation(), parameterName));
    }

    private static bool TryParseIsNullOrWhiteSpaceCheck(ExpressionSyntax condition, out string parameterName) {
        parameterName = "";

        // Match: string.IsNullOrWhiteSpace(x)
        if (condition is InvocationExpressionSyntax {
            Expression: MemberAccessExpressionSyntax {
                Name.Identifier.Text: "IsNullOrWhiteSpace"
            } memberAccess,
            ArgumentList.Arguments.Count: 1
        } invocation) {
            // Check if it's called on string type (lowercase keyword or uppercase class name)
            var expression = memberAccess.Expression;
            if (expression is IdentifierNameSyntax { Identifier.Text: "String" }
                or PredefinedTypeSyntax { Keyword.RawKind: (int)SyntaxKind.StringKeyword }) {
                var argument = invocation.ArgumentList.Arguments[0].Expression;
                parameterName = GetExpressionName(argument);
                return !string.IsNullOrEmpty(parameterName);
            }
        }

        return false;
    }

    private static string GetExpressionName(ExpressionSyntax expression) =>
        expression switch {
            IdentifierNameSyntax identifier => identifier.Identifier.Text,
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.Text,
            _ => "value"
        };

    private static ThrowStatementSyntax? TryGetThrowStatement(StatementSyntax statement) =>
        statement switch {
            ThrowStatementSyntax t => t,
            BlockSyntax { Statements: [ThrowStatementSyntax t] } => t,
            _ => null
        };

    private static bool IsArgumentNullOrArgumentExceptionThrow(
        ThrowStatementSyntax? throwStmt,
        SemanticModel model) {
        if (throwStmt is null) {
            return false;
        }
        if (throwStmt.Expression is not ObjectCreationExpressionSyntax creation) {
            return false;
        }

        var typeSymbol = ModelExtensions.GetTypeInfo(model, creation.Type).Type;
        if (typeSymbol is null) {
            // Fall back to syntax-based check
            var typeName = creation.Type.ToString();
            return typeName is "ArgumentNullException" or "System.ArgumentNullException"
                   or "ArgumentException" or "System.ArgumentException";
        }

        var displayName = typeSymbol.ToDisplayString();
        return displayName is "System.ArgumentNullException" or "System.ArgumentException";
    }
}
