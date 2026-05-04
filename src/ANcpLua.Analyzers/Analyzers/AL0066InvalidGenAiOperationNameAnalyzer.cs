
namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0066: Detects GenAI operation names that don't follow semantic conventions.
/// </summary>
/// <remarks>
///     <para>
///         GenAI operation names should be one of the standard values:
///         <list type="bullet">
///             <item>chat - for chat completions</item>
///             <item>generate_content - for content generation APIs</item>
///             <item>text_completion - for text completions</item>
///             <item>embeddings - for embedding generation</item>
///             <item>retrieval - for retrieval operations</item>
///             <item>create_agent, invoke_agent, execute_tool, invoke_workflow - for agent/workflow operations</item>
///         </list>
///     </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0066InvalidGenAiOperationNameAnalyzer : AlAnalyzer {
    /// <summary>The diagnostic identifier for AL0066.</summary>
    private const string DiagnosticId = "AL0066";

    private static readonly DiagnosticDescriptor s_rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.GenAI,
        DiagnosticSeverities.Suggestion);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_rule];

    /// <summary>Registers operation actions to analyze string literals used as operation names.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);

    private static void AnalyzeInvocation(OperationAnalysisContext context) {
        var invocation = (IInvocationOperation)context.Operation;

        if (invocation.TargetMethod.Name != "SetTag" ||
            invocation.Arguments.Length < 2 ||
            invocation.Arguments[0].Value.UnwrapAllConversions().ConstantValue is not { HasValue: true, Value: string tagName } ||
            !tagName.EqualsIgnoreCase("gen_ai.operation.name") ||
            invocation.Arguments[1].Value.UnwrapAllConversions().ConstantValue is not { HasValue: true, Value: string operationName } ||
            OpenTelemetryGenAiSemconvFacts.IsValidOperationName(operationName)) {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(s_rule, invocation.Arguments[1].Syntax.GetLocation(), operationName));
    }
}
