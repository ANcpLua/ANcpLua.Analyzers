
namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0079: Detects complex async patterns in [Traced] methods where manual instrumentation
///     may provide better observability than auto-instrumentation.
/// </summary>
/// <remarks>
///     <para>
///         Auto-instrumentation from [Traced] creates a single span for the method execution.
///         However, complex async patterns may not be fully captured:
///         <list type="bullet">
///             <item><b>Task.WhenAll</b>: Parallel tasks run concurrently but appear as sequential in traces</item>
///             <item><b>Parallel.ForEach</b>: Parallel iterations are not individually traced</item>
///             <item><b>Multiple awaits</b>: Gaps between awaits may obscure actual execution flow</item>
///             <item><b>ConfigureAwait(false)</b>: Context loss may affect trace propagation</item>
///         </list>
///     </para>
///     <para>
///         For these patterns, consider using manual <c>Activity.StartActivity()</c> to create
///         explicit child spans that capture the parallel execution branches.
///     </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0079ManualSpanRecommendedAnalyzer : AlAnalyzer {
    /// <summary>The diagnostic identifier for AL0079.</summary>
    private const string DiagnosticId = "AL0079";

    private const string TracedAttributeFullName = "Qyl.Instrumentation.Instrumentation.TracedAttribute";

    private static readonly DiagnosticDescriptor s_rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.OpenTelemetry,
        DiagnosticSeverities.HiddenByDefault);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_rule];

    /// <summary>Registers syntax node actions to analyze methods with [Traced] attribute.</summary>
    protected override void RegisterActions(AnalysisContext context) {
        context.RegisterCompilationStartAction(compilationContext => {
            if (compilationContext.Compilation.GetTypeByMetadataName(TracedAttributeFullName) is not { } tracedAttributeType) {
                return;
            }

            compilationContext.RegisterSyntaxNodeAction(
                ctx => AnalyzeMethod(ctx, tracedAttributeType),
                SyntaxKind.MethodDeclaration);
        });
    }

    private static void AnalyzeMethod(SyntaxNodeAnalysisContext context, INamedTypeSymbol tracedAttributeType) {
        var method = (MethodDeclarationSyntax)context.Node;

        if (!method.Modifiers.Any(SyntaxKind.AsyncKeyword)) {
            return;
        }

        if (ModelExtensions.GetDeclaredSymbol(context.SemanticModel, method, context.CancellationToken) is not { } methodSymbol
            || (!HasTracedAttribute(methodSymbol, tracedAttributeType)
                && !HasTracedAttribute(methodSymbol.ContainingType, tracedAttributeType))) {
            return;
        }

        var complexPatterns = DetectComplexPatterns(method, context.SemanticModel, context.CancellationToken);

        if (complexPatterns.Length > 0) {
            context.ReportDiagnostic(s_rule, method.Identifier.GetLocation(), methodSymbol.Name, string.Join(", ", complexPatterns));
        }
    }

    private static bool HasTracedAttribute(ISymbol symbol, INamedTypeSymbol tracedAttributeType) {
        foreach (var attribute in symbol.GetAttributes()) {
            if (attribute.AttributeClass.IsEqualTo(tracedAttributeType)) {
                return true;
            }
        }

        return false;
    }

    private static ImmutableArray<string> DetectComplexPatterns(
        MethodDeclarationSyntax method,
        SemanticModel semanticModel,
        CancellationToken cancellationToken) {
        if (method.Body is null && method.ExpressionBody is null) {
            return [];
        }

        var walker = new ComplexPatternWalker(semanticModel, cancellationToken);
        walker.Visit(method);

        var patterns = ImmutableArray.CreateBuilder<string>();

        if (walker.HasTaskWhenAll) {
            patterns.Add("Task.WhenAll");
        }

        if (walker.HasTaskWhenAny) {
            patterns.Add("Task.WhenAny");
        }

        if (walker.HasParallelForEach) {
            patterns.Add("Parallel.ForEach/ForEachAsync");
        }

        if (walker.HasParallelFor) {
            patterns.Add("Parallel.For/ForAsync");
        }

        if (walker.HasConfigureAwaitFalse) {
            patterns.Add("ConfigureAwait(false)");
        }

        if (walker.AwaitCount >= 3) {
            patterns.Add($"multiple awaits ({walker.AwaitCount})");
        }

        return patterns.ToImmutable();
    }

    /// <summary>
    ///     Syntax walker that detects complex async patterns in method bodies.
    /// </summary>
    private sealed partial class ComplexPatternWalker(SemanticModel semanticModel, CancellationToken cancellationToken)
        : CSharpSyntaxWalker {
        public int AwaitCount { get; private set; }
        public bool HasTaskWhenAll { get; private set; }
        public bool HasTaskWhenAny { get; private set; }
        public bool HasParallelForEach { get; private set; }
        public bool HasParallelFor { get; private set; }
        public bool HasConfigureAwaitFalse { get; private set; }

        public override void VisitAwaitExpression(AwaitExpressionSyntax node) {
            AwaitCount++;
            base.VisitAwaitExpression(node);
        }

        public override void VisitInvocationExpression(InvocationExpressionSyntax node) {
            if (ModelExtensions.GetSymbolInfo(semanticModel, node, cancellationToken).Symbol is IMethodSymbol methodSymbol) {
                switch (methodSymbol.ContainingType?.ToDisplayString(), methodSymbol.Name) {
                    case ("System.Threading.Tasks.Task", "WhenAll"):
                        HasTaskWhenAll = true;
                        break;
                    case ("System.Threading.Tasks.Task", "WhenAny"):
                        HasTaskWhenAny = true;
                        break;
                    case ("System.Threading.Tasks.Parallel", "ForEach" or "ForEachAsync"):
                        HasParallelForEach = true;
                        break;
                    case ("System.Threading.Tasks.Parallel", "For" or "ForAsync"):
                        HasParallelFor = true;
                        break;
                    case (_, "ConfigureAwait") when methodSymbol.Parameters.Length == 1
                        && node.ArgumentList.Arguments is [{ Expression: LiteralExpressionSyntax { Token.Value: false } }]:
                        HasConfigureAwaitFalse = true;
                        break;
                }
            }

            base.VisitInvocationExpression(node);
        }

        // Nested lambdas/local functions have their own scope -- don't count their awaits
        public override void VisitParenthesizedLambdaExpression(ParenthesizedLambdaExpressionSyntax node) { }

        public override void VisitSimpleLambdaExpression(SimpleLambdaExpressionSyntax node) { }

        public override void VisitLocalFunctionStatement(LocalFunctionStatementSyntax node) { }
    }
}
