using ANcpLua.Analyzers.Core;
using ANcpLua.Roslyn.Utilities.Matching;

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
    /// <summary>The diagnostic identifier for AL0102.</summary>
    public const string DiagnosticId = "AL0102";

    private const string TypeTypeName = "System.Type";
    private static readonly InvocationMatcher GetTypeInvocation = Invoke.Method("GetType");

    private static readonly DiagnosticDescriptor Rule = CreateRule(
        DiagnosticId,
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
        if (context.Operation is not IInvocationOperation invocation ||
            !GetTypeInvocation.Matches(invocation)) {
            return;
        }

        var targetMethod = invocation.TargetMethod;
        if (!targetMethod.ContainingType.IsEqualTo(typeType)) {
            return;
        }

        // Only analyze overloads whose type-name argument is a string.
        if (GetArgumentValue(invocation, "typeName") is not { } typeNameArgument ||
            typeNameArgument.Type?.SpecialType is not SpecialType.System_String) {
            return;
        }

        var isLiteralTypeName = typeNameArgument.ConstantValue is { HasValue: true, Value: string };
        var ignoreCaseArgument = GetArgumentValue(invocation, "ignoreCase");

        // Report if:
        // 1. Type name is not a literal, OR
        // 2. ignoreCase is true or non-literal
        if (!isLiteralTypeName) {
            context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.Syntax.GetLocation(), "a dynamic type name"));
        }
        else if (ignoreCaseArgument is not null && IsPotentiallyTrue(ignoreCaseArgument)) {
            context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.Syntax.GetLocation(), "case-insensitive search"));
        }
    }

    private static IOperation? GetArgumentValue(IInvocationOperation invocation, string parameterName) {
        foreach (var argument in invocation.Arguments)
            if (argument.Parameter?.Name == parameterName)
                return argument.Value;

        return null;
    }

    private static bool IsPotentiallyTrue(IOperation operation) {
        var constantValue = operation.ConstantValue;
        if (!constantValue.HasValue) {
            return true;
        }

        return constantValue.Value is true;
    }
}
