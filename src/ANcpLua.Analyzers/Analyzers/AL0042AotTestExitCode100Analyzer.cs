using ANcpLua.Analyzers.Core;
using ANcpLua.Roslyn.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

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
///         This analyzer reports a warning only when a method has NO return statement
///         returning 100. Methods with conditional failure returns (e.g., return 1 for errors)
///         alongside a success return (return 100) are not flagged.
///     </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0042AotTestExitCode100Analyzer : AlAnalyzer {
    /// <summary>The diagnostic identifier for AL0042.</summary>
    public const string DiagnosticId = "AL0042";

    private const string AotTestAttributeName = "AotTest";
    private const string TrimTestAttributeName = "TrimTest";
    private const int ExpectedExitCode = 100;

    private static readonly DiagnosticDescriptor Rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.AotTesting,
        DiagnosticSeverity.Warning);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

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
                CheckSingleReturnValue(context, expressionBody.Expression);
            } else {
                // Block-bodied method with no return statements - implicit return
                var location = methodDeclaration.Identifier.GetLocation();
                context.ReportDiagnostic(
                    Rule, location,
                    $"Method ends without explicit 'return {ExpectedExitCode};' statement");
            }

            return;
        }

        // First pass: check if ANY return statement returns 100
        var hasSuccessReturn = false;
        foreach (var returnStatement in returnStatements) {
            if (returnStatement.Expression is null) {
                continue;
            }

            if (IsExpectedExitCode(context, returnStatement.Expression)) {
                hasSuccessReturn = true;
                break;
            }
        }

        // If there's at least one return 100, don't report on failure returns (they're intentional)
        if (hasSuccessReturn) {
            return;
        }

        // No return 100 found - report on the method identifier
        var methodLocation = methodDeclaration.Identifier.GetLocation();
        context.ReportDiagnostic(
            Rule, methodLocation,
            $"Method has no 'return {ExpectedExitCode};' statement to indicate success");
    }

    private static bool IsExpectedExitCode(SyntaxNodeAnalysisContext context, ExpressionSyntax expression) {
        var constantValue = context.SemanticModel.GetConstantValue(expression, context.CancellationToken);
        return constantValue is { HasValue: true, Value: int intValue } && intValue == ExpectedExitCode;
    }

    private static void CheckSingleReturnValue(SyntaxNodeAnalysisContext context, ExpressionSyntax expression) {
        var constantValue = context.SemanticModel.GetConstantValue(expression, context.CancellationToken);

        if (!constantValue.HasValue) {
            // Not a constant - could be a variable or method call
            context.ReportDiagnostic(
                Rule, expression.GetLocation(),
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
            Rule, expression.GetLocation(),
            $"Return value should be {ExpectedExitCode} for success, not {actualValue}");
    }

    private static bool HasAotOrTrimTestAttribute(IMethodSymbol method) =>
        method.HasAttributeByShortName(AotTestAttributeName) ||
        method.HasAttributeByShortName(TrimTestAttributeName);
}
