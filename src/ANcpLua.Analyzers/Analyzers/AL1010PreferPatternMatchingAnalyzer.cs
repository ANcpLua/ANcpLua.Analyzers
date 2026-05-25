
namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL1010: Prefer pattern matching over equality operators for null and zero comparisons.
/// </summary>
/// <remarks>
///     <list type="bullet">
///         <item><c>x == null</c> → <c>x is null</c></item>
///         <item><c>x != null</c> → <c>x is not null</c></item>
///         <item><c>x == 0</c> → <c>x is 0</c> (includes 0L, 0u, 0f, 0d, 0m)</item>
///         <item><c>x == 0.0</c> → <c>x is 0.0</c> (includes 0.0f, 0.0d, 0.0m)</item>
///         <item><c>x != 0</c> → <c>x is not 0</c></item>
///     </list>
///     Pattern matching syntax is more expressive and, for null checks,
///     bypasses any overloaded equality operators ensuring true reference comparison.
///     <para>
///         This analyzer skips code inside expression trees (<c>Expression&lt;T&gt;</c>)
///         because pattern matching is not supported in expression trees.
///     </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al1010PreferPatternMatchingAnalyzer : AlAnalyzer {
    /// <summary>AL1010: Prefer pattern matching for null and zero comparisons.</summary>
    public const string DiagnosticId = "AL1010";

    /// <summary>Property key indicating whether this is a null check.</summary>
    private const string PropertyIsNullCheck = "IsNullCheck";
    /// <summary>Property key indicating whether the pattern is negated.</summary>
    private const string PropertyIsNegated = "IsNegated";
    /// <summary>Property key indicating whether the expression is on the left side.</summary>
    private const string PropertyExpressionIsLeft = "ExpressionIsLeft";

    private static readonly DiagnosticDescriptor s_rule = new(
        DiagnosticId,
        "Prefer pattern matching for null and zero comparisons",
        "Use '{0}' instead of '{1}'",
        DiagnosticCategories.Style,
        DiagnosticSeverity.Warning,
        true,
        "Pattern matching syntax (is/is not) is more expressive and idiomatic. " +
        "For null checks, it also bypasses overloaded equality operators.",
        HelpLink(DiagnosticId));

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_rule];

    /// <summary>Registers compilation start action to analyze binary operations for null/zero comparisons.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterCompilationStartAction(compilationContext => {
            var expressionType = compilationContext.Compilation
                .GetTypeByMetadataName("System.Linq.Expressions.Expression`1");

            compilationContext.RegisterOperationAction(
                ctx => AnalyzeBinaryOperation(ctx, expressionType),
                OperationKind.Binary);
        });

    private static void AnalyzeBinaryOperation(OperationAnalysisContext context, INamedTypeSymbol? expressionType) {
        var operation = (IBinaryOperation)context.Operation;

        if (operation.OperatorKind is not (BinaryOperatorKind.Equals or BinaryOperatorKind.NotEquals)) {
            return;
        }

        if (IsInsidePatternContext(operation.Syntax)) {
            return;
        }

        // Skip operations inside expression trees (pattern matching not supported)
        if (operation.IsInExpressionTree(expressionType)) {
            return;
        }

        // Skip when the user has overloaded operator == / != — swapping to 'is' silently changes
        // semantics (operator bypass → reference/pattern compare). Typed-quantity libraries like
        // UnitsNet rely on this operator for value semantics; the "fix" would be a real regression.
        if (operation.OperatorMethod is not null) {
            return;
        }

        if (!TryGetComparisonInfo(operation, out var isNullCheck, out var expressionIsLeft)) {
            return;
        }

        var isNegated = operation.OperatorKind == BinaryOperatorKind.NotEquals;
        var expressionOperand = expressionIsLeft ? operation.LeftOperand : operation.RightOperand;
        var literalOperand = expressionIsLeft ? operation.RightOperand : operation.LeftOperand;

        var originalText = $"{expressionOperand.Syntax} {GetOperatorText(operation)} {literalOperand.Syntax}";
        var patternKeyword = isNegated ? "is not" : "is";
        var suggestedText = $"{expressionOperand.Syntax} {patternKeyword} {literalOperand.Syntax}";

        var properties = ImmutableDictionary.CreateBuilder<string, string?>();
        properties.Add(PropertyIsNullCheck, isNullCheck.ToString());
        properties.Add(PropertyIsNegated, isNegated.ToString());
        properties.Add(PropertyExpressionIsLeft, expressionIsLeft.ToString());

        context.ReportDiagnostic(Diagnostic.Create(
            s_rule,
            operation.Syntax.GetLocation(),
            properties.ToImmutable(),
            suggestedText,
            originalText));
    }

    private static bool TryGetComparisonInfo(
        IBinaryOperation operation,
        out bool isNullCheck,
        out bool expressionIsLeft) {
        isNullCheck = false;
        expressionIsLeft = false;

        // Check right operand for null/zero
        if (operation.RightOperand.IsConstantNull()) {
            isNullCheck = true;
            expressionIsLeft = true;
            return true;
        }

        if (operation.LeftOperand.IsConstantNull()) {
            isNullCheck = true;
            expressionIsLeft = false;
            return true;
        }

        // Check for zero literals
        if (IsZeroLiteral(operation.RightOperand)) {
            isNullCheck = false;
            expressionIsLeft = true;
            return true;
        }

        if (IsZeroLiteral(operation.LeftOperand)) {
            isNullCheck = false;
            expressionIsLeft = false;
            return true;
        }

        return false;
    }

    private static bool IsZeroLiteral(IOperation operation) {
        // Use IsConstantZero for numeric zero detection
        if (!operation.IsConstantZero()) {
            return false;
        }

        // Ensure it's a literal expression (not just any constant zero expression)
        return operation.Syntax is LiteralExpressionSyntax { Token.ValueText: "0" or "0.0" };
    }

    private static bool IsInsidePatternContext(SyntaxNode node) {
        for (var current = node.Parent; current is not null; current = current.Parent) {
            switch (current) {
                case IsPatternExpressionSyntax or CasePatternSwitchLabelSyntax:
                    return true;
                // For switch expressions, only skip if we're in the pattern part, not the expression part
                case SwitchExpressionArmSyntax arm when node.SpanStart >= arm.Expression.SpanStart:
                    return false;
            }
        }

        return false;
    }

    private static string GetOperatorText(IBinaryOperation operation) =>
        operation.OperatorKind == BinaryOperatorKind.Equals ? "==" : "!=";
}
