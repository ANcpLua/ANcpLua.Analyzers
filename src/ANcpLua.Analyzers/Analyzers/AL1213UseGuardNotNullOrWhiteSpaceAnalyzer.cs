
namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL1213: Suggests using Guard.NotNullOrWhiteSpace() instead of if (string.IsNullOrWhiteSpace(x)) throw patterns.
/// </summary>
/// <remarks>
///     <list type="bullet">
///         <item><c>if (string.IsNullOrWhiteSpace(value)) throw new ArgumentNullException(nameof(value))</c> becomes <c>Guard.NotNullOrWhiteSpace(value)</c></item>
///         <item><c>if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException(...)</c> becomes <c>Guard.NotNullOrWhiteSpace(value)</c></item>
///     </list>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al1213UseGuardNotNullOrWhiteSpaceAnalyzer : AlAnalyzer {
    /// <summary>The diagnostic identifier for AL1213.</summary>
    public const string DiagnosticId = "AL1213";

    private static readonly DiagnosticDescriptor s_rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.RoslynUtilities,
        DiagnosticSeverities.Suggestion);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_rule];

    /// <summary>Registers syntax or operation actions for analysis.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterSyntaxNodeAction(AnalyzeIfStatement, SyntaxKind.IfStatement);

    private static void AnalyzeIfStatement(SyntaxNodeAnalysisContext context) {
        var ifStatement = (IfStatementSyntax)context.Node;

        if (ifStatement.Else is not null ||
            !TryParseIsNullOrWhiteSpaceCheck(ifStatement.Condition, out var parameterName) ||
            !IsArgumentNullOrArgumentExceptionThrow(TryGetThrowStatement(ifStatement.Statement), context.SemanticModel)) {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(s_rule, ifStatement.GetLocation(), parameterName));
    }

    private static bool TryParseIsNullOrWhiteSpaceCheck(ExpressionSyntax condition, out string parameterName) {
        parameterName = "";

        if (condition is InvocationExpressionSyntax {
            Expression: MemberAccessExpressionSyntax {
                Name.Identifier.Text: "IsNullOrWhiteSpace",
                Expression: IdentifierNameSyntax { Identifier.Text: "String" }
                    or PredefinedTypeSyntax { Keyword.RawKind: (int)SyntaxKind.StringKeyword }
            },
            ArgumentList.Arguments.Count: 1
        } invocation) {
            parameterName = GetExpressionName(invocation.ArgumentList.Arguments[0].Expression);
            return !string.IsNullOrEmpty(parameterName);
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
        if (throwStmt?.Expression is not ObjectCreationExpressionSyntax creation) {
            return false;
        }

        var typeSymbol = ModelExtensions.GetTypeInfo(model, creation.Type).Type;
        if (typeSymbol is null) {
            var typeName = creation.Type.ToString();
            return typeName is "ArgumentNullException" or "System.ArgumentNullException"
                   or "ArgumentException" or "System.ArgumentException";
        }

        return typeSymbol.ToDisplayString() is "System.ArgumentNullException" or "System.ArgumentException";
    }
}
