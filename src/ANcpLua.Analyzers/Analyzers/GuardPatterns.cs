namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     Shared matching for the AL12xx <c>Guard.*</c> rules (AL1208, AL1212–AL1218, AL1220).
/// </summary>
internal static partial class GuardPatterns {
    private const string GuardMetadataName = "ANcpLua.Roslyn.Utilities.Guard";

    /// <summary>
    ///     Returns <see langword="true" /> when <c>ANcpLua.Roslyn.Utilities.Guard</c> exists and is
    ///     accessible from <paramref name="compilation" />. The Guard rules only fire then; otherwise the
    ///     code fix would rewrite to a symbol the consumer cannot resolve. Projects that do not reference
    ///     ANcpLua.Roslyn.Utilities are unaffected.
    /// </summary>
    internal static bool IsGuardAccessible(Compilation compilation) =>
        compilation.GetTypeByMetadataName(GuardMetadataName) is { } guardType &&
        compilation.IsSymbolAccessibleWithin(guardType, compilation.Assembly);

    /// <summary>
    ///     Returns the throw statement when <paramref name="statement" /> is a <c>throw</c> or a block
    ///     whose only statement is a <c>throw</c>; otherwise <see langword="null" />.
    /// </summary>
    internal static ThrowStatementSyntax? TryGetSingleThrowStatement(StatementSyntax statement) =>
        statement switch {
            ThrowStatementSyntax t => t,
            BlockSyntax { Statements: [ThrowStatementSyntax t] } => t,
            _ => null
        };

    /// <summary>
    ///     Returns the type created by <c>throw new X(...)</c> when <paramref name="operation" /> is that
    ///     throw or a block whose only operation is that throw; otherwise <see langword="null" />.
    /// </summary>
    internal static ITypeSymbol? GetSingleThrownCreationType(IOperation? operation) {
        if (operation is IBlockOperation block) {
            if (block.Operations.Length != 1) {
                return null;
            }

            operation = block.Operations[0];
        }

        if (operation is IExpressionStatementOperation exprStmt) {
            operation = exprStmt.Operation;
        }

        return operation is IThrowOperation { Exception: { } exception } &&
               exception.UnwrapAllConversions() is IObjectCreationOperation { Type: { } exceptionType }
            ? exceptionType
            : null;
    }

    /// <summary>
    ///     Returns the identifier text of a simple name or the member name of a member access;
    ///     <c>"value"</c> for any other expression.
    /// </summary>
    internal static string GetExpressionName(ExpressionSyntax expression) =>
        expression switch {
            IdentifierNameSyntax identifier => identifier.Identifier.Text,
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.Text,
            _ => "value"
        };
}
