using ANcpLua.Analyzers.Core;

namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0087: Detects usage of string literals for semantic convention attribute names
///     and suggests using constant fields from GenAiAttributes or SemanticConventions classes.
/// </summary>
/// <remarks>
///     <para>
///         Using constant fields instead of string literals for attribute names provides:
///         <list type="bullet">
///             <item>IntelliSense support and discoverability</item>
///             <item>Compile-time typo prevention</item>
///             <item>Consistency with the latest semantic conventions</item>
///             <item>Centralized updates when conventions change</item>
///         </list>
///     </para>
///     <para>
///         The analyzer detects patterns like:
///         <code>
///         // Bad - string literal
///         activity.SetTag("gen_ai.system", "openai");
///         activity.SetTag("gen_ai.request.model", "gpt-4");
///
///         // Good - constant reference
///         activity.SetTag(GenAiAttributes.System, "openai");
///         activity.SetTag(GenAiAttributes.RequestModel, "gpt-4");
///         </code>
///     </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0087PreferConstantAttributeAnalyzer : AlAnalyzer {
    /// <summary>The diagnostic identifier for AL0087.</summary>
    public const string DiagnosticId = "AL0087";

    private static readonly DiagnosticDescriptor Rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.OpenTelemetry,
        DiagnosticSeverities.HiddenByDefault);

    /// <summary>
    ///     Known semantic convention attribute names mapped to their suggested constant references.
    /// </summary>
    /// <remarks>
    ///     Keys are snake_case attribute names, values are the suggested constant expression.
    /// </remarks>
    private static readonly Dictionary<string, string> KnownAttributes = new(StringComparer.Ordinal) {
        // GenAI semantic conventions
        ["gen_ai.system"] = "GenAiAttributes.System",
        ["gen_ai.provider.name"] = "GenAiAttributes.ProviderName",
        ["gen_ai.request.model"] = "GenAiAttributes.RequestModel",
        ["gen_ai.response.model"] = "GenAiAttributes.ResponseModel",
        ["gen_ai.operation.name"] = "GenAiAttributes.OperationName",
        ["gen_ai.usage.input_tokens"] = "GenAiAttributes.UsageInputTokens",
        ["gen_ai.usage.output_tokens"] = "GenAiAttributes.UsageOutputTokens",
        ["gen_ai.request.max_tokens"] = "GenAiAttributes.RequestMaxTokens",
        ["gen_ai.request.temperature"] = "GenAiAttributes.RequestTemperature",
        ["gen_ai.request.top_p"] = "GenAiAttributes.RequestTopP",
        ["gen_ai.request.stop_sequences"] = "GenAiAttributes.RequestStopSequences",
        ["gen_ai.request.presence_penalty"] = "GenAiAttributes.RequestPresencePenalty",
        ["gen_ai.request.frequency_penalty"] = "GenAiAttributes.RequestFrequencyPenalty",
        ["gen_ai.response.finish_reasons"] = "GenAiAttributes.ResponseFinishReasons",
        ["gen_ai.response.id"] = "GenAiAttributes.ResponseId",
        ["gen_ai.prompt"] = "GenAiAttributes.Prompt",
        ["gen_ai.completion"] = "GenAiAttributes.Completion",

        // HTTP semantic conventions
        ["http.request.method"] = "SemanticConventions.HttpRequestMethod",
        ["http.response.status_code"] = "SemanticConventions.HttpResponseStatusCode",
        ["url.full"] = "SemanticConventions.UrlFull",
        ["url.path"] = "SemanticConventions.UrlPath",
        ["url.scheme"] = "SemanticConventions.UrlScheme",
        ["server.address"] = "SemanticConventions.ServerAddress",
        ["server.port"] = "SemanticConventions.ServerPort",

        // Database semantic conventions
        ["db.system"] = "SemanticConventions.DbSystem",
        ["db.namespace"] = "SemanticConventions.DbNamespace",
        ["db.operation.name"] = "SemanticConventions.DbOperationName",
        ["db.query.text"] = "SemanticConventions.DbQueryText",

        // Messaging semantic conventions
        ["messaging.system"] = "SemanticConventions.MessagingSystem",
        ["messaging.destination.name"] = "SemanticConventions.MessagingDestinationName",
        ["messaging.operation.type"] = "SemanticConventions.MessagingOperationType",

        // Error semantic conventions
        ["error.type"] = "SemanticConventions.ErrorType",
        ["exception.type"] = "SemanticConventions.ExceptionType",
        ["exception.message"] = "SemanticConventions.ExceptionMessage",
        ["exception.stacktrace"] = "SemanticConventions.ExceptionStacktrace"
    };

    /// <summary>
    ///     Method names that indicate a telemetry context where attribute constants should be used.
    /// </summary>
    private static readonly HashSet<string> TelemetryMethodNames = new(StringComparer.Ordinal) {
        "SetTag",
        "SetAttribute",
        "AddTag",
        "SetStatus"
    };

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    /// <summary>Registers syntax node actions to analyze string literals for known attribute names.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterSyntaxNodeAction(AnalyzeStringLiteral, SyntaxKind.StringLiteralExpression);

    private static void AnalyzeStringLiteral(SyntaxNodeAnalysisContext context) {
        var literal = (LiteralExpressionSyntax)context.Node;
        var value = literal.Token.ValueText;

        if (string.IsNullOrEmpty(value)) {
            return;
        }

        // Check if this is a known attribute name
        if (!KnownAttributes.TryGetValue(value, out var suggestedConstant)) {
            return;
        }

        // Check if we're in a telemetry context
        if (!IsInTelemetryContext(literal)) {
            return;
        }

        context.ReportDiagnostic(Rule, literal.GetLocation(), suggestedConstant, value);
    }

    private static bool IsInTelemetryContext(SyntaxNode node) {
        var current = node.Parent;

        while (current is not null) {
            switch (current) {
                // Method invocations: activity.SetTag("gen_ai.system", value)
                case InvocationExpressionSyntax invocation:
                    if (IsLikelyTelemetryMethod(GetMethodName(invocation))) {
                        return true;
                    }

                    break;

                // Dictionary/collection indexers: tags["gen_ai.system"]
                case ElementAccessExpressionSyntax elementAccess:
                    if (IsLikelyTelemetryContainer(GetIdentifierName(elementAccess.Expression))) {
                        return true;
                    }

                    break;

                // Assignment in initializers: { "gen_ai.system", value }
                case InitializerExpressionSyntax:
                    return true;
            }

            current = current.Parent;
        }

        return false;
    }

    private static bool IsLikelyTelemetryMethod(string? methodName) =>
        methodName is not null && TelemetryMethodNames.Contains(methodName);

    private static bool IsLikelyTelemetryContainer(string? identifier) =>
        identifier is not null &&
        (identifier.ContainsIgnoreCase("ATTRIBUTE") ||
         identifier.ContainsIgnoreCase("TAG") ||
         identifier.ContainsIgnoreCase("ATTR"));

    private static string? GetMethodName(InvocationExpressionSyntax invocation) =>
        invocation.Expression switch {
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.Text,
            IdentifierNameSyntax identifier => identifier.Identifier.Text,
            _ => null
        };

    private static string? GetIdentifierName(ExpressionSyntax expression) =>
        expression switch {
            IdentifierNameSyntax identifier => identifier.Identifier.Text,
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.Text,
            _ => null
        };
}
