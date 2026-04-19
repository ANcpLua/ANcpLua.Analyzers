
namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0066: Detects GenAI operation names that don't follow semantic conventions.
/// </summary>
/// <remarks>
///     <para>
///         GenAI operation names should be one of the standard values:
///         <list type="bullet">
///             <item>chat - for chat completions</item>
///             <item>text_completion - for text completions</item>
///             <item>embeddings - for embedding generation</item>
///         </list>
///     </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0066InvalidGenAiOperationNameAnalyzer : AlAnalyzer {
    /// <summary>The diagnostic identifier for AL0066.</summary>
    private const string DiagnosticId = "AL0066";

    private static readonly HashSet<string> ValidOperationNames =
        new(StringComparer.OrdinalIgnoreCase) { "chat", "text_completion", "embeddings" };

    private static readonly DiagnosticDescriptor Rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.GenAI,
        DiagnosticSeverities.Suggestion);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    /// <summary>Registers operation actions to analyze string literals used as operation names.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);

    private static void AnalyzeInvocation(OperationAnalysisContext context) {
        var invocation = (IInvocationOperation)context.Operation;

        if (invocation.TargetMethod.Name != "SetTag" ||
            invocation.Arguments.Length < 2 ||
            invocation.Arguments[0].Value.ConstantValue is not { HasValue: true, Value: string tagName } ||
            !tagName.EqualsIgnoreCase("gen_ai.operation.name") ||
            invocation.Arguments[1].Value.ConstantValue is not { HasValue: true, Value: string operationName } ||
            ValidOperationNames.Contains(operationName)) {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.Arguments[1].Syntax.GetLocation(), operationName));
    }
}
