using ANcpLua.Analyzers.Core;

namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0061: Detects Activity/Span creation without semantic convention attributes.
/// </summary>
/// <remarks>
///     <para>
///         OpenTelemetry Activities (Spans) should include semantic convention attributes
///         appropriate for their operation type to enable correlation, filtering, and analysis.
///     </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0061ActivityMissingSemconvAnalyzer : AlAnalyzer {
    // Operation types and their expected semantic convention prefixes
    private static readonly Dictionary<string, string[]> OperationTypePrefixes = new(StringComparer.OrdinalIgnoreCase) {
        ["http"] = ["http.", "url.", "server.", "client."],
        ["db"] = ["db."],
        ["rpc"] = ["rpc."],
        ["messaging"] = ["messaging."],
        ["faas"] = ["faas."],
        ["gen_ai"] = ["gen_ai."]
    };

    private static readonly DiagnosticDescriptor Rule = CreateRule(
        DiagnosticIds.ActivityMissingSemconv,
        DiagnosticCategories.OpenTelemetry,
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

        // Get the activity name
        if (GetActivityName(invocation) is not { } activityName) {
            return;
        }

        // Determine the operation type from the activity name
        if (InferOperationType(activityName) is not { } operationType) {
            return; // Can't determine operation type, skip analysis
        }

        // Collect SetTag calls in the same scope
        var setTags = CollectSetTagCalls(invocation);

        // Check if any semantic convention attributes for this operation type are present
        if (!OperationTypePrefixes.TryGetValue(operationType, out var expectedPrefixes)) {
            return;
        }

        var hasRelevantTags = setTags.Any(tag =>
            expectedPrefixes.Any(tag.StartsWithIgnoreCase));

        if (!hasRelevantTags) {
            context.ReportDiagnostic(Diagnostic.Create(
                Rule,
                invocation.Syntax.GetLocation(),
                activityName,
                operationType));
        }
    }

    private static string? GetActivityName(IInvocationOperation invocation) {
        if (invocation.Arguments.Length > 0 &&
            invocation.Arguments[0].Value.ConstantValue is { HasValue: true, Value: string name }) {
            return name;
        }

        return null;
    }

    private static string? InferOperationType(string activityName) {
        foreach (var kvp in OperationTypePrefixes) {
            if (activityName.ContainsIgnoreCase(kvp.Key)) {
                return kvp.Key;
            }
        }

        // Additional heuristics
        if (activityName.ContainsIgnoreCase("request") ||
            activityName.ContainsIgnoreCase("response") ||
            activityName.ContainsIgnoreCase("get") ||
            activityName.ContainsIgnoreCase("post")) {
            return "http";
        }

        if (activityName.ContainsIgnoreCase("query") ||
            activityName.ContainsIgnoreCase("execute") ||
            activityName.ContainsIgnoreCase("select") ||
            activityName.ContainsIgnoreCase("insert")) {
            return "db";
        }

        if (activityName.ContainsIgnoreCase("chat") ||
            activityName.ContainsIgnoreCase("completion") ||
            activityName.ContainsIgnoreCase("embedding") ||
            activityName.ContainsIgnoreCase("llm")) {
            return "gen_ai";
        }

        return null;
    }

    private static HashSet<string> CollectSetTagCalls(IInvocationOperation startActivity) {
        var tags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Walk sibling operations looking for SetTag calls
        if (startActivity.Parent is not { } parent) {
            return tags;
        }

        foreach (var child in parent.ChildOperations) {
            if (child is IInvocationOperation childInvocation &&
                childInvocation.TargetMethod.Name == "SetTag" &&
                childInvocation.Arguments.Length >= 1 &&
                childInvocation.Arguments[0].Value.ConstantValue is { HasValue: true, Value: string tagName }) {
                tags.Add(tagName);
            }
        }

        return tags;
    }
}
