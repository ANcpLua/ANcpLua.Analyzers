
namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL1401: [AotTest]/[TrimTest] methods should return 100 to indicate success.
/// </summary>
/// <remarks>
///     <para>
///         By convention, AOT and Trim test methods should return 100 to indicate success.
///         Other values are reserved for different failure conditions.
///     </para>
///     <para>
///         This analyzer reports a warning only when a method has NO return statement
///         returning 100. Methods with conditional failure returns (e.g., return 1 for errors)
///         alongside a success return (return 100) are not flagged.
///     </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al1401AotTestExitCode100Analyzer : AlAnalyzer {
    /// <summary>The diagnostic identifier for AL1401.</summary>
    private const string DiagnosticId = "AL1401";

    private const string AotTestAttributeName = "AotTest";
    private const string TrimTestAttributeName = "TrimTest";
    private const int ExpectedExitCode = 100;

    private static readonly DiagnosticDescriptor s_rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.AotTesting,
        DiagnosticSeverity.Warning);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_rule];

    /// <summary>Registers syntax or operation actions for analysis.</summary>

    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterSyntaxNodeAction(AnalyzeMethodDeclaration, SyntaxKind.MethodDeclaration);

    private static void AnalyzeMethodDeclaration(SyntaxNodeAnalysisContext context) {
        var methodDeclaration = (MethodDeclarationSyntax)context.Node;
        if (context.SemanticModel.GetDeclaredSymbol(methodDeclaration, context.CancellationToken)
            is not { } methodSymbol) {
            return;
        }

        if (!HasAotOrTrimTestAttribute(methodSymbol)) {
            return;
        }

        var intType = context.SemanticModel.Compilation.GetSpecialType(SpecialType.System_Int32);
        if (!methodSymbol.ReturnType.IsEqualTo(intType)) {
            return;
        }

        AnalyzeReturnStatements(context, methodDeclaration);
    }

    private static void AnalyzeReturnStatements(
        SyntaxNodeAnalysisContext context,
        MethodDeclarationSyntax methodDeclaration) {
        var returnStatements = methodDeclaration.DescendantNodes()
            .OfType<ReturnStatementSyntax>()
            .ToList();

        if (returnStatements.Count is 0) {
            if (methodDeclaration.ExpressionBody is { } expressionBody) {
                CheckSingleReturnValue(context, expressionBody.Expression);
            } else {
                context.ReportDiagnostic(
                    s_rule, methodDeclaration.Identifier.GetLocation(),
                    $"Method ends without explicit 'return {ExpectedExitCode};' statement");
            }

            return;
        }

        if (returnStatements.Any(r => r.Expression is not null && IsExpectedExitCode(context, r.Expression))) {
            return;
        }

        context.ReportDiagnostic(
            s_rule, methodDeclaration.Identifier.GetLocation(),
            $"Method has no 'return {ExpectedExitCode};' statement to indicate success");
    }

    private static bool IsExpectedExitCode(SyntaxNodeAnalysisContext context, ExpressionSyntax expression) =>
        context.SemanticModel.GetConstantValue(expression, context.CancellationToken)
            is { HasValue: true, Value: ExpectedExitCode };

    private static void CheckSingleReturnValue(SyntaxNodeAnalysisContext context, ExpressionSyntax expression) {
        var constantValue = context.SemanticModel.GetConstantValue(expression, context.CancellationToken);

        switch (constantValue) {
            case { HasValue: false }:
                context.ReportDiagnostic(
                    s_rule, expression.GetLocation(),
                    $"Consider returning {ExpectedExitCode} for success instead of a computed value");
                break;
            case { Value: ExpectedExitCode }:
                break;
            default:
                context.ReportDiagnostic(
                    s_rule, expression.GetLocation(),
                    $"Return value should be {ExpectedExitCode} for success, not {constantValue.Value?.ToString() ?? "null"}");
                break;
        }
    }

    private static bool HasAotOrTrimTestAttribute(IMethodSymbol method) =>
        method.HasAttributeByShortName(AotTestAttributeName) ||
        method.HasAttributeByShortName(TrimTestAttributeName);
}
