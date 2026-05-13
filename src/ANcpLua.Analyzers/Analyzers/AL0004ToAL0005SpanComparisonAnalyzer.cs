using Microsoft.CodeAnalysis.Text;

namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0004: Use pattern matching when comparing Span and a constant.
///     AL0005: Use SequenceEqual when comparing Span and a non-constant.
/// </summary>
/// <remarks>
///     <para>
///         <see cref="Span{T}" /> and <see cref="ReadOnlySpan{T}" /> do not
///         override the equality operators, so <c>span == "value"</c> compiles but performs
///         reference equality rather than content comparison, which is almost never the
///         intended behavior.
///     </para>
///     <para>
///         AL0004 triggers when comparing against compile-time constants (string literals,
///         constant collection expressions, constant array initializers). The fix is to use
///         pattern matching: <c>span is "value"</c>.
///     </para>
///     <para>
///         AL0005 triggers when comparing against runtime values (variables, method results).
///         The fix is to use <c>SequenceEqual</c>: <c>span.SequenceEqual(other)</c>.
///     </para>
///     <para>
///         The analyzer applies to both <c>==</c> and <c>!=</c> comparisons where a
///         <see cref="Span{T}" /> or <see cref="ReadOnlySpan{T}" /> is on
///         the left side.
///     </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0004ToAl0005SpanComparisonAnalyzer : AlAnalyzer {
    /// <summary>AL0004: Use pattern matching when comparing Span and a constant.</summary>
    public const string DiagnosticIdAl0004 = "AL0004";
    /// <summary>AL0005: Use SequenceEqual when comparing Span and a non-constant.</summary>
    public const string DiagnosticIdAl0005 = "AL0005";

    private static readonly LocalizableResourceString s_titleAl0004 = new(
        nameof(Resources.AL0004AnalyzerTitle), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableResourceString s_messageFormatAl0004 = new(
        nameof(Resources.AL0004AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableResourceString s_descriptionAl0004 = new(
        nameof(Resources.AL0004AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableResourceString s_titleAl0005 = new(
        nameof(Resources.AL0005AnalyzerTitle), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableResourceString s_messageFormatAl0005 = new(
        nameof(Resources.AL0005AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableResourceString s_descriptionAl0005 = new(
        nameof(Resources.AL0005AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

    private static readonly DiagnosticDescriptor s_ruleAl0004 = new(
        DiagnosticIdAl0004, s_titleAl0004, s_messageFormatAl0004, DiagnosticCategories.Usage,
        DiagnosticSeverity.Warning, true, s_descriptionAl0004,
        HelpLink(DiagnosticIdAl0004));

    private static readonly DiagnosticDescriptor s_ruleAl0005 = new(
        DiagnosticIdAl0005, s_titleAl0005, s_messageFormatAl0005, DiagnosticCategories.Usage,
        DiagnosticSeverity.Warning, true, s_descriptionAl0005,
        HelpLink(DiagnosticIdAl0005));

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics (AL0004 and AL0005).</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_ruleAl0004, s_ruleAl0005];

    /// <summary>Registers compilation start action to analyze Span comparison operations.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterCompilationStartAction(CompilationStartAction);

    private static void CompilationStartAction(CompilationStartAnalysisContext context) {
        var spanType = context.Compilation.GetTypeByMetadataName("System.Span`1");
        var readOnlySpanType = context.Compilation.GetTypeByMetadataName("System.ReadOnlySpan`1");

        if (spanType is null || readOnlySpanType is null) {
            return;
        }

        context.RegisterSyntaxNodeAction(
            snac => SyntaxNodeAction(snac, spanType, readOnlySpanType),
            SyntaxKind.EqualsExpression, SyntaxKind.NotEqualsExpression);
    }

    private static void SyntaxNodeAction(
        SyntaxNodeAnalysisContext context,
        INamedTypeSymbol spanType,
        INamedTypeSymbol readOnlySpanType) {
        var model = context.SemanticModel;
        var token = context.CancellationToken;
        var node = (BinaryExpressionSyntax)context.Node;
        var operation = model.GetOperation(node, token) as IBinaryOperation;

        if (operation?.LeftOperand.Type is not INamedTypeSymbol leftType) {
            return;
        }

        var leftDef = leftType.OriginalDefinition;
        if (!leftDef.IsEqualTo(spanType) &&
            !leftDef.IsEqualTo(readOnlySpanType)) {
            return;
        }

        var rightSyntax = operation.RightOperand.Syntax;
        var hasNonConstant = !IsConstantCollection(rightSyntax, model, token);

        var start = node.OperatorToken.Span.Start;
        var end = node.Right.Span.End;

        context.ReportDiagnostic(hasNonConstant ? s_ruleAl0005 : s_ruleAl0004,
            Location.Create(node.SyntaxTree, TextSpan.FromBounds(start, end)));
    }

    private static bool IsConstantCollection(SyntaxNode syntax, SemanticModel model, CancellationToken token) =>
        syntax.Kind() switch {
            SyntaxKind.StringLiteralExpression => true,
            SyntaxKind.CollectionExpression => IsConstantCollectionExpression(
                (CollectionExpressionSyntax)syntax, model, token),
            SyntaxKind.ArrayCreationExpression => IsConstantArrayCreation(
                (ArrayCreationExpressionSyntax)syntax, model, token),
            SyntaxKind.ImplicitArrayCreationExpression => ((ImplicitArrayCreationExpressionSyntax)syntax).Initializer
                .Expressions
                .All(e => model.GetConstantValue(e, token).HasValue),
            _ => false
        };

    private static bool IsConstantCollectionExpression(
        CollectionExpressionSyntax collection,
        SemanticModel model,
        CancellationToken token) {
        foreach (var element in collection.Elements) {
            if (element is not ExpressionElementSyntax expr) {
                return false;
            }

            if (!model.GetConstantValue(expr.Expression, token).HasValue) {
                return false;
            }
        }

        return true;
    }

    private static bool IsConstantArrayCreation(
        ArrayCreationExpressionSyntax arrayCreation,
        SemanticModel model,
        CancellationToken token) {
        if (arrayCreation.Initializer is not { } initializer) {
            return false;
        }

        return initializer.Expressions.All(e => model.GetConstantValue(e, token).HasValue);
    }
}
