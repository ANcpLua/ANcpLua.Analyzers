using ANcpLua.Analyzers.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

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

    private static readonly DiagnosticDescriptor Rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.RoslynUtilities,
        DiagnosticSeverities.Suggestion);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    /// <summary>Registers syntax actions for if statement analysis.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterSyntaxNodeAction(AnalyzeIfStatement, SyntaxKind.IfStatement);

    private static void AnalyzeIfStatement(SyntaxNodeAnalysisContext context) {
        var ifStatement = (IfStatementSyntax)context.Node;

        // Skip if statements with else branches - Guard.Positive doesn't handle else logic
        if (ifStatement.Else is not null) {
            return;
        }

        // Try to parse x <= 0 or 0 >= x pattern from the condition
        if (!TryParseLessThanOrEqualZeroCheck(ifStatement.Condition, out var identifier)) {
            return;
        }

        // Check if the if body throws ArgumentOutOfRangeException
        if (TryGetThrowStatement(ifStatement.Statement) is not { } throwStmt) {
            return;
        }

        if (!IsArgumentOutOfRangeExceptionThrow(throwStmt, context.SemanticModel)) {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(Rule, ifStatement.IfKeyword.GetLocation(), identifier));
    }

    /// <summary>
    ///     Parses the condition to check for x &lt;= 0 or 0 &gt;= x patterns.
    /// </summary>
    private static bool TryParseLessThanOrEqualZeroCheck(ExpressionSyntax condition, out string identifier) {
        identifier = "";

        // Handle x <= 0
        if (condition is BinaryExpressionSyntax { Left: var left, Right: var right } bin
            && bin.IsKind(SyntaxKind.LessThanOrEqualExpression)) {
            if (IsZeroLiteral(right) && TryGetIdentifier(left, out identifier)) {
                return true;
            }
        }

        // Handle 0 >= x (reversed comparison, equivalent to x <= 0)
        if (condition is BinaryExpressionSyntax { Left: var leftGe, Right: var rightGe } binGe
            && binGe.IsKind(SyntaxKind.GreaterThanOrEqualExpression)) {
            if (IsZeroLiteral(leftGe) && TryGetIdentifier(rightGe, out identifier)) {
                return true;
            }
        }

        return false;
    }

    private static bool IsZeroLiteral(ExpressionSyntax expression) =>
        expression switch {
            LiteralExpressionSyntax lit when lit.IsKind(SyntaxKind.NumericLiteralExpression)
                => lit.Token.Value is 0 or 0L or 0.0 or 0.0f or 0m or (short)0 or (byte)0,
            PrefixUnaryExpressionSyntax { Operand: LiteralExpressionSyntax innerLit } prefix
                when prefix.IsKind(SyntaxKind.UnaryMinusExpression)
                     && innerLit.IsKind(SyntaxKind.NumericLiteralExpression)
                => innerLit.Token.Value is 0 or 0L or 0.0 or 0.0f or 0m,
            _ => false
        };

    private static bool TryGetIdentifier(ExpressionSyntax expression, out string identifier) {
        identifier = "";

        switch (expression) {
            case IdentifierNameSyntax id:
                identifier = id.Identifier.Text;
                return true;
            case MemberAccessExpressionSyntax { Name: IdentifierNameSyntax memberId }:
                identifier = memberId.Identifier.Text;
                return true;
            default:
                return false;
        }
    }

    private static ThrowStatementSyntax? TryGetThrowStatement(StatementSyntax statement) =>
        statement switch {
            ThrowStatementSyntax t => t,
            BlockSyntax { Statements: [ThrowStatementSyntax t] } => t,
            _ => null
        };

    private static bool IsArgumentOutOfRangeExceptionThrow(
        ThrowStatementSyntax throwStmt,
        SemanticModel model) {
        if (throwStmt.Expression is not ObjectCreationExpressionSyntax creation) {
            return false;
        }

        var typeSymbol = ModelExtensions.GetTypeInfo(model, creation.Type).Type;
        var isArgumentOutOfRangeException = typeSymbol is not null
            ? typeSymbol.ToDisplayString() == "System.ArgumentOutOfRangeException"
            : creation.Type.ToString() is "ArgumentOutOfRangeException" or "System.ArgumentOutOfRangeException";

        return isArgumentOutOfRangeException;
    }
}
