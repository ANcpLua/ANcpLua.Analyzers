using ANcpLua.Analyzers.Core;
using ANcpLua.Roslyn.Utilities;
using Microsoft.CodeAnalysis.CSharp;

namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0016: Combine declaration with subsequent null-check.
///     Detects a pattern where a local variable is declared and then immediately checked for null,
///     flagging them for potential combination into a single pattern match.
/// </summary>
/// <remarks>
///     Detects patterns like:
///     <list type="bullet">
///         <item><c>var x = M(); if (x is null) return;</c></item>
///         <item><c>var x = M(); if (x == null) return;</c></item>
///     </list>
///     Only triggers if:
///     - Declaration has exactly one variable and initializer
///     - Next statement is an if with null check (no else clause)
///     - If body is an early-exit (return/throw/continue/break or block with single early-exit)
///     - Compilation is C# 7.0+ (for pattern variables)
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AL0016CombineDeclarationWithNullCheckAnalyzer : ALAnalyzer
{
    public const string DiagnosticId = DiagnosticIds.CombineDeclarationWithNullCheck;

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Combine declaration with subsequent null-check",
        "Combine this declaration with the following null-check",
        DiagnosticCategories.Style,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "A local variable declaration can be combined with its immediately following null-check into a single pattern match.",
        helpLinkUri: HelpLinkBase + "AL0016.md");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    protected override void RegisterActions(AnalysisContext context)
    {
        context.RegisterSyntaxNodeAction(
            AnalyzeLocalDeclaration,
            SyntaxKind.LocalDeclarationStatement);
    }

    private static void AnalyzeLocalDeclaration(SyntaxNodeAnalysisContext context)
    {
        var declaration = (LocalDeclarationStatementSyntax)context.Node;

        // Gate on language version >= C# 7.0 (for pattern variables)
        if (!context.Compilation.HasLanguageVersionAtLeastEqualTo(LanguageVersion.CSharp7))
            return;

        // Verify declaration has exactly one variable
        if (declaration.Declaration.Variables.Count != 1)
            return;

        var variable = declaration.Declaration.Variables[0];

        // Verify variable has an initializer
        if (variable.Initializer is null)
            return;

        var variableName = variable.Identifier.Text;

        // Get the next sibling statement
        var nextStatement = TryGetNextStatement(declaration);
        if (nextStatement is null)
            return;

        // Verify next statement is an if with null check
        if (nextStatement is not IfStatementSyntax ifStatement)
            return;

        // Verify no else clause
        if (ifStatement.Else is not null)
            return;

        // Verify the if condition is a null check on the same variable
        if (!IsNullCheckIf(ifStatement, variableName))
            return;

        // Verify if body is an early-exit
        if (!IsEarlyExit(ifStatement.Statement))
            return;

        // All checks passed, report diagnostic
        context.ReportDiagnostic(Diagnostic.Create(
            Rule,
            declaration.GetLocation()));
    }

    /// <summary>
    /// Gets the next sibling statement after the current local declaration.
    /// </summary>
    private static StatementSyntax? TryGetNextStatement(LocalDeclarationStatementSyntax currentNode)
    {
        // Navigate up to find the containing block
        var containingBlock = currentNode.Parent as BlockSyntax;
        if (containingBlock is null)
            return null;

        // Find the index of the current statement
        var currentIndex = containingBlock.Statements.IndexOf(currentNode);
        if (currentIndex < 0 || currentIndex >= containingBlock.Statements.Count - 1)
            return null;

        // Return the next statement
        return containingBlock.Statements[currentIndex + 1];
    }

    /// <summary>
    /// Checks if an if statement is checking a variable for null.
    /// </summary>
    private static bool IsNullCheckIf(IfStatementSyntax ifStatement, string variableName)
    {
        return IsIdentifierNullCheck(ifStatement.Condition, out var identifierName) &&
               identifierName == variableName;
    }

    /// <summary>
    /// Checks if an expression is a null check and extracts the identifier name.
    /// Supports both "x is null" and "x == null" patterns.
    /// </summary>
    private static bool IsIdentifierNullCheck(ExpressionSyntax condition, out string identifierName)
    {
        identifierName = string.Empty;

        // Pattern: x is null
        if (condition is IsPatternExpressionSyntax isPattern)
        {
            if (isPattern.Pattern is not ConstantPatternSyntax constantPattern)
                return false;

            if (!constantPattern.Expression.IsKind(SyntaxKind.NullLiteralExpression))
                return false;

            // Get identifier from expression
            if (isPattern.Expression is not IdentifierNameSyntax identifierNameNode)
                return false;

            identifierName = identifierNameNode.Identifier.Text;
            return true;
        }

        // Pattern: x == null
        if (condition is BinaryExpressionSyntax binary)
        {
            // Only match ==, not !=
            if (!binary.IsKind(SyntaxKind.EqualsExpression))
                return false;

            bool expressionIsLeft;
            if (binary.Right.IsKind(SyntaxKind.NullLiteralExpression))
            {
                expressionIsLeft = true;
            }
            else if (binary.Left.IsKind(SyntaxKind.NullLiteralExpression))
            {
                expressionIsLeft = false;
            }
            else
            {
                return false;
            }

            var expr = expressionIsLeft ? binary.Left : binary.Right;

            // Must be a simple identifier
            if (expr is not IdentifierNameSyntax identifierNameNode)
                return false;

            identifierName = identifierNameNode.Identifier.Text;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Checks if a statement is an early-exit statement.
    /// Early-exit: return, throw, continue, break, or a block with exactly one of these.
    /// </summary>
    private static bool IsEarlyExit(StatementSyntax statement)
    {
        // Direct early-exit: return, throw, continue, break
        if (statement is ReturnStatementSyntax or ThrowStatementSyntax or ContinueStatementSyntax or BreakStatementSyntax)
            return true;

        // Block with single early-exit statement
        if (statement is BlockSyntax { Statements.Count: 1 } block)
        {
            return IsEarlyExit(block.Statements[0]);
        }

        return false;
    }
}
