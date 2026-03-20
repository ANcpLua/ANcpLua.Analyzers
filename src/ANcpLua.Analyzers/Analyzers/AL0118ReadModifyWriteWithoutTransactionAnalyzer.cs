
namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0118: Detects methods that perform both read and write database operations without a transaction.
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
public sealed partial class Al0118ReadModifyWriteWithoutTransactionAnalyzer : AlAnalyzer {
    /// <summary>The diagnostic identifier for AL0118.</summary>
    public const string DiagnosticId = "AL0118";

    private static readonly DiagnosticDescriptor Rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.Reliability,
        DiagnosticSeverity.Warning);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

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

        var hasRead = false;
        var hasWrite = false;
        var hasTransaction = false;

        foreach (var invocation in body.DescendantNodes().OfType<InvocationExpressionSyntax>()) {
            if (GetInvokedMethodName(invocation) is not { } methodName) {
                continue;
            }

            switch (methodName) {
                case "ExecuteReader" or "ExecuteReaderAsync" or "ExecuteScalar" or "ExecuteScalarAsync":
                    hasRead = true;
                    break;
                case "ExecuteNonQuery" or "ExecuteNonQueryAsync":
                    hasWrite = true;
                    break;
                case "BeginTransaction" or "BeginTransactionAsync":
                    hasTransaction = true;
                    break;
            }

            if (hasTransaction) {
                return;
            }
        }

        if (hasRead && hasWrite) {
            context.ReportDiagnostic(Rule, identifier.GetLocation(), identifier.Text);
        }
    }

    private static string? GetInvokedMethodName(InvocationExpressionSyntax invocation) =>
        invocation.Expression switch {
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.Text,
            IdentifierNameSyntax identifier => identifier.Identifier.Text,
            _ => null
        };
}
