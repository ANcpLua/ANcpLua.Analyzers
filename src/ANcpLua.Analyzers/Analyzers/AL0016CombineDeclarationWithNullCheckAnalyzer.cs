
namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0016: Combine declaration with subsequent null-check.
///     Detects "var x = M(); if (x is null) return;" and suggests "if (M() is not { } x) return;".
/// </summary>
/// <remarks>
///     <para>
///         The pattern of declaring a variable, checking it for null, and returning early
///         can be expressed more concisely using C# 9's pattern matching with variable
///         declaration. The combined form <c>if (M() is not { } x) return;</c> declares
///         the variable and checks for null in a single statement.
///     </para>
///     <para>
///         The analyzer only triggers when: the declaration has exactly one variable with
///         an initializer, the next statement is an if-statement checking that variable
///         for null, the if-body is an early exit (return, throw, break, continue), and
///         the variable is not used within the if-body (except in <c>nameof</c> expressions).
///     </para>
///     <para>
///         This rule requires C# 9 or later. It only applies to reference types since
///         value types cannot be null. The analyzer does not fire if the if-statement
///         has an else clause, as the combined pattern does not naturally express that.
///     </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0016CombineDeclarationWithNullCheckAnalyzer : AlAnalyzer {
    /// <summary>AL0016: Combine declaration with subsequent null-check.</summary>
    public const string DiagnosticId = "AL0016";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Combine declaration with subsequent null-check",
        "Combine declaration of '{0}' with subsequent null-check",
        DiagnosticCategories.Style,
        DiagnosticSeverity.Info,
        true,
        "Combines a variable declaration and an immediate null-check into a single pattern match.",
        HelpLinkBase);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    /// <summary>Registers syntax node actions to analyze local variable declarations.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterSyntaxNodeAction(AnalyzeDeclaration, SyntaxKind.LocalDeclarationStatement);

    private static void AnalyzeDeclaration(SyntaxNodeAnalysisContext context) {
        if (((CSharpCompilation)context.Compilation).LanguageVersion < LanguageVersion.CSharp9) {
            return;
        }

        var declaration = (LocalDeclarationStatementSyntax)context.Node;

        if (declaration.Declaration.Variables.Count != 1) {
            return;
        }

        var variable = declaration.Declaration.Variables[0];
        if (variable.Initializer is null) {
            return;
        }

        var variableName = variable.Identifier.Text;

        if (ModelExtensions.GetDeclaredSymbol(context.SemanticModel, variable) is not ILocalSymbol symbol ||
            !symbol.Type.IsReferenceType) {
            return;
        }

        if (declaration.Parent is not BlockSyntax block) {
            return;
        }

        var index = block.Statements.IndexOf(declaration);
        if (index == -1 || index + 1 >= block.Statements.Count) {
            return;
        }

        if (block.Statements[index + 1] is not IfStatementSyntax ifStatement) {
            return;
        }

        if (ifStatement.Else is not null) {
            return;
        }

        if (!IsNullCheck(ifStatement.Condition, variableName, context.SemanticModel)) {
            return;
        }

        if (!IsEarlyExit(ifStatement.Statement)) {
            return;
        }

        if (ContainsNonNameofUsage(ifStatement.Statement, variableName, context.SemanticModel)) {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(Rule, declaration.GetLocation(), variableName));
    }

    private static bool IsNullCheck(ExpressionSyntax condition, string name, SemanticModel semanticModel) {
        switch (condition) {
            case IsPatternExpressionSyntax { Pattern: ConstantPatternSyntax { Expression: var expr } } pattern
                when expr.IsKind(SyntaxKind.NullLiteralExpression):
                return pattern.Expression is IdentifierNameSyntax id && id.Identifier.Text == name;

            case BinaryExpressionSyntax bin when bin.IsKind(SyntaxKind.EqualsExpression): {
                // Use semantic model to get operations for null check
                if (semanticModel.GetOperation(bin) is not IBinaryOperation operation) {
                    return false;
                }

                // Check if this is an equality comparison with null
                var leftIsNull = operation.LeftOperand.IsConstantNull();
                var rightIsNull = operation.RightOperand.IsConstantNull();

                if (!leftIsNull && !rightIsNull) {
                    return false;
                }

                // Verify the non-null side is our variable
                var variableSide = leftIsNull ? bin.Right : bin.Left;
                return variableSide is IdentifierNameSyntax identifier && identifier.Identifier.Text == name;
            }
        }

        return false;
    }

    private static bool IsEarlyExit(StatementSyntax stmt) {
        while (true) {
            if (stmt is not BlockSyntax { Statements.Count: 1 } b) {
                return stmt.Kind() is SyntaxKind.ReturnStatement or SyntaxKind.ThrowStatement
                    or SyntaxKind.BreakStatement or SyntaxKind.ContinueStatement;
            }

            stmt = b.Statements[0];
        }
    }

    private static bool ContainsNonNameofUsage(SyntaxNode node, string name, SemanticModel semanticModel) {
        foreach (var identifier in node.DescendantNodes().OfType<IdentifierNameSyntax>()) {
            if (identifier.Identifier.Text != name) {
                continue;
            }

            // Get the operation for this identifier to check if it's inside nameof
            var operation = semanticModel.GetOperation(identifier);
            if (operation is null) {
                // Fallback to syntax-based check
                if (!IsInsideNameofSyntax(identifier)) {
                    return true;
                }

                continue;
            }

            // Use OperationExtensions.IsInNameofOperation()
            if (!operation.IsInNameofOperation()) {
                return true;
            }
        }

        return false;
    }

    private static bool IsInsideNameofSyntax(SyntaxNode node) =>
        node.Parent is ArgumentSyntax {
            Parent: ArgumentListSyntax {
                Parent: InvocationExpressionSyntax {
                    Expression: IdentifierNameSyntax { Identifier.Text: "nameof" }
                }
            }
        };
}
