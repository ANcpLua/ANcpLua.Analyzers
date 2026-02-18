
namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0105: Detects blocking calls (.Result, .Wait(), .GetAwaiter().GetResult()) inside async methods.
/// </summary>
/// <remarks>
///     <para>
///         Blocking on async code inside an async method can cause deadlocks when a
///         <c>SynchronizationContext</c> is present (ASP.NET, WPF, WinForms). Even without
///         a sync context, blocking wastes the thread pool thread that could be doing other work.
///     </para>
///     <para>
///         This analyzer detects:
///         <list type="bullet">
///             <item><c>task.Result</c> — blocks until the task completes</item>
///             <item><c>task.Wait()</c> — blocks until the task completes</item>
///             <item><c>task.GetAwaiter().GetResult()</c> — blocks without wrapping in AggregateException</item>
///         </list>
///     </para>
///     <para>
///         The fix is to <c>await</c> the task instead:
///         <list type="bullet">
///             <item><c>task.Result</c> → <c>await task</c></item>
///             <item><c>task.Wait()</c> → <c>await task</c></item>
///             <item><c>task.GetAwaiter().GetResult()</c> → <c>await task</c></item>
///         </list>
///     </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0105AvoidBlockingCallsInAsyncAnalyzer : AlAnalyzer {
    /// <summary>The diagnostic identifier for AL0105.</summary>
    public const string DiagnosticId = "AL0105";

    private static readonly DiagnosticDescriptor Rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.Threading,
        DiagnosticSeverity.Warning);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    /// <summary>Registers compilation start action to resolve Task types.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterCompilationStartAction(OnCompilationStart);

    private static void OnCompilationStart(CompilationStartAnalysisContext context) {
        var taskType = context.Compilation.GetTypeByMetadataName("System.Threading.Tasks.Task");
        var taskOfTType = context.Compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1");
        var valueTaskType = context.Compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask");
        var valueTaskOfTType = context.Compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask`1");

        if (taskType is null && taskOfTType is null) {
            return;
        }

        context.RegisterSyntaxNodeAction(
            ctx => AnalyzeMemberAccess(ctx, taskType, taskOfTType, valueTaskType, valueTaskOfTType),
            SyntaxKind.SimpleMemberAccessExpression);

        context.RegisterSyntaxNodeAction(
            ctx => AnalyzeInvocation(ctx, taskType, taskOfTType, valueTaskType, valueTaskOfTType),
            SyntaxKind.InvocationExpression);
    }

    /// <summary>Analyzes property access for .Result on Task types.</summary>
    private static void AnalyzeMemberAccess(
        SyntaxNodeAnalysisContext context,
        INamedTypeSymbol? taskType,
        INamedTypeSymbol? taskOfTType,
        INamedTypeSymbol? valueTaskType,
        INamedTypeSymbol? valueTaskOfTType) {
        var memberAccess = (MemberAccessExpressionSyntax)context.Node;

        // Only interested in .Result property access
        if (memberAccess.Name.Identifier.Text != "Result") {
            return;
        }

        // Must be inside an async context
        if (!AsyncContextHelper.IsInsideAsyncContext(memberAccess)) {
            return;
        }

        // Check if the expression type is a Task/ValueTask type
        var typeInfo = context.SemanticModel.GetTypeInfo(memberAccess.Expression, context.CancellationToken);
        if (typeInfo.Type is not { } expressionType) {
            return;
        }

        if (IsTaskLike(expressionType, taskType, taskOfTType, valueTaskType, valueTaskOfTType)) {
            context.ReportDiagnostic(Rule, memberAccess.Name.GetLocation(), ".Result");
        }
    }

    /// <summary>Analyzes method calls for .Wait() and .GetAwaiter().GetResult() on Task types.</summary>
    private static void AnalyzeInvocation(
        SyntaxNodeAnalysisContext context,
        INamedTypeSymbol? taskType,
        INamedTypeSymbol? taskOfTType,
        INamedTypeSymbol? valueTaskType,
        INamedTypeSymbol? valueTaskOfTType) {
        var invocation = (InvocationExpressionSyntax)context.Node;

        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess) {
            return;
        }

        // Must be inside an async context
        if (!AsyncContextHelper.IsInsideAsyncContext(invocation)) {
            return;
        }

        var methodName = memberAccess.Name.Identifier.Text;

        switch (methodName) {
            // task.Wait()
            case "Wait": {
                var typeInfo = context.SemanticModel.GetTypeInfo(memberAccess.Expression, context.CancellationToken);
                if (typeInfo.Type is { } expressionType &&
                    IsTaskLike(expressionType, taskType, taskOfTType, valueTaskType, valueTaskOfTType)) {
                    context.ReportDiagnostic(Rule, memberAccess.Name.GetLocation(), ".Wait()");
                }

                break;
            }

            // task.GetAwaiter().GetResult()
            case "GetResult" when memberAccess.Expression is InvocationExpressionSyntax innerInvocation &&
                                  innerInvocation.Expression is MemberAccessExpressionSyntax innerMember &&
                                  innerMember.Name.Identifier.Text == "GetAwaiter": {
                var typeInfo = context.SemanticModel.GetTypeInfo(innerMember.Expression, context.CancellationToken);
                if (typeInfo.Type is { } expressionType &&
                    IsTaskLike(expressionType, taskType, taskOfTType, valueTaskType, valueTaskOfTType)) {
                    context.ReportDiagnostic(Rule, memberAccess.GetLocation(), ".GetAwaiter().GetResult()");
                }

                break;
            }
        }
    }

    /// <summary>Determines if a type is Task, Task&lt;T&gt;, ValueTask, or ValueTask&lt;T&gt;.</summary>
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
