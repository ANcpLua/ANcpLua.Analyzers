using ANcpLua.Analyzers.Core;

namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0025: Anonymous function can be made static.
///     Reports when a lambda or anonymous method doesn't capture any variables
///     and can therefore be marked with the 'static' modifier for better performance.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AL0025PreferStaticLambdaAnalyzer : ALAnalyzer {
    private static readonly LocalizableResourceString Title = new(
        nameof(Resources.AL0025AnalyzerTitle), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableResourceString MessageFormat = new(
        nameof(Resources.AL0025AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableResourceString Description = new(
        nameof(Resources.AL0025AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.PreferStaticLambda,
        Title, MessageFormat, DiagnosticCategories.Usage,
        DiagnosticSeverity.Info, true, Description,
        HelpLinkBase);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterSyntaxNodeAction(
            AnalyzeLambda,
            SyntaxKind.SimpleLambdaExpression,
            SyntaxKind.ParenthesizedLambdaExpression,
            SyntaxKind.AnonymousMethodExpression);

    private static void AnalyzeLambda(SyntaxNodeAnalysisContext context) {
        var lambda = (AnonymousFunctionExpressionSyntax)context.Node;

        if (!CanBeStatic(lambda, context.SemanticModel)) {
            return;
        }

        // Report diagnostic on the entire lambda for proper Fix All support
        context.ReportDiagnostic(Diagnostic.Create(Rule, lambda.GetLocation()));
    }

    /// <summary>
    ///     Determines if a lambda can be made static.
    ///     Exposed for use by the refactoring provider.
    /// </summary>
    public static bool CanBeStatic(AnonymousFunctionExpressionSyntax lambda, SemanticModel semanticModel) {
        // Skip if already static
        if (HasStaticModifier(lambda)) {
            return false;
        }

        // Skip if inside another lambda (nested lambdas are complex)
        if (lambda.Parent?.FirstAncestorOrSelf<AnonymousFunctionExpressionSyntax>() is not null) {
            return false;
        }

        // Use data flow analysis to check for captured variables
        var dataFlow = semanticModel.AnalyzeDataFlow(lambda);
        if (dataFlow is null || !dataFlow.Succeeded) {
            return false;
        }

        // If the lambda captures any variables, it cannot be static
        if (dataFlow.CapturedInside.Length > 0 || dataFlow.Captured.Length > 0) {
            return false;
        }

        // Check if the lambda references 'this' implicitly
        if (ReferencesThis(lambda, semanticModel)) {
            return false;
        }

        return true;
    }

    private static bool HasStaticModifier(AnonymousFunctionExpressionSyntax lambda) =>
        lambda switch {
            SimpleLambdaExpressionSyntax simple => simple.Modifiers.Any(SyntaxKind.StaticKeyword),
            ParenthesizedLambdaExpressionSyntax paren => paren.Modifiers.Any(SyntaxKind.StaticKeyword),
            AnonymousMethodExpressionSyntax anon => anon.Modifiers.Any(SyntaxKind.StaticKeyword),
            _ => false
        };

    private static bool ReferencesThis(SyntaxNode lambda, SemanticModel semanticModel) {
        // Check for explicit 'this' usage
        if (lambda.DescendantNodes().OfType<ThisExpressionSyntax>().Any()) {
            return true;
        }

        // Get the containing type of the lambda
        var lambdaContainingType = semanticModel.GetEnclosingSymbol(lambda.SpanStart)?.ContainingType;
        if (lambdaContainingType is null) {
            return false;
        }

        // Check for implicit 'this' through instance member access
        foreach (var identifier in lambda.DescendantNodes().OfType<IdentifierNameSyntax>()) {
            var symbol = semanticModel.GetSymbolInfo(identifier).Symbol;
            if (symbol is null) {
                continue;
            }

            // Check if it's an instance member from the containing type
            if (IsInstanceMemberOfType(symbol, lambdaContainingType)) {
                return true;
            }
        }

        return false;
    }

    private static bool IsInstanceMemberOfType(ISymbol symbol, INamedTypeSymbol containingType) {
        // Must be from the same type
        if (!SymbolEqualityComparer.Default.Equals(symbol.ContainingType, containingType)) {
            return false;
        }

        return symbol switch {
            IFieldSymbol field => !field.IsStatic,
            IPropertySymbol property => !property.IsStatic,
            IMethodSymbol method => !method.IsStatic && method.MethodKind != MethodKind.Constructor,
            IEventSymbol @event => !@event.IsStatic,
            _ => false
        };
    }

}
