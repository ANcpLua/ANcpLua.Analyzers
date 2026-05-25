
namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL1305: Detects blocking calls (.Result, .Wait(), .GetAwaiter().GetResult()) inside async methods.
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
public sealed partial class Al1305AvoidBlockingCallsInAsyncAnalyzer : AlAnalyzer {
    /// <summary>The diagnostic identifier for AL1305.</summary>
    private const string DiagnosticId = "AL1305";

    private enum KnownType { Task, TaskOfT, ValueTask, ValueTaskOfT }

    private static readonly string[] s_knownTypeNames = [
        "System.Threading.Tasks.Task",
        "System.Threading.Tasks.Task`1",
        "System.Threading.Tasks.ValueTask",
        "System.Threading.Tasks.ValueTask`1"
    ];

    private static readonly DiagnosticDescriptor s_rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.Threading,
        DiagnosticSeverity.Warning);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_rule];

    /// <summary>Registers compilation start action to resolve Task types.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterCompilationStartAction(OnCompilationStart);

    private static void OnCompilationStart(CompilationStartAnalysisContext context) {
        var cache = new TypeCache<KnownType>(type => context.Compilation.GetTypeByMetadataName(s_knownTypeNames[(int)type]));

        if (cache.Get(KnownType.Task) is null && cache.Get(KnownType.TaskOfT) is null) {
            return;
        }

        context.RegisterSyntaxNodeAction(
            ctx => AnalyzeMemberAccess(ctx, cache),
            SyntaxKind.SimpleMemberAccessExpression);

        context.RegisterSyntaxNodeAction(
            ctx => AnalyzeInvocation(ctx, cache),
            SyntaxKind.InvocationExpression);
    }

    private static void AnalyzeMemberAccess(SyntaxNodeAnalysisContext context, TypeCache<KnownType> cache) {
        var memberAccess = (MemberAccessExpressionSyntax)context.Node;

        if (memberAccess.Name.Identifier.Text != "Result") {
            return;
        }

        if (!AsyncContextHelper.IsInsideAsyncContext(memberAccess)) {
            return;
        }

        if (context.SemanticModel.GetTypeInfo(memberAccess.Expression, context.CancellationToken).Type is { } expressionType &&
            IsTaskLike(expressionType, cache)) {
            context.ReportDiagnostic(s_rule, memberAccess.Name.GetLocation(), ".Result");
        }
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context, TypeCache<KnownType> cache) {
        var invocation = (InvocationExpressionSyntax)context.Node;

        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess) {
            return;
        }

        if (!AsyncContextHelper.IsInsideAsyncContext(invocation)) {
            return;
        }

        switch (memberAccess.Name.Identifier.Text) {
            case "Wait": {
                if (context.SemanticModel.GetTypeInfo(memberAccess.Expression, context.CancellationToken).Type is { } expressionType &&
                    IsTaskLike(expressionType, cache)) {
                    context.ReportDiagnostic(s_rule, memberAccess.Name.GetLocation(), ".Wait()");
                }

                break;
            }

            case "GetResult" when memberAccess.Expression is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Name.Identifier.Text: "GetAwaiter" } innerMember }: {
                if (context.SemanticModel.GetTypeInfo(innerMember.Expression, context.CancellationToken).Type is { } expressionType &&
                    IsTaskLike(expressionType, cache)) {
                    context.ReportDiagnostic(s_rule, memberAccess.Name.GetLocation(), ".GetAwaiter().GetResult()");
                }

                break;
            }
        }
    }

    private static bool IsTaskLike(ITypeSymbol type, TypeCache<KnownType> cache) =>
        cache.IsType(type, KnownType.Task) ||
        cache.IsTypeDefinition(type, KnownType.TaskOfT) ||
        cache.IsType(type, KnownType.ValueTask) ||
        cache.IsTypeDefinition(type, KnownType.ValueTaskOfT);
}
