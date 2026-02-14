using ANcpLua.Analyzers.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using System.Collections.Immutable;

namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0102: Avoid Type.GetType with dynamic name in AOT context.
/// </summary>
/// <remarks>
///     <para>
///         <c>Type.GetType(string)</c> with a dynamic type name or case-insensitive search
///         prevents the trimmer from statically analyzing which types to preserve.
///     </para>
///     <para>
///         Use typeof() for compile-time type references. For string literals without
///         case-insensitive search, the trimmer can analyze the call statically.
///     </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0102AvoidTypeGetTypeAnalyzer : AlAnalyzer {
    private const string TypeTypeName = "System.Type";

    private static readonly DiagnosticDescriptor Rule = CreateRule(
        DiagnosticIds.AvoidTypeGetTypeWithDynamicName,
        DiagnosticCategories.AotTesting,
        DiagnosticSeverities.Suggestion);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    /// <summary>Registers a compilation start action to resolve the Type type once.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterCompilationStartAction(OnCompilationStart);

    private static void OnCompilationStart(CompilationStartAnalysisContext context) {
        if (context.Compilation.GetTypeByMetadataName(TypeTypeName) is not { } typeType) {
            // System.Type is not referenced; nothing to analyze
            return;
        }

        context.RegisterOperationAction(
            ctx => AnalyzeInvocation(ctx, typeType),
            OperationKind.Invocation);
    }

    private static void AnalyzeInvocation(OperationAnalysisContext context, INamedTypeSymbol typeType) {
        var invocation = (IInvocationOperation)context.Operation;
        var targetMethod = invocation.TargetMethod;

        // Check if the method is GetType on System.Type
        if (targetMethod.Name is not "GetType") {
            return;
        }

        if (!SymbolEqualityComparer.Default.Equals(targetMethod.ContainingType, typeType)) {
            return;
        }

        // Must have at least one parameter (the type name string)
        if (targetMethod.Parameters.Length is 0) {
            return;
        }

        // First parameter must be string
        if (targetMethod.Parameters[0].Type.SpecialType != SpecialType.System_String) {
            return;
        }

        // Check if first argument is a literal string
        var firstArgument = invocation.Arguments[0].Value;
        var isLiteral = firstArgument.ConstantValue.HasValue && firstArgument.ConstantValue.Value is string;

        // Check the ignoreCase parameter (3rd parameter if present)
        var hasIgnoreCase = false;
        var ignoreCaseIsTrue = false;

        if (targetMethod.Parameters.Length >= 3 && invocation.Arguments.Length >= 3) {
            // Third parameter is ignoreCase (bool)
            var ignoreCaseArg = invocation.Arguments[2].Value;
            hasIgnoreCase = true;

            // Check if it's a literal true or any non-false-literal
            if (ignoreCaseArg.ConstantValue.HasValue) {
                if (ignoreCaseArg.ConstantValue.Value is true) {
                    ignoreCaseIsTrue = true;
                }
            }
            else {
                // Non-literal ignoreCase - could be true at runtime
                ignoreCaseIsTrue = true;
            }
        }

        // Report if:
        // 1. Type name is not a literal, OR
        // 2. ignoreCase is true or non-literal
        if (!isLiteral) {
            context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.Syntax.GetLocation(), "a dynamic type name"));
        }
        else if (hasIgnoreCase && ignoreCaseIsTrue) {
            context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.Syntax.GetLocation(), "case-insensitive search"));
        }
    }
}
