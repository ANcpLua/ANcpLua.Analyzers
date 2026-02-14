using ANcpLua.Analyzers.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0047: Suggests using Guard.NotZero() instead of if (x == 0) throw ArgumentOutOfRangeException patterns.
/// </summary>
/// <remarks>
///     <list type="bullet">
///         <item><c>if (x == 0) throw new ArgumentOutOfRangeException(...)</c> becomes <c>Guard.NotZero(x)</c></item>
///         <item><c>if (0 == x) throw new ArgumentOutOfRangeException(...)</c> becomes <c>Guard.NotZero(x)</c></item>
///     </list>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0047UseGuardNotZeroAnalyzer : AlAnalyzer {
    /// <summary>The diagnostic identifier for AL0047.</summary>
    public const string DiagnosticId = "AL0047";

    private static readonly DiagnosticDescriptor Rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.RoslynUtilities,
        DiagnosticSeverities.Suggestion);

    /// <summary>Property key for the parameter identifier.</summary>
    public const string PropertyIdentifier = "Id";

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    /// <summary>Registers syntax or operation actions for analysis.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterSyntaxNodeAction(AnalyzeIfStatement, SyntaxKind.IfStatement);

    private static void AnalyzeIfStatement(SyntaxNodeAnalysisContext context) {
        var ifStatement = (IfStatementSyntax)context.Node;

        // Skip if there's an else clause - Guard.NotZero only replaces simple throw patterns
        if (ifStatement.Else is not null) {
            return;
        }

        // Check if condition is a zero comparison
        if (!TryParseZeroCheck(ifStatement.Condition, out var identifier)) {
            return;
        }

        // Check if the body is a throw statement
        if (TryGetThrowStatement(ifStatement.Statement) is not { } throwStmt) {
            return;
        }

        // Check if it throws ArgumentOutOfRangeException
        if (!IsArgumentOutOfRangeExceptionThrow(throwStmt, context.SemanticModel)) {
            return;
        }

        var properties = ImmutableDictionary.CreateBuilder<string, string?>();
        properties.Add(PropertyIdentifier, identifier);

        context.ReportDiagnostic(Diagnostic.Create(
            Rule,
            ifStatement.IfKeyword.GetLocation(),
            properties.ToImmutable(),
            identifier));
    }

    private static bool TryParseZeroCheck(ExpressionSyntax condition, out string identifier) {
        identifier = "";

        // Handle: x == 0 or 0 == x
        if (condition is BinaryExpressionSyntax { RawKind: (int)SyntaxKind.EqualsExpression } binary) {
            // Check for: x == 0
            if (IsZeroLiteral(binary.Right) && binary.Left is IdentifierNameSyntax leftId) {
                identifier = leftId.Identifier.Text;
                return true;
            }

            // Check for: 0 == x
            if (IsZeroLiteral(binary.Left) && binary.Right is IdentifierNameSyntax rightId) {
                identifier = rightId.Identifier.Text;
                return true;
            }
        }

        // Handle: x is 0
        if (condition is IsPatternExpressionSyntax {
                Expression: IdentifierNameSyntax patternId,
                Pattern: ConstantPatternSyntax { Expression: LiteralExpressionSyntax literal }
            } && IsZeroLiteral(literal)) {
            identifier = patternId.Identifier.Text;
            return true;
        }

        return false;
    }

    private static bool IsZeroLiteral(ExpressionSyntax expression) {
        if (expression is not LiteralExpressionSyntax literal) {
            return false;
        }

        // Check for numeric zero literals (int, long, double, decimal, etc.)
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

    private static bool IsArgumentOutOfRangeExceptionThrow(
        ThrowStatementSyntax throwStmt,
        SemanticModel model) {
        if (throwStmt.Expression is not ObjectCreationExpressionSyntax creation) {
            return false;
        }

        var typeSymbol = ModelExtensions.GetTypeInfo(model, creation.Type).Type;
        if (typeSymbol is null) {
            // Fallback to string comparison if symbol resolution fails
            var typeName = creation.Type.ToString();
            return typeName is "ArgumentOutOfRangeException" or "System.ArgumentOutOfRangeException";
        }

        return typeSymbol.ToDisplayString() == "System.ArgumentOutOfRangeException";
    }
}
