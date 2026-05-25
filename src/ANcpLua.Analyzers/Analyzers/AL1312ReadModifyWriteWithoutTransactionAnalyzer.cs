
namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL1312: Detects methods that perform both read and write database operations without a transaction.
/// </summary>
/// <remarks>
///     <para>
///         A method that calls <c>ExecuteReader</c>/<c>ExecuteScalar</c> (read) and <c>ExecuteNonQuery</c> (write)
///         on the same connection without a <c>BeginTransaction</c> call is vulnerable to race conditions.
///         Another connection could modify the data between the read and write, producing inconsistent results.
///     </para>
///     <para>
///         This analyzer flags methods containing both:
///         <list type="bullet">
///             <item>A read call: <c>ExecuteReader</c>, <c>ExecuteReaderAsync</c>, <c>ExecuteScalar</c>, or <c>ExecuteScalarAsync</c></item>
///             <item>A write call: <c>ExecuteNonQuery</c> or <c>ExecuteNonQueryAsync</c></item>
///         </list>
///         without any <c>BeginTransaction</c> or <c>BeginTransactionAsync</c> call in the same method body.
///     </para>
///     <para>
///         The fix is to wrap the read-modify-write sequence in a transaction via
///         <c>connection.BeginTransaction()</c> or <c>connection.BeginTransactionAsync()</c>.
///     </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al1312ReadModifyWriteWithoutTransactionAnalyzer : AlAnalyzer {
    /// <summary>The diagnostic identifier for AL1312.</summary>
    private const string DiagnosticId = "AL1312";

    private static readonly DiagnosticDescriptor s_rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.Reliability,
        DiagnosticSeverity.Warning);

    private static readonly ImmutableHashSet<string> s_readMethods =
        ImmutableHashSet.Create(StringComparer.Ordinal, "ExecuteReader", "ExecuteReaderAsync", "ExecuteScalar",
            "ExecuteScalarAsync");

    private static readonly ImmutableHashSet<string> s_writeMethods =
        ImmutableHashSet.Create(StringComparer.Ordinal, "ExecuteNonQuery", "ExecuteNonQueryAsync");

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_rule];

    /// <summary>Registers syntax node action on method declarations and local functions.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterSyntaxNodeAction(
            Analyze,
            SyntaxKind.MethodDeclaration,
            SyntaxKind.LocalFunctionStatement);

    private static void Analyze(SyntaxNodeAnalysisContext context) {
        var (body, identifier) = context.Node switch {
            MethodDeclarationSyntax method => ((SyntaxNode?)method.Body ?? method.ExpressionBody, method.Identifier),
            LocalFunctionStatementSyntax local => ((SyntaxNode?)local.Body ?? local.ExpressionBody, local.Identifier),
            _ => (null, default)
        };

        if (body is null) {
            return;
        }

        var hasUnknownCorrelation = false;
        var reads = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
        var writes = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
        var transactions = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
        var commandConnections = new Dictionary<ISymbol, ISymbol>(SymbolEqualityComparer.Default);

        foreach (var invocation in body.DescendantNodes().OfType<InvocationExpressionSyntax>()) {
            if (GetInvokedMethodName(invocation) is not { } methodName) {
                continue;
            }

            var receiver = TryGetReceiverSymbol(context.SemanticModel, invocation);
            switch (methodName) {
                case var name when s_readMethods.Contains(name):
                    if (receiver is null) {
                        hasUnknownCorrelation = true;
                    }
                    else {
                        reads.Add(receiver);
                    }

                    break;
                case var name when s_writeMethods.Contains(name):
                    if (receiver is null) {
                        hasUnknownCorrelation = true;
                    }
                    else {
                        writes.Add(receiver);
                    }

                    break;
                case "CreateCommand":
                    if (receiver is not null
                        && TryGetAssignedSymbol(context.SemanticModel, invocation) is { } commandSymbol) {
                        commandConnections[commandSymbol] = receiver;
                    }

                    break;
                case "BeginTransaction" or "BeginTransactionAsync":
                    if (receiver is null) {
                        hasUnknownCorrelation = true;
                    }
                    else {
                        transactions.Add(receiver);
                    }

                    break;
            }
        }

        if (hasUnknownCorrelation) {
            return;
        }

        foreach (var command in reads) {
            if (!writes.Contains(command)) {
                continue;
            }

            if (IsProtectedByTransaction(command, commandConnections, transactions)) {
                continue;
            }

            context.ReportDiagnostic(s_rule, identifier.GetLocation(), identifier.Text);
            return;
        }
    }

    private static bool IsProtectedByTransaction(
        ISymbol command,
        Dictionary<ISymbol, ISymbol> commandConnections,
        HashSet<ISymbol> transactions) {
        if (transactions.Contains(command)) {
            return true;
        }

        if (commandConnections.TryGetValue(command, out var commandConnection)
            && transactions.Contains(commandConnection)) {
            return true;
        }

        return false;
    }

    private static ISymbol? TryGetReceiverSymbol(SemanticModel semanticModel, InvocationExpressionSyntax invocation) {
        if (invocation.Expression is not MemberAccessExpressionSyntax { Expression: var expression }) {
            return null;
        }

        while (true) {
            switch (expression) {
                case CastExpressionSyntax castExpression:
                    expression = castExpression.Expression;
                    continue;
                case ParenthesizedExpressionSyntax parenthesized:
                    expression = parenthesized.Expression;
                    continue;
                default:
                    return semanticModel.GetSymbolInfo(expression).Symbol;
            }
        }
    }

    private static ISymbol? TryGetAssignedSymbol(SemanticModel semanticModel, InvocationExpressionSyntax invocation) {
        var node = (SyntaxNode?)invocation;
        while (node is not null) {
            switch (node) {
                case EqualsValueClauseSyntax { Parent: VariableDeclaratorSyntax declarator }:
                    return semanticModel.GetDeclaredSymbol(declarator);
                case AssignmentExpressionSyntax assignment when assignment.Right == node:
                    return semanticModel.GetSymbolInfo(assignment.Left).Symbol;
            }

            node = node.Parent;
        }

        return null;
    }

    private static string? GetInvokedMethodName(InvocationExpressionSyntax invocation) =>
        invocation.Expression switch {
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.Text,
            IdentifierNameSyntax identifier => identifier.Identifier.Text,
            _ => null
        };
}
