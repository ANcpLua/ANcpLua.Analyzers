using ANcpLua.Analyzers.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0050: Suggests using Guard.NotEmpty() instead of if (guid == Guid.Empty) throw patterns.
/// </summary>
/// <remarks>
///     <list type="bullet">
///         <item><c>if (id == Guid.Empty) throw new ArgumentException(...)</c> -> <c>Guard.NotEmpty(id)</c></item>
///         <item><c>if (Guid.Empty == id) throw new ArgumentException(...)</c> -> <c>Guard.NotEmpty(id)</c></item>
///     </list>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0050UseGuardNotEmptyGuidAnalyzer : AlAnalyzer {
    /// <summary>The diagnostic identifier for AL0050.</summary>
    public const string DiagnosticId = "AL0050";

    private static readonly DiagnosticDescriptor Rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.RoslynUtilities,
        DiagnosticSeverities.Suggestion);

    /// <summary>Property key for the Guid identifier.</summary>
    public const string PropertyIdentifier = "Id";

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    /// <summary>Registers syntax actions for analysis.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterSyntaxNodeAction(AnalyzeIfStatement, SyntaxKind.IfStatement);

    private static void AnalyzeIfStatement(SyntaxNodeAnalysisContext context) {
        var ifStatement = (IfStatementSyntax)context.Node;

        // Don't report for if statements with else clauses
        if (ifStatement.Else is not null) {
            return;
        }

        // Check if the condition is comparing a Guid to Guid.Empty
        if (!TryParseGuidEmptyCheck(ifStatement.Condition, context.SemanticModel, out var identifier)) {
            return;
        }

        // Check if the body is a throw statement with ArgumentException
        if (TryGetThrowStatement(ifStatement.Statement) is not { } throwStmt) {
            return;
        }

        if (!IsArgumentExceptionThrow(throwStmt, context.SemanticModel)) {
            return;
        }

        var properties = ImmutableDictionary.CreateBuilder<string, string?>();
        properties.Add(PropertyIdentifier, identifier);

        context.ReportDiagnostic(Diagnostic.Create(
            Rule,
            ifStatement.GetLocation(),
            properties.ToImmutable(),
            identifier));
    }

    private static bool TryParseGuidEmptyCheck(
        ExpressionSyntax condition,
        SemanticModel model,
        out string identifier) {
        identifier = "";

        // Handle: guid == Guid.Empty or Guid.Empty == guid
        if (condition is not BinaryExpressionSyntax { RawKind: (int)SyntaxKind.EqualsExpression } binary) {
            return false;
        }

        // Check both sides for the pattern
        if (IsGuidEmpty(binary.Right, model) && TryGetGuidIdentifier(binary.Left, model, out identifier)) {
            return true;
        }

        if (IsGuidEmpty(binary.Left, model) && TryGetGuidIdentifier(binary.Right, model, out identifier)) {
            return true;
        }

        return false;
    }

    private static bool IsGuidEmpty(ExpressionSyntax expression, SemanticModel model) {
        // Check for Guid.Empty
        if (expression is MemberAccessExpressionSyntax {
                Name.Identifier.Text: "Empty"
            } memberAccess) {
            var typeInfo = ModelExtensions.GetTypeInfo(model, memberAccess.Expression);
            if (typeInfo.Type?.ToDisplayString() == "System.Guid") {
                return true;
            }

            // Fallback: check by name pattern
            if (memberAccess.Expression is IdentifierNameSyntax { Identifier.Text: "Guid" }) {
                return true;
            }
        }

        return false;
    }

    private static bool TryGetGuidIdentifier(
        ExpressionSyntax expression,
        SemanticModel model,
        out string identifier) {
        identifier = "";

        // Check that it's a Guid type
        var typeInfo = ModelExtensions.GetTypeInfo(model, expression);
        if (typeInfo.Type?.ToDisplayString() != "System.Guid") {
            return false;
        }

        identifier = expression switch {
            IdentifierNameSyntax id => id.Identifier.Text,
            MemberAccessExpressionSyntax { Name: IdentifierNameSyntax memberId } => memberId.Identifier.Text,
            _ => ""
        };

        return !string.IsNullOrEmpty(identifier);
    }

    private static ThrowStatementSyntax? TryGetThrowStatement(StatementSyntax statement) =>
        statement switch {
            ThrowStatementSyntax t => t,
            BlockSyntax { Statements: [ThrowStatementSyntax t] } => t,
            _ => null
        };

    private static bool IsArgumentExceptionThrow(ThrowStatementSyntax throwStmt, SemanticModel model) {
        if (throwStmt.Expression is not ObjectCreationExpressionSyntax creation) {
            return false;
        }

        var typeSymbol = ModelExtensions.GetTypeInfo(model, creation.Type).Type;
        if (typeSymbol is null) {
            // Fallback to string comparison
            var typeName = creation.Type.ToString();
            return typeName is "ArgumentException" or "System.ArgumentException"
                or "ArgumentNullException" or "System.ArgumentNullException";
        }

        var displayName = typeSymbol.ToDisplayString();
        return displayName is "System.ArgumentException" or "System.ArgumentNullException";
    }
}
