using ANcpLua.Analyzers.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using System.Collections.Immutable;

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
    private const string LambdaExpressionTypeName = "System.Linq.Expressions.LambdaExpression";

    private static readonly DiagnosticDescriptor Rule = CreateRule(
        DiagnosticIds.AvoidExpressionCompile,
        DiagnosticCategories.AotTesting,
        DiagnosticSeverities.Suggestion);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    /// <summary>Registers a compilation start action to resolve the LambdaExpression type once.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterCompilationStartAction(OnCompilationStart);

    private static void OnCompilationStart(CompilationStartAnalysisContext context) {
        if (context.Compilation.GetTypeByMetadataName(LambdaExpressionTypeName) is not { } lambdaExpressionType) {
            // System.Linq.Expressions is not referenced; nothing to analyze
            return;
        }

        context.RegisterOperationAction(
            ctx => AnalyzeInvocation(ctx, lambdaExpressionType),
            OperationKind.Invocation);
    }

    private static void AnalyzeInvocation(OperationAnalysisContext context, INamedTypeSymbol lambdaExpressionType) {
        var invocation = (IInvocationOperation)context.Operation;
        var targetMethod = invocation.TargetMethod;

        // Check if the method is Compile or CompileToMethod
        var methodName = targetMethod.Name;
        if (methodName is not ("Compile" or "CompileToMethod")) {
            return;
        }

        // Check if the containing type derives from LambdaExpression
        if (targetMethod.ContainingType is not { } containingType
            || !IsDerivedFromOrEquals(containingType, lambdaExpressionType)) {
            return;
        }

        var displayName = $"{containingType.Name}.{methodName}()";
        context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.Syntax.GetLocation(), displayName));
    }

    private static bool IsDerivedFromOrEquals(INamedTypeSymbol type, INamedTypeSymbol baseType) {
        var current = type;
        while (current is not null) {
            if (SymbolEqualityComparer.Default.Equals(current, baseType)) {
                return true;
            }
            current = current.BaseType;
        }
        return false;
    }
}
