using ANcpLua.Analyzers.Core;

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
    public const string DiagnosticId = "AL0064";

    private static readonly string[] RequiredGenAiAttributes = [
        "gen_ai.provider.name",
        "gen_ai.request.model",
        "gen_ai.operation.name"
    ];

    private static readonly DiagnosticDescriptor Rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.GenAI,
        DiagnosticSeverities.Suggestion);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    /// <summary>Registers operation actions to analyze Activity.StartActivity calls.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);

    private static void AnalyzeInvocation(OperationAnalysisContext context) {
        var invocation = (IInvocationOperation)context.Operation;

        // Look for ActivitySource.StartActivity calls
        if (invocation.TargetMethod.Name != "StartActivity") {
            return;
        }

        // Check if the activity name contains "gen_ai" or "genai" (case insensitive)
        var activityName = GetActivityName(invocation);
        if (activityName is null || !IsGenAiActivity(activityName)) {
            return;
        }

        // Collect set tags in the current method context
        var setTags = CollectSetTagCalls(invocation);

        // Check for missing required attributes
        foreach (var requiredAttribute in RequiredGenAiAttributes) {
            if (!setTags.Contains(requiredAttribute, StringComparer.OrdinalIgnoreCase)) {
                context.ReportDiagnostic(Diagnostic.Create(
                    Rule,
                    invocation.Syntax.GetLocation(),
                    activityName,
                    requiredAttribute));
            }
        }
    }

    private static string? GetActivityName(IInvocationOperation invocation) {
        // First argument is typically the activity name
        if (invocation.Arguments.Length > 0 &&
            invocation.Arguments[0].Value.ConstantValue is { HasValue: true, Value: string name }) {
            return name;
        }

        return null;
    }

    private static bool IsGenAiActivity(string activityName) =>
        activityName.ContainsIgnoreCase("gen_ai") ||
        activityName.ContainsIgnoreCase("genai") ||
        activityName.ContainsIgnoreCase("chat") ||
        activityName.ContainsIgnoreCase("completion") ||
        activityName.ContainsIgnoreCase("embedding");

    private static HashSet<string> CollectSetTagCalls(IInvocationOperation startActivity) {
        var tags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (FindEnclosingBlock(startActivity) is not { } block) {
            return tags;
        }

        CollectSetTagCallsRecursive(block, tags);
        return tags;
    }

    private static IBlockOperation? FindEnclosingBlock(IOperation operation) {
        var current = operation.Parent;
        while (current is not null) {
            if (current is IBlockOperation block) {
                return block;
            }

            current = current.Parent;
        }

        return null;
    }

    private static void CollectSetTagCallsRecursive(IOperation operation, HashSet<string> tags) {
        if (operation is IInvocationOperation invocation &&
            invocation.TargetMethod.Name == "SetTag" &&
            invocation.Arguments.Length >= 1 &&
            invocation.Arguments[0].Value.ConstantValue is { HasValue: true, Value: string tagName }) {
            tags.Add(tagName);
            return;
        }

        foreach (var child in operation.ChildOperations) {
            CollectSetTagCallsRecursive(child, tags);
        }
    }
}
