using ANcpLua.Analyzers.Core;

namespace ANcpLua.Analyzers.CodeFixes.CodeFixes;

/// <summary>
///     Code fix for AL0030: Converts type hierarchy loops to extension methods.
/// </summary>
/// <remarks>
///     <c>foreach (var iface in type.AllInterfaces) if (Equals(iface, target)) ...</c> → <c>type.Implements(target)</c>
///     <c>while (base != null) { if (Equals(base, target)) ... base = base.BaseType; }</c> →
///     <c>type.InheritsFrom(target)</c>
/// </remarks>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(Al0030UseTypeHierarchyCodeFixProvider))]
[Shared]
public sealed partial class Al0030UseTypeHierarchyCodeFixProvider : CodeFixProvider {
    public override ImmutableArray<string> FixableDiagnosticIds => [DiagnosticIds.UseTypeHierarchyExtensions];

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context) {
        if (await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false) is not { } root) {
            return;
        }

        var diagnostic = context.Diagnostics.First();
        var diagnosticSpan = diagnostic.Location.SourceSpan;
        var node = root.FindNode(diagnosticSpan);

        // Handle foreach over AllInterfaces
        if (node is ForEachStatementSyntax forEachStatement &&
            TryExtractAllInterfacesInfo(forEachStatement, out var typeExpr, out var targetExpr)) {
            context.RegisterCodeFix(
                CodeAction.Create(
                    CodeFixResources.AL0030ImplementsCodeFixTitle,
                    _ => ConvertToImplements(context.Document, root, forEachStatement, typeExpr, targetExpr),
                    nameof(Al0030UseTypeHierarchyCodeFixProvider) + "_Implements"),
                diagnostic);
        }

        // Handle while loop over BaseType
        if (node is WhileStatementSyntax whileStatement &&
            TryExtractBaseTypeInfo(whileStatement, out var baseTypeExpr, out var baseTargetExpr)) {
            context.RegisterCodeFix(
                CodeAction.Create(
                    CodeFixResources.AL0030InheritsFromCodeFixTitle,
                    _ => ConvertToInheritsFrom(context.Document, root, whileStatement, baseTypeExpr, baseTargetExpr),
                    nameof(Al0030UseTypeHierarchyCodeFixProvider) + "_InheritsFrom"),
                diagnostic);
        }
    }

    private static bool TryExtractAllInterfacesInfo(
        ForEachStatementSyntax forEach,
        [NotNullWhen(true)] out ExpressionSyntax? typeExpr,
        [NotNullWhen(true)] out ExpressionSyntax? targetExpr) {
        typeExpr = null;
        targetExpr = null;

        // Pattern: foreach (var iface in type.AllInterfaces)
        if (forEach.Expression is not MemberAccessExpressionSyntax { Name.Identifier.Text: "AllInterfaces" } allInterfacesAccess) {
            return false;
        }

        typeExpr = allInterfacesAccess.Expression;

        // Find the comparison target in the body
        targetExpr = FindComparisonTarget(forEach.Statement, forEach.Identifier.Text);

        return targetExpr is not null;
    }

    private static bool TryExtractBaseTypeInfo(
        WhileStatementSyntax whileLoop,
        [NotNullWhen(true)] out ExpressionSyntax? typeExpr,
        [NotNullWhen(true)] out ExpressionSyntax? targetExpr) {
        typeExpr = null;
        targetExpr = null;

        // Find the original type variable from the BaseType assignment pattern
        // Look for: current = current.BaseType or similar
        var baseTypeAssignment = whileLoop.Statement.DescendantNodes()
            .OfType<AssignmentExpressionSyntax>()
            .FirstOrDefault(static a => a.Right is MemberAccessExpressionSyntax { Name.Identifier.Text: "BaseType" });

        if (baseTypeAssignment?.Left is not IdentifierNameSyntax currentVar) {
            return false;
        }

        // The type expression is what we're walking from - find it in an ancestor or prior statement
        // For now, just use the current variable as a placeholder
        typeExpr = currentVar;

        // Find the comparison target
        targetExpr = FindComparisonTarget(whileLoop.Statement, currentVar.Identifier.Text);

        return targetExpr is not null;
    }

    private static ExpressionSyntax? FindComparisonTarget(StatementSyntax body, string iteratorName) {
        foreach (var invocation in body.DescendantNodes().OfType<InvocationExpressionSyntax>()) {
            if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess) {
                continue;
            }

            // Pattern 1: SymbolEqualityComparer.*.Equals(iterator, target)
            if (memberAccess.Name.Identifier.Text == "Equals" &&
                memberAccess.Expression is MemberAccessExpressionSyntax {
                    Expression: IdentifierNameSyntax { Identifier.Text: "SymbolEqualityComparer" }
                } &&
                invocation.ArgumentList.Arguments.Count == 2) {
                var arg0 = invocation.ArgumentList.Arguments[0].Expression;
                var arg1 = invocation.ArgumentList.Arguments[1].Expression;

                if (arg0 is IdentifierNameSyntax { Identifier.Text: var name0 } && name0 == iteratorName) {
                    return arg1;
                }

                if (arg1 is IdentifierNameSyntax { Identifier.Text: var name1 } && name1 == iteratorName) {
                    return arg0;
                }
            }

            // Pattern 2: iterator.IsEqualTo(target) from ANcpLua.Roslyn.Utilities
            if (memberAccess.Name.Identifier.Text == "IsEqualTo" &&
                memberAccess.Expression is IdentifierNameSyntax { Identifier.Text: var receiverName } &&
                receiverName == iteratorName &&
                invocation.ArgumentList.Arguments.Count == 1) {
                return invocation.ArgumentList.Arguments[0].Expression;
            }
        }

        return null;
    }

    private static Task<Document> ConvertToImplements(
        Document document,
        SyntaxNode root,
        ForEachStatementSyntax forEach,
        ExpressionSyntax typeExpr,
        ExpressionSyntax targetExpr) {
        // Create: type.Implements(target)
        var newExpression = CreateExtensionCall(typeExpr, "Implements", targetExpr);

        // Replace the foreach with a return statement containing the new expression
        var newStatement = SyntaxFactory.ReturnStatement(newExpression)
            .WithTriviaFrom(forEach);

        var newRoot = root.ReplaceNode(forEach, newStatement);

        // Remove any following "return false/null;" statement that would now be unreachable
        newRoot = RemoveFollowingReturnStatement(newRoot, newStatement);

        return Task.FromResult(document.WithSyntaxRoot(newRoot));
    }

    private static Task<Document> ConvertToInheritsFrom(
        Document document,
        SyntaxNode root,
        WhileStatementSyntax whileLoop,
        ExpressionSyntax typeExpr,
        ExpressionSyntax targetExpr) {
        // Create: type.InheritsFrom(target)
        var newExpression = CreateExtensionCall(typeExpr, "InheritsFrom", targetExpr);

        // Replace the while with a return statement containing the new expression
        var newStatement = SyntaxFactory.ReturnStatement(newExpression)
            .WithTriviaFrom(whileLoop);

        var newRoot = root.ReplaceNode(whileLoop, newStatement);

        // Remove any following "return false/null;" statement that would now be unreachable
        newRoot = RemoveFollowingReturnStatement(newRoot, newStatement);

        return Task.FromResult(document.WithSyntaxRoot(newRoot));
    }

    private static SyntaxNode RemoveFollowingReturnStatement(SyntaxNode root, ReturnStatementSyntax newStatement) {
        // Find the new statement in the updated tree
        var statementInNewTree = root.DescendantNodes()
            .OfType<ReturnStatementSyntax>()
            .FirstOrDefault(s => s.IsEquivalentTo(newStatement));

        if (statementInNewTree?.Parent is not BlockSyntax block) {
            return root;
        }

        var statements = block.Statements;
        var index = statements.IndexOf(statementInNewTree);

        // Check if there's a following return statement with false/null/default
        if (index >= 0 && index + 1 < statements.Count &&
            statements[index + 1] is ReturnStatementSyntax followingReturn &&
            IsDefaultReturnValue(followingReturn.Expression)) {
            return root.RemoveNode(followingReturn, SyntaxRemoveOptions.KeepNoTrivia) ?? root;
        }

        return root;
    }

    private static bool IsDefaultReturnValue(ExpressionSyntax? expression) =>
        expression switch {
            LiteralExpressionSyntax literal => literal.Kind() is SyntaxKind.FalseLiteralExpression
                or SyntaxKind.NullLiteralExpression
                or SyntaxKind.DefaultLiteralExpression,
            DefaultExpressionSyntax => true,
            _ => false
        };

    private static InvocationExpressionSyntax CreateExtensionCall(
        ExpressionSyntax receiver,
        string methodName,
        ExpressionSyntax argument) =>
        SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                receiver.WithoutTrivia(),
                SyntaxFactory.IdentifierName(methodName)),
            SyntaxFactory.ArgumentList(
                SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.Argument(argument.WithoutTrivia()))));
}
