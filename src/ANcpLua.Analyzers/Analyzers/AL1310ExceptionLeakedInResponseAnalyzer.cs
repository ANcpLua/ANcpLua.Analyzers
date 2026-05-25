
namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL1310: Detects exception internals leaked in HTTP responses.
/// </summary>
/// <remarks>
///     <para>
///         Returning <c>ex.Message</c>, <c>ex.ToString()</c>, or <c>ex.StackTrace</c> in
///         ASP.NET Core result factory methods exposes sensitive implementation details
///         (file paths, internal types, connection strings) to HTTP clients.
///     </para>
///     <para>
///         This analyzer flags these usages when they appear as arguments to:
///         <list type="bullet">
///             <item><c>Results.BadRequest(...)</c></item>
///             <item><c>Results.Problem(...)</c></item>
///             <item><c>Results.StatusCode(...)</c></item>
///             <item><c>Results.Json(...)</c></item>
///             <item><c>TypedResults.BadRequest(...)</c></item>
///             <item><c>TypedResults.Problem(...)</c></item>
///             <item><c>TypedResults.StatusCode(...)</c></item>
///             <item><c>TypedResults.Json(...)</c></item>
///         </list>
///     </para>
///     <para>
///         The fix is to return a generic error message and log the exception server-side.
///     </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al1310ExceptionLeakedInResponseAnalyzer : AlAnalyzer {
    /// <summary>The diagnostic identifier for AL1310.</summary>
    private const string DiagnosticId = "AL1310";

    private static readonly DiagnosticDescriptor s_rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.Reliability,
        DiagnosticSeverity.Warning);

    private static readonly string[] s_leakedMembers = ["Message", "StackTrace"];

    private static readonly string[] s_resultFactoryMethods = [
        "BadRequest",
        "Problem",
        "StatusCode",
        "Json"
    ];

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_rule];

    /// <summary>Registers syntax node action for member access expressions.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.SimpleMemberAccessExpression);

    private static void Analyze(SyntaxNodeAnalysisContext context) {
        var memberAccess = (MemberAccessExpressionSyntax)context.Node;
        var memberName = memberAccess.Name.Identifier.Text;

        // Check for ex.Message, ex.StackTrace (property access)
        // or ex.ToString() (invocation on member access -- handled below via parent check)
        var isLeakedProperty = s_leakedMembers.Contains(memberName);
        var isToString = memberName == "ToString";

        if (!isLeakedProperty && !isToString) {
            return;
        }

        // For ToString(), verify the parent is an invocation (ex.ToString() not ex.ToString)
        if (isToString && memberAccess.Parent is not InvocationExpressionSyntax) {
            return;
        }

        // The expression part (before the dot) must be a catch variable
        if (!IsCatchVariable(memberAccess.Expression, context.SemanticModel, context.CancellationToken)) {
            return;
        }

        // Walk up to see if this is an argument to a Results.*/TypedResults.* method
        if (!IsInsideResultsFactoryCall(memberAccess, context.SemanticModel, context.CancellationToken)) {
            return;
        }

        var displayText = isToString ? $"{memberAccess.Expression}.ToString()" : memberAccess.ToString();
        context.ReportDiagnostic(s_rule, memberAccess.GetLocation(), displayText);
    }

    /// <summary>Determines if the expression references a variable declared in a catch clause.</summary>
    private static bool IsCatchVariable(
        ExpressionSyntax expression,
        SemanticModel semanticModel,
        CancellationToken cancellationToken) {
        if (expression is not IdentifierNameSyntax) {
            return false;
        }

        var symbol = semanticModel.GetSymbolInfo(expression, cancellationToken).Symbol;
        if (symbol is not ILocalSymbol local) {
            return false;
        }

        // GetSyntax() returns the CatchDeclarationSyntax itself for catch variables
        foreach (var declaringRef in local.DeclaringSyntaxReferences) {
            if (declaringRef.GetSyntax(cancellationToken) is CatchDeclarationSyntax) {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    ///     Determines if the member access is ultimately used as an argument to a
    ///     Results.* or TypedResults.* factory method.
    /// </summary>
    private static bool IsInsideResultsFactoryCall(
        MemberAccessExpressionSyntax memberAccess,
        SemanticModel semanticModel,
        CancellationToken cancellationToken) {
        // Walk up to find the containing ArgumentSyntax, then the InvocationExpression
        for (var current = memberAccess.Parent; current is not null; current = current.Parent) {
            switch (current) {
                // Stop at statement boundaries
                case StatementSyntax:
                case MemberDeclarationSyntax:
                    return false;

                case ArgumentSyntax { Parent: ArgumentListSyntax { Parent: InvocationExpressionSyntax invocation } }:
                    return IsResultsFactoryInvocation(invocation, semanticModel, cancellationToken);
            }
        }

        return false;
    }

    /// <summary>Checks if the invocation is Results.X or TypedResults.X where X is a known factory method.</summary>
    private static bool IsResultsFactoryInvocation(
        InvocationExpressionSyntax invocation,
        SemanticModel semanticModel,
        CancellationToken cancellationToken) {
        if (invocation.Expression is not MemberAccessExpressionSyntax { Name: IdentifierNameSyntax methodName } access) {
            return false;
        }

        if (!s_resultFactoryMethods.Contains(methodName.Identifier.Text)) {
            return false;
        }

        // Verify the receiver is Microsoft.AspNetCore.Http.Results or TypedResults
        var symbolInfo = semanticModel.GetSymbolInfo(access.Expression, cancellationToken);

        return symbolInfo.Symbol switch {
            INamedTypeSymbol namedType => IsResultsType(namedType),
            _ => false
        };
    }

    private static bool IsResultsType(INamedTypeSymbol type) =>
        type is { ContainingNamespace:
            {
                Name: "Http",
                ContainingNamespace: {
                    Name: "AspNetCore",
                    ContainingNamespace: {
                        Name: "Microsoft",
                        ContainingNamespace.IsGlobalNamespace: true
                    }
                }
            },
            Name: "Results" or "TypedResults"
        };
}
