using ANcpLua.Analyzers.Core;

namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0045: Suggests using Guard.NotNullOrEmpty() instead of if/throw patterns.
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
public sealed partial class Al0045UseGuardNotNullOrEmptyAnalyzer : AlAnalyzer {
    /// <summary>The diagnostic identifier for AL0045.</summary>
    public const string DiagnosticId = "AL0045";

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
        if (context.Node is not IfStatementSyntax ifStatement) {
            return;
        }

        // Check if condition is string.IsNullOrEmpty(...)
        if (!IsStringIsNullOrEmptyCall(ifStatement.Condition, out var argumentName)) {
            return;
        }

        // Skip if there's an else branch - the pattern is not a simple guard
        if (ifStatement.Else is not null) {
            return;
        }

        // Check if the body contains a throw statement for ArgumentNullException or ArgumentException
        if (!IsArgumentExceptionThrow(ifStatement.Statement)) {
            return;
        }

        // Report diagnostic on the entire if statement
        context.ReportDiagnostic(Diagnostic.Create(Rule, ifStatement.GetLocation(), argumentName));
    }

    private static bool IsStringIsNullOrEmptyCall(ExpressionSyntax condition, out string argumentName) {
        argumentName = "value";

        // Unwrap parentheses
        while (condition is ParenthesizedExpressionSyntax paren) {
            condition = paren.Expression;
        }

        // Check for string.IsNullOrEmpty(x) pattern
        if (condition is not InvocationExpressionSyntax invocation) {
            return false;
        }

        // Check the method being called
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess) {
            return false;
        }

        // Check if it's string.IsNullOrEmpty
        if (memberAccess.Name.Identifier.Text != "IsNullOrEmpty") {
            return false;
        }

        // Check if the receiver is "string" or "String" (type name)
        // In Roslyn, "string" keyword is a PredefinedTypeSyntax, while "String" is IdentifierNameSyntax
        var isStringType = memberAccess.Expression switch {
            PredefinedTypeSyntax predefined => predefined.Keyword.IsKind(SyntaxKind.StringKeyword),
            IdentifierNameSyntax identifier => identifier.Identifier.Text == "String",
            _ => false
        };

        if (!isStringType) {
            return false;
        }

        // Get the argument name
        if (invocation.ArgumentList.Arguments.Count != 1) {
            return false;
        }

        var arg = invocation.ArgumentList.Arguments[0].Expression;
        argumentName = GetExpressionName(arg);

        return true;
    }

    private static bool IsArgumentExceptionThrow(StatementSyntax statement) {
        // Handle block with single statement
        if (statement is BlockSyntax block) {
            if (block.Statements.Count != 1) {
                return false;
            }

            statement = block.Statements[0];
        }

        // Check if it's a throw statement
        if (statement is not ThrowStatementSyntax throwStatement || throwStatement.Expression is null) {
            return false;
        }

        // Check if throwing ArgumentNullException or ArgumentException
        if (throwStatement.Expression is not ObjectCreationExpressionSyntax objectCreation) {
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
