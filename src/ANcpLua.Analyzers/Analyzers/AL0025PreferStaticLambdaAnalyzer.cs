using ANcpLua.Analyzers.Core;

namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0025: Anonymous function can be made static.
///     Reports when a lambda or anonymous method doesn't capture any variables
///     and can therefore be marked with the 'static' modifier for better performance.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0025PreferStaticLambdaAnalyzer : AlAnalyzer {
    /// <summary>The diagnostic identifier for AL0025.</summary>
    public const string DiagnosticId = "AL0025";

    private static readonly DiagnosticDescriptor Rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.Usage,
        DiagnosticSeverity.Warning);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    /// <summary>Registers syntax node actions to analyze lambdas and anonymous methods.</summary>
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

        // Use data flow analysis to check for captured variables
        var dataFlow = ModelExtensions.AnalyzeDataFlow(semanticModel, lambda);
        if (dataFlow is null || !dataFlow.Succeeded) {
            return false;
        }

        // If the lambda captures any variables, it cannot be static
        if (dataFlow.CapturedInside.Length > 0 || dataFlow.Captured.Length > 0) {
            return false;
        }

        // Check if the lambda references 'this' implicitly
        return !ReferencesThis(lambda, semanticModel);
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
        if (semanticModel.GetEnclosingSymbol(lambda.SpanStart)?.ContainingType is not { } lambdaContainingType) {
            return false;
        }

        // Check for implicit 'this' through instance member access
        foreach (var identifier in lambda.DescendantNodes().OfType<IdentifierNameSyntax>()) {
            if (ModelExtensions.GetSymbolInfo(semanticModel, identifier).Symbol is not { } symbol) {
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
        if (!symbol.ContainingType.IsEqualTo(containingType)) {
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
