
namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0064: Detects GenAI spans that are missing required semantic convention attributes.
/// </summary>
/// <remarks>
///     <para>
///         GenAI spans require these attributes for proper observability:
///         <list type="bullet">
///             <item>gen_ai.provider.name - The GenAI provider (e.g., "openai")</item>
///             <item>gen_ai.request.model - The model name</item>
///             <item>gen_ai.operation.name - The operation type</item>
///         </list>
///     </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0064GenAiMissingRequiredAttributesAnalyzer : AlAnalyzer {
    /// <summary>The diagnostic identifier for AL0064.</summary>
    private const string DiagnosticId = "AL0064";

    private static readonly string[] s_requiredGenAiAttributes = OpenTelemetryGenAiSemconvFacts.s_requiredAttributeKeys;

    private static readonly DiagnosticDescriptor s_rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.GenAI,
        DiagnosticSeverities.Suggestion);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_rule];

    /// <summary>Registers operation actions to analyze Activity.StartActivity calls.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);

    private static void AnalyzeInvocation(OperationAnalysisContext context) {
        var invocation = (IInvocationOperation)context.Operation;

        if (invocation.TargetMethod.Name != "StartActivity" ||
            GetActivityName(invocation) is not { } activityName ||
            !IsGenAiActivity(activityName)) {
            return;
        }

        var setTags = CollectSetTagCalls(invocation);

        foreach (var requiredAttribute in s_requiredGenAiAttributes) {
            if (!setTags.Contains(requiredAttribute, StringComparer.OrdinalIgnoreCase)) {
                context.ReportDiagnostic(Diagnostic.Create(s_rule, invocation.Syntax.GetLocation(), activityName, requiredAttribute));
            }
        }
    }

    private static string? GetActivityName(IInvocationOperation invocation) =>
        invocation.Arguments.Length > 0 &&
        invocation.Arguments[0].Value.ConstantValue is { HasValue: true, Value: string name }
            ? name
            : null;

    private static bool IsGenAiActivity(string activityName) =>
        activityName.ContainsIgnoreCase("gen_ai") ||
        activityName.ContainsIgnoreCase("genai") ||
        activityName.ContainsIgnoreCase("chat") ||
        activityName.ContainsIgnoreCase("completion") ||
        activityName.ContainsIgnoreCase("embedding");

    private static HashSet<string> CollectSetTagCalls(IInvocationOperation startActivity) {
        var tags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var current = startActivity.Parent; current is not null; current = current.Parent) {
            if (current is IBlockOperation block) {
                CollectSetTagCallsRecursive(block, tags);
                break;
            }
        }

        return tags;
    }

    private static void CollectSetTagCallsRecursive(IOperation operation, HashSet<string> tags) {
        if (operation is IInvocationOperation { TargetMethod.Name: "SetTag", Arguments.Length: >= 1 } invocation &&
            invocation.Arguments[0].Value.ConstantValue is { HasValue: true, Value: string tagName }) {
            tags.Add(tagName);
            return;
        }

        foreach (var child in operation.ChildOperations) {
            CollectSetTagCallsRecursive(child, tags);
        }
    }
}
