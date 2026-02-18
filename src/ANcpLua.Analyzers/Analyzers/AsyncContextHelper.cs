
namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     Shared helper for determining if a syntax node is inside an async execution context.
/// </summary>
internal static partial class AsyncContextHelper {
    /// <summary>Determines if a syntax node is inside an async method, lambda, or local function.</summary>
    internal static bool IsInsideAsyncContext(SyntaxNode node) {
        for (var current = node.Parent; current is not null; current = current.Parent) {
            switch (current) {
                case MethodDeclarationSyntax method:
                    return method.Modifiers.Any(SyntaxKind.AsyncKeyword);
                case LocalFunctionStatementSyntax localFunction:
                    return localFunction.Modifiers.Any(SyntaxKind.AsyncKeyword);
                case AnonymousFunctionExpressionSyntax lambda:
                    return lambda.AsyncKeyword.IsKind(SyntaxKind.AsyncKeyword);
                case GlobalStatementSyntax:
                    // Top-level statements are only async when the compilation unit
                    // contains at least one await keyword (await expr, await foreach,
                    // await using). Without it, the compiler generates a synchronous
                    // entry point and await-based constructs would not compile.
                    return HasAwaitInTopLevelStatements(current);
            }
        }

        return false;
    }

    private static bool HasAwaitInTopLevelStatements(SyntaxNode globalStatement) {
        if (globalStatement.Parent is not CompilationUnitSyntax compilationUnit) {
            return false;
        }

        foreach (var member in compilationUnit.Members) {
            if (member is GlobalStatementSyntax gs &&
                gs.DescendantTokens().Any(static t => t.IsKind(SyntaxKind.AwaitKeyword))) {
                return true;
            }
        }

        return false;
    }
}
