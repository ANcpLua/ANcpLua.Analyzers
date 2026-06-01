
namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL1212: Suggests using Guard.NotNullOrEmpty() instead of if/throw patterns.
/// </summary>
/// <remarks>
///     <list type="bullet">
///         <item>
///             <c>if (string.IsNullOrEmpty(value)) throw new ArgumentNullException(nameof(value))</c>
///             to <c>Guard.NotNullOrEmpty(value)</c>
///         </item>
///         <item>
///             <c>if (string.IsNullOrEmpty(value)) throw new ArgumentException(...)</c>
///             to <c>Guard.NotNullOrEmpty(value)</c>
///         </item>
///     </list>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al1212UseGuardNotNullOrEmptyAnalyzer : AlAnalyzer {
    /// <summary>The diagnostic identifier for AL1212.</summary>
    public const string DiagnosticId = "AL1212";

    private const string GuardMetadataName = "ANcpLua.Roslyn.Utilities.Guard";

    private static readonly DiagnosticDescriptor s_rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.RoslynUtilities,
        DiagnosticSeverities.Suggestion);

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
        if (context.Node is not IfStatementSyntax { Else: null } ifStatement) {
            return;
        }

        if (!IsStringIsNullOrEmptyCall(ifStatement.Condition, out var argumentName)) {
            return;
        }

        if (!IsArgumentExceptionThrow(ifStatement.Statement)) {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(s_rule, ifStatement.GetLocation(), argumentName));
    }

    private static bool IsStringIsNullOrEmptyCall(ExpressionSyntax condition, out string argumentName) {
        argumentName = "value";

        while (condition is ParenthesizedExpressionSyntax paren) {
            condition = paren.Expression;
        }

        if (condition is not InvocationExpressionSyntax {
                Expression: MemberAccessExpressionSyntax { Name.Identifier.Text: "IsNullOrEmpty" } memberAccess,
                ArgumentList.Arguments: [{ Expression: var arg }]
            }) {
            return false;
        }

        var isStringType = memberAccess.Expression switch {
            PredefinedTypeSyntax predefined => predefined.Keyword.IsKind(SyntaxKind.StringKeyword),
            IdentifierNameSyntax { Identifier.Text: "String" } => true,
            _ => false
        };

        if (!isStringType) {
            return false;
        }

        argumentName = GetExpressionName(arg);
        return true;
    }

    private static bool IsArgumentExceptionThrow(StatementSyntax statement) {
        if (statement is BlockSyntax { Statements: [var single] }) {
            statement = single;
        }

        if (statement is not ThrowStatementSyntax { Expression: ObjectCreationExpressionSyntax objectCreation }) {
            return false;
        }

        var typeName = GetTypeName(objectCreation.Type);
        return typeName is "ArgumentNullException" or "ArgumentException" or
            "System.ArgumentNullException" or "System.ArgumentException";
    }

    private static string GetTypeName(TypeSyntax type) =>
        type switch {
            IdentifierNameSyntax identifier => identifier.Identifier.Text,
            QualifiedNameSyntax qualified => $"{GetTypeName(qualified.Left)}.{qualified.Right.Identifier.Text}",
            _ => string.Empty
        };

    private static string GetExpressionName(ExpressionSyntax expression) =>
        expression switch {
            IdentifierNameSyntax identifier => identifier.Identifier.Text,
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.Text,
            _ => "value"
        };
}
