
namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0112: Detects fire-and-forget task discards (<c>_ = SomeAsyncMethod()</c>).
/// </summary>
/// <remarks>
///     <para>
///         Discarding a Task with <c>_ = ...</c> causes fire-and-forget behavior where exceptions
///         thrown by the asynchronous operation are silently lost. This makes failures invisible
///         and can lead to data corruption, resource leaks, or cascading issues that are extremely
///         difficult to diagnose in production.
///     </para>
///     <para>
///         This analyzer detects <c>ISimpleAssignmentOperation</c> where the target is
///         an <c>IDiscardOperation</c> and the value is an invocation returning
///         <c>Task</c>, <c>Task&lt;T&gt;</c>, <c>ValueTask</c>, or <c>ValueTask&lt;T&gt;</c>.
///     </para>
///     <para>
///         The fix is to await the task, store it for later observation, or add a continuation
///         with explicit error handling.
///     </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0112FireAndForgetTaskAnalyzer : AlAnalyzer {
    /// <summary>The diagnostic identifier for AL0112.</summary>
    private const string DiagnosticId = "AL0112";

    private static readonly DiagnosticDescriptor s_rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.Reliability,
        DiagnosticSeverity.Warning);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_rule];

    /// <summary>Registers compilation start action to resolve Task types.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterCompilationStartAction(OnCompilationStart);

    private static void OnCompilationStart(CompilationStartAnalysisContext context) {
        var taskType = context.Compilation.GetTypeByMetadataName("System.Threading.Tasks.Task");
        var taskOfTType = context.Compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1");
        var valueTaskType = context.Compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask");
        var valueTaskOfTType = context.Compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask`1");

        if (taskType is null && taskOfTType is null && valueTaskType is null && valueTaskOfTType is null) {
            return;
        }

        context.RegisterOperationAction(
            ctx => AnalyzeAssignment(ctx, taskType, taskOfTType, valueTaskType, valueTaskOfTType),
            OperationKind.SimpleAssignment);
    }

    private static void AnalyzeAssignment(
        OperationAnalysisContext context,
        INamedTypeSymbol? taskType,
        INamedTypeSymbol? taskOfTType,
        INamedTypeSymbol? valueTaskType,
        INamedTypeSymbol? valueTaskOfTType) {
        var assignment = (ISimpleAssignmentOperation)context.Operation;

        if (assignment.Target is not IDiscardOperation) {
            return;
        }

        if (assignment.Value is not IInvocationOperation invocation) {
            return;
        }

        var returnType = invocation.TargetMethod.ReturnType;

        if (!IsTaskLike(returnType, taskType, taskOfTType, valueTaskType, valueTaskOfTType)) {
            return;
        }

        var methodName = invocation.TargetMethod.Name;
        context.ReportDiagnostic(s_rule, assignment.Syntax.GetLocation(), methodName);
    }

    private static bool IsTaskLike(
        ITypeSymbol type,
        INamedTypeSymbol? taskType,
        INamedTypeSymbol? taskOfTType,
        INamedTypeSymbol? valueTaskType,
        INamedTypeSymbol? valueTaskOfTType) {
        if (taskType is not null && type.IsEqualTo(taskType)) {
            return true;
        }

        if (taskOfTType is not null && type.OriginalDefinition.IsEqualTo(taskOfTType)) {
            return true;
        }

        if (valueTaskType is not null && type.IsEqualTo(valueTaskType)) {
            return true;
        }

        return valueTaskOfTType is not null && type.OriginalDefinition.IsEqualTo(valueTaskOfTType);
    }
}
