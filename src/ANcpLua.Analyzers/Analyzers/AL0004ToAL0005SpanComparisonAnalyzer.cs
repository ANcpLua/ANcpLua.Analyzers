using ANcpLua.Analyzers.Core;
using Microsoft.CodeAnalysis.Text;

namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
/// AL0004: Use pattern matching when comparing Span and a constant.
/// AL0005: Use SequenceEqual when comparing Span and a non-constant.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AL0004ToAL0005SpanComparisonAnalyzer : ALAnalyzer
{
    public const string DiagnosticIdAL0004 = "AL0004";
    public const string DiagnosticIdAL0005 = "AL0005";
    private const string Category = "Usage";

    private static readonly LocalizableResourceString TitleAL0004 = new(
        nameof(Resources.AL0004AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
    private static readonly LocalizableResourceString MessageFormatAL0004 = new(
        nameof(Resources.AL0004AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
    private static readonly LocalizableResourceString DescriptionAL0004 = new(
        nameof(Resources.AL0004AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableResourceString TitleAL0005 = new(
        nameof(Resources.AL0005AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
    private static readonly LocalizableResourceString MessageFormatAL0005 = new(
        nameof(Resources.AL0005AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
    private static readonly LocalizableResourceString DescriptionAL0005 = new(
        nameof(Resources.AL0005AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

    private static readonly DiagnosticDescriptor RuleAL0004 = new(
        DiagnosticIdAL0004, TitleAL0004, MessageFormatAL0004, Category,
        DiagnosticSeverity.Warning, isEnabledByDefault: true, DescriptionAL0004,
        HelpLinkBase + "AL0004.md");

    private static readonly DiagnosticDescriptor RuleAL0005 = new(
        DiagnosticIdAL0005, TitleAL0005, MessageFormatAL0005, Category,
        DiagnosticSeverity.Warning, isEnabledByDefault: true, DescriptionAL0005,
        HelpLinkBase + "AL0005.md");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [RuleAL0004, RuleAL0005];

    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterCompilationStartAction(CompilationStartAction);

    private static void CompilationStartAction(CompilationStartAnalysisContext context)
    {
        var spanType = context.Compilation.GetTypeByMetadataName("System.Span`1");
        var readOnlySpanType = context.Compilation.GetTypeByMetadataName("System.ReadOnlySpan`1");

        if (spanType is null || readOnlySpanType is null)
            return;

        context.RegisterSyntaxNodeAction(
            snac => SyntaxNodeAction(snac, spanType, readOnlySpanType),
            SyntaxKind.EqualsExpression, SyntaxKind.NotEqualsExpression);
    }

    private static void SyntaxNodeAction(
        SyntaxNodeAnalysisContext context,
        INamedTypeSymbol spanType,
        INamedTypeSymbol readOnlySpanType)
    {
        var model = context.SemanticModel;
        var token = context.CancellationToken;
        var node = (BinaryExpressionSyntax)context.Node;
        var operation = model.GetOperation(node, token) as IBinaryOperation;

        if (operation?.LeftOperand.Type is not INamedTypeSymbol leftType)
            return;

        var leftDef = leftType.OriginalDefinition ?? leftType;
        if (!SymbolEqualityComparer.Default.Equals(leftDef, spanType) &&
            !SymbolEqualityComparer.Default.Equals(leftDef, readOnlySpanType))
            return;

        var rightSyntax = operation.RightOperand.Syntax;
        var hasNonConstant = !IsConstantCollection(rightSyntax, model, token);

        var start = node.OperatorToken.Span.Start;
        var end = node.Right.Span.End;

        context.ReportDiagnostic(hasNonConstant ? RuleAL0005 : RuleAL0004,
            Location.Create(node.SyntaxTree, TextSpan.FromBounds(start, end)));
    }

    private static bool IsConstantCollection(SyntaxNode syntax, SemanticModel model, CancellationToken token) =>
        syntax.Kind() switch
        {
            SyntaxKind.StringLiteralExpression => true,
            SyntaxKind.CollectionExpression => ((CollectionExpressionSyntax)syntax).Elements
                .All(e => model.GetConstantValue(e.DescendantNodes().Single(), token).HasValue),
            SyntaxKind.ArrayCreationExpression => ((ArrayCreationExpressionSyntax)syntax).Initializer?.Expressions
                .All(e => model.GetConstantValue(e, token).HasValue) ?? true,
            SyntaxKind.ImplicitArrayCreationExpression => ((ImplicitArrayCreationExpressionSyntax)syntax).Initializer.Expressions
                .All(e => model.GetConstantValue(e, token).HasValue),
            _ => false
        };
}
