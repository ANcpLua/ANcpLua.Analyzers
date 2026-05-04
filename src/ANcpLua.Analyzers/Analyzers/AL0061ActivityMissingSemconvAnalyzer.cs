
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
    /// <summary>The diagnostic identifier for AL0061.</summary>
    private const string DiagnosticId = "AL0061";

    // Operation types and their expected semantic convention prefixes
    private static readonly Dictionary<string, string[]> s_operationTypePrefixes = new(StringComparer.OrdinalIgnoreCase) {
        ["http"] = ["http.", "url.", "server.", "client.", "network.", "user_agent."],
        ["db"] = ["db."],
        ["rpc"] = ["rpc.", "jsonrpc."],
        ["messaging"] = ["messaging."],
        ["faas"] = ["faas."],
        ["gen_ai"] = ["gen_ai.", "openai."]
    };

    private static readonly DiagnosticDescriptor s_rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.OpenTelemetry,
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
            InferOperationType(activityName) is not { } operationType ||
            !s_operationTypePrefixes.TryGetValue(operationType, out var expectedPrefixes)) {
            return;
        }

        var setTags = CollectSetTagCalls(invocation);
        if (!HasRelevantSemconv(setTags, expectedPrefixes)) {
            context.ReportDiagnostic(Diagnostic.Create(s_rule, invocation.Syntax.GetLocation(), activityName, operationType));
        }
    }

    private static bool HasRelevantSemconv(HashSet<string> tagNames, string[] expectedPrefixes) {
        foreach (var tagName in tagNames) {
            if (expectedPrefixes.Any(tagName.StartsWithIgnoreCase)) {
                return true;
            }

            if (OpenTelemetryDeprecatedSemconvCatalog.TryGetDeprecatedAttribute(tagName, out var deprecatedAttribute) &&
                expectedPrefixes.Any(deprecatedAttribute.Replacement.StartsWithIgnoreCase)) {
                return true;
            }

            if (OpenTelemetryDeprecatedSemconvCatalog.TryGetDeprecatedGenAiAttribute(tagName, out var genAiReplacement) &&
                expectedPrefixes.Any(genAiReplacement.StartsWithIgnoreCase)) {
                return true;
            }
        }

        return false;
    }

    private static string? GetActivityName(IInvocationOperation invocation) =>
        invocation.Arguments.Length > 0 &&
        invocation.Arguments[0].Value.ConstantValue is { HasValue: true, Value: string name }
            ? name
            : null;

    private static string? InferOperationType(string activityName) {
        foreach (var kvp in s_operationTypePrefixes) {
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
