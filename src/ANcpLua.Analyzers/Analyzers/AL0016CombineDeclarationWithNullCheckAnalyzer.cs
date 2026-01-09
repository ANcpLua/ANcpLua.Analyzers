using ANcpLua.Analyzers.Core;

namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0016: Combine declaration with subsequent null-check.
///     Detects "var x = M(); if (x is null) return;" and suggests "if (M() is not { } x) return;".
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AL0016CombineDeclarationWithNullCheckAnalyzer : ALAnalyzer {
    public const string DiagnosticId = DiagnosticIds.CombineDeclarationWithNullCheck;

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Combine declaration with subsequent null-check",
        "Combine declaration of '{0}' with subsequent null-check",
        DiagnosticCategories.Style,
        DiagnosticSeverity.Info,
        true,
        "Combines a variable declaration and an immediate null-check into a single pattern match.",
        HelpLinkBase + "AL0016.md");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

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
        if (variable.Initializer == null) {
            return;
        }

        var variableName = variable.Identifier.Text;


        if (context.SemanticModel.GetDeclaredSymbol(variable) is not ILocalSymbol symbol ||
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

        if (ifStatement.Else != null) {
            return;
        }


        if (!IsNullCheck(ifStatement.Condition, variableName)) {
            return;
        }


        if (!IsEarlyExit(ifStatement.Statement)) {
            return;
        }


        if (ContainsNonNameofUsage(ifStatement.Statement, variableName)) {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(Rule, declaration.GetLocation(), variableName));
    }

    private static bool IsNullCheck(ExpressionSyntax condition, string name) {
        switch (condition) {
            case IsPatternExpressionSyntax {
                Pattern: ConstantPatternSyntax { Expression: LiteralExpressionSyntax l }
            } p
                when l.IsKind(SyntaxKind.NullLiteralExpression):
                return p.Expression is IdentifierNameSyntax id && id.Identifier.Text == name;


            case BinaryExpressionSyntax bin when bin.IsKind(SyntaxKind.EqualsExpression): {
                if (bin.Right.IsKind(SyntaxKind.NullLiteralExpression) && bin.Left is IdentifierNameSyntax lId) {
                    return lId.Identifier.Text == name;
                }

                if (bin.Left.IsKind(SyntaxKind.NullLiteralExpression) && bin.Right is IdentifierNameSyntax rId) {
                    return rId.Identifier.Text == name;
                }

                break;
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

    private static bool ContainsNonNameofUsage(SyntaxNode node, string name) =>
        node.DescendantNodes()
            .OfType<IdentifierNameSyntax>()
            .Where(id => id.Identifier.Text == name)
            .Any(id => id.Parent is not ArgumentSyntax {
                Parent: ArgumentListSyntax {
                    Parent: InvocationExpressionSyntax {
                        Expression: IdentifierNameSyntax { Identifier.Text: "nameof" }
                    }
                }
            });
}
