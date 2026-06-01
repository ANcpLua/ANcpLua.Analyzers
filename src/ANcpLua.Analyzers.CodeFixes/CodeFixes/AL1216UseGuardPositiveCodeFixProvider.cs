using ANcpLua.Analyzers.Analyzers;

namespace ANcpLua.Analyzers.CodeFixes.CodeFixes;

/// <summary>
///     Code fix for AL1216: Converts if (x &lt;= 0) throw to Guard.Positive(x).
/// </summary>
/// <remarks>
///     <list type="bullet">
///         <item><c>if (x &lt;= 0) throw new ArgumentOutOfRangeException(...)</c> to <c>Guard.Positive(x);</c></item>
///         <item><c>if (0 &gt;= x) throw new ArgumentOutOfRangeException(...)</c> to <c>Guard.Positive(x);</c></item>
///     </list>
/// </remarks>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(Al1216UseGuardPositiveCodeFixProvider))]
[Shared]
public sealed partial class Al1216UseGuardPositiveCodeFixProvider
    : AlCodeFixProvider<IfStatementSyntax> {
    private const string ExtensionsNamespace = "ANcpLua.Roslyn.Utilities";

    private const string PropertyExpression = "Expression";

    /// <summary>Gets the diagnostic IDs this code fix can fix.</summary>
    public override ImmutableArray<string> FixableDiagnosticIds => [Al1216UseGuardPositiveAnalyzer.DiagnosticId];

    /// <summary>Creates the code action for this fix.</summary>
    protected override CodeAction CreateCodeAction(
        Document document,
        IfStatementSyntax ifStatement,
        SyntaxNode root,
        Diagnostic diagnostic) =>
        CodeAction.Create(
            CodeFixResources.AL1216CodeFixTitle,
            _ => ConvertToGuardPositive(document, ifStatement, root, GetExpressionFromDiagnostic(diagnostic, ifStatement)),
            Al1216UseGuardPositiveAnalyzer.DiagnosticId);

    private static Task<Document> ConvertToGuardPositive(
        Document document,
        IfStatementSyntax ifStatement,
        SyntaxNode root,
        ExpressionSyntax expression) {
        // Create: Guard.Positive(expression);
        var guardCall = SyntaxFactory.ExpressionStatement(
                SyntaxFactory.InvocationExpression(
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.IdentifierName("Guard"),
                            SyntaxFactory.IdentifierName("Positive")))
                    .WithArgumentList(
                        SyntaxFactory.ArgumentList(
                            SyntaxFactory.SingletonSeparatedList(
                                SyntaxFactory.Argument(
                                    expression)))))
            .WithLeadingTrivia(ifStatement.GetLeadingTrivia())
            .WithTrailingTrivia(ifStatement.GetTrailingTrivia());

        var newRoot = root.ReplaceNode(ifStatement, guardCall);
        newRoot = AddUsingIfMissing(newRoot, ExtensionsNamespace);
        return Task.FromResult(document.WithSyntaxRoot(newRoot));
    }

    private static ExpressionSyntax GetExpressionFromDiagnostic(
        Diagnostic diagnostic,
        IfStatementSyntax ifStatement) {
        if (diagnostic.Properties.TryGetValue(PropertyExpression, out var expressionText) &&
            expressionText is { } text &&
            !string.IsNullOrWhiteSpace(text)) {
            return SyntaxFactory.ParseExpression(text.Trim());
        }

        return TryGetCheckedExpression(ifStatement.Condition)
            ?? SyntaxFactory.IdentifierName("value");
    }

    private static ExpressionSyntax? TryGetCheckedExpression(ExpressionSyntax condition) {
        if (condition is BinaryExpressionSyntax { Left: var left, Right: var right } bin
            && bin.IsKind(SyntaxKind.LessThanOrEqualExpression)
            && IsZeroLiteral(right)) {
            return left;
        }

        if (condition is BinaryExpressionSyntax { Left: var leftGe, Right: var rightGe } binGe
            && binGe.IsKind(SyntaxKind.GreaterThanOrEqualExpression)
            && IsZeroLiteral(leftGe)) {
            return rightGe;
        }

        return null;
    }

    private static bool IsZeroLiteral(ExpressionSyntax expression) =>
        expression switch {
            LiteralExpressionSyntax lit when lit.IsKind(SyntaxKind.NumericLiteralExpression) =>
                lit.Token.Value is 0 or 0L or 0.0 or 0.0f or 0m or (short)0 or (byte)0,
            PrefixUnaryExpressionSyntax { Operand: LiteralExpressionSyntax innerLit } prefix
                when prefix.IsKind(SyntaxKind.UnaryMinusExpression)
                     && innerLit.IsKind(SyntaxKind.NumericLiteralExpression) =>
                innerLit.Token.Value is 0 or 0L or 0.0 or 0.0f or 0m,
            _ => false
        };

    private static SyntaxNode AddUsingIfMissing(SyntaxNode root, string namespaceName) {
        if (root is not CompilationUnitSyntax compilationUnit) {
            return root;
        }

        if (compilationUnit.Usings.Any(u => u.Name?.ToString() == namespaceName)) {
            return root;
        }

        var newUsing = SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(namespaceName))
            .WithTrailingTrivia(DetectEndOfLine(compilationUnit));

        return compilationUnit.AddUsings(newUsing);
    }

    private static SyntaxTrivia DetectEndOfLine(CompilationUnitSyntax compilationUnit) {
        // Preserve the file's CRLF/LF convention so the inserted using does not corrupt line endings.
        foreach (var trivia in compilationUnit.DescendantTrivia()) {
            if (trivia.IsKind(SyntaxKind.EndOfLineTrivia)) {
                return trivia;
            }
        }

        return SyntaxFactory.LineFeed;
    }
}
