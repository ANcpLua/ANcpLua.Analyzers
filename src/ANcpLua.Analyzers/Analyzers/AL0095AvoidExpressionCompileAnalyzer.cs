
namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0095: Avoid Expression.Compile() in AOT context.
/// </summary>
/// <remarks>
///     <para>
///         <c>Expression.Compile()</c> and <c>Expression.CompileToMethod()</c> rely on
///         System.Reflection.Emit to generate IL at runtime. In Native AOT, these methods
///         fall back to an interpreted mode that is significantly slower than JIT-compiled delegates.
///     </para>
///     <para>
///         This is not caught by the built-in IL3XXX analyzers because the methods are not annotated
///         with [RequiresDynamicCode]. They technically work in AOT (via interpretation) but with
///         severe performance degradation that developers should be aware of.
///     </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0095AvoidExpressionCompileAnalyzer : AlAnalyzer {
    /// <summary>The diagnostic identifier for AL0095.</summary>
    private const string DiagnosticId = "AL0095";

    private const string LambdaExpressionTypeName = "System.Linq.Expressions.LambdaExpression";

    private static readonly DiagnosticDescriptor Rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.AotTesting,
        DiagnosticSeverities.Suggestion);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    /// <summary>Registers a compilation start action to resolve the LambdaExpression type once.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterCompilationStartAction(OnCompilationStart);

    private static void OnCompilationStart(CompilationStartAnalysisContext context) {
        if (!AotContext.IsAotTargeting(context.Options.AnalyzerConfigOptionsProvider.GlobalOptions)) {
            return;
        }

        if (context.Compilation.GetTypeByMetadataName(LambdaExpressionTypeName) is not { } lambdaExpressionType) {
            return;
        }

        context.RegisterOperationAction(
            ctx => AnalyzeInvocation(ctx, lambdaExpressionType),
            OperationKind.Invocation);
    }

    private static void AnalyzeInvocation(OperationAnalysisContext context, INamedTypeSymbol lambdaExpressionType) {
        var invocation = (IInvocationOperation)context.Operation;
        var targetMethod = invocation.TargetMethod;

        if (targetMethod.Name is not ("Compile" or "CompileToMethod")
            || targetMethod.ContainingType is not { } containingType
            || (!containingType.IsEqualTo(lambdaExpressionType) && !containingType.InheritsFrom(lambdaExpressionType))) {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            Rule, invocation.Syntax.GetLocation(), $"{containingType.Name}.{targetMethod.Name}()"));
    }
}
