using ANcpLua.Analyzers.Core;

namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0042: [AotTest]/[TrimTest] methods should return 100 to indicate success.
/// </summary>
/// <remarks>
///     <para>
///         By convention, AOT and Trim test methods should return 100 to indicate success.
///         Other values are reserved for different failure conditions.
///     </para>
///     <para>
///         This analyzer checks return statements and reports:
///         - Warning if a literal return value is not 100
///         - Info if the method ends without an explicit return 100
///     </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0042AotTestExitCode100Analyzer : AlAnalyzer {
    private const string AotTestAttributeName = "AotTest";
    private const string TrimTestAttributeName = "TrimTest";
    private const int ExpectedExitCode = 100;

    private static readonly LocalizableResourceString Title = new(
        nameof(Resources.AL0042AnalyzerTitle), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableResourceString MessageFormat = new(
        nameof(Resources.AL0042AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableResourceString Description = new(
        nameof(Resources.AL0042AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

    private static readonly DiagnosticDescriptor WarningRule = new(
        DiagnosticIds.AotTestExitCode100,
        Title, MessageFormat, DiagnosticCategories.AotTesting,
        DiagnosticSeverity.Warning, true, Description,
        HelpLinkBase);

    private static readonly DiagnosticDescriptor InfoRule = new(
        DiagnosticIds.AotTestExitCode100,
        Title, MessageFormat, DiagnosticCategories.AotTesting,
        DiagnosticSeverity.Info, true, Description,
        HelpLinkBase);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [WarningRule, InfoRule];

    /// <summary>Registers syntax or operation actions for analysis.</summary>

    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterSyntaxNodeAction(AnalyzeMethodDeclaration, SyntaxKind.MethodDeclaration);

    private static void AnalyzeMethodDeclaration(SyntaxNodeAnalysisContext context) {
        var methodDeclaration = (MethodDeclarationSyntax)context.Node;
        if (context.SemanticModel.GetDeclaredSymbol(methodDeclaration, context.CancellationToken)
            is not { } methodSymbol) {
            return;
        }

        // Check if method has [AotTest] or [TrimTest] attribute
        if (!HasAotOrTrimTestAttribute(methodSymbol)) {
            return;
        }

        // Check if method returns int
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
            // Method has no explicit return statements
            // For expression-bodied methods, check the expression
            if (methodDeclaration.ExpressionBody is { } expressionBody) {
                CheckReturnValue(context, expressionBody.Expression, InfoRule);
            } else {
                // Block-bodied method with no return statements - implicit return
                var location = methodDeclaration.Identifier.GetLocation();
                context.ReportDiagnostic(
                    InfoRule, location,
                    $"Method ends without explicit 'return {ExpectedExitCode};' statement");
            }

            return;
        }

        foreach (var returnStatement in returnStatements) {
            if (returnStatement.Expression is null) {
                continue;
            }

            CheckReturnValue(context, returnStatement.Expression, WarningRule);
        }
    }

    private static void CheckReturnValue(
        SyntaxNodeAnalysisContext context,
        CSharpSyntaxNode expression,
        DiagnosticDescriptor rule) {
        var constantValue = context.SemanticModel.GetConstantValue(expression, context.CancellationToken);

        if (!constantValue.HasValue) {
            // Not a constant - could be a variable or method call
            // Report info suggesting they should return 100
            context.ReportDiagnostic(
                InfoRule, expression.GetLocation(),
                $"Consider returning {ExpectedExitCode} for success instead of a computed value");
            return;
        }

        if (constantValue.Value is int intValue && intValue == ExpectedExitCode) {
            // Correct exit code - no diagnostic
            return;
        }

        // Return value is a literal but not 100
        var actualValue = constantValue.Value?.ToString() ?? "null";
        context.ReportDiagnostic(
            rule, expression.GetLocation(),
            $"Return value should be {ExpectedExitCode} for success, not {actualValue}");
    }

    private static bool HasAotOrTrimTestAttribute(IMethodSymbol method) =>
        method.HasAttributeByShortName(AotTestAttributeName) ||
        method.HasAttributeByShortName(TrimTestAttributeName);
}
