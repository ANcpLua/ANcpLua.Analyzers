
namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0074: Detects deprecated GenAI semantic convention attribute names.
/// </summary>
/// <remarks>
///     <para>
///         The OpenTelemetry GenAI semantic conventions have evolved, with some
///         attribute names being renamed or deprecated. This analyzer helps
///         migrate to the current conventions for better interoperability.
///     </para>
///     <para>
///         Deprecated attributes detected:
///         <list type="bullet">
///             <item>gen_ai.system -> gen_ai.system (still valid, but check usage)</item>
///             <item>gen_ai.prompt.tokens -> gen_ai.usage.input_tokens</item>
///             <item>gen_ai.completion.tokens -> gen_ai.usage.output_tokens</item>
///             <item>prompt_tokens -> gen_ai.usage.input_tokens</item>
///             <item>completion_tokens -> gen_ai.usage.output_tokens</item>
///         </list>
///     </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0074DeprecatedGenAiAttributeAnalyzer : AlAnalyzer {
    /// <summary>
    ///     Mapping of deprecated GenAI attribute names to their replacements.
    /// </summary>

    /// <summary>The diagnostic identifier for AL0074.</summary>
    public const string DiagnosticId = "AL0074";

    private static readonly DiagnosticDescriptor s_rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.GenAI,
        DiagnosticSeverities.Suggestion);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_rule];

    internal static bool TryGetDeprecatedAttribute(string attributeName, [NotNullWhen(true)] out string? replacement) =>
        OpenTelemetryDeprecatedSemconvCatalog.TryGetDeprecatedGenAiAttribute(attributeName, out replacement);

    /// <summary>Registers syntax node actions to analyze string literals for deprecated GenAI attributes.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterSyntaxNodeAction(AnalyzeStringLiteral, SyntaxKind.StringLiteralExpression);

    private static void AnalyzeStringLiteral(SyntaxNodeAnalysisContext context) {
        var literal = (LiteralExpressionSyntax)context.Node;
        var value = literal.Token.ValueText;

        if (string.IsNullOrEmpty(value)
            || !TryGetDeprecatedAttribute(value, out var replacement)
            || !IsInTelemetryContext(literal)) {
            return;
        }

        var properties = ImmutableDictionary.CreateBuilder<string, string?>();
        properties.Add("Replacement", replacement);

        context.ReportDiagnostic(Diagnostic.Create(
            s_rule,
            literal.GetLocation(),
            properties.ToImmutable(),
            value,
            replacement));
    }

    private static bool IsInTelemetryContext(SyntaxNode node) {
        var current = node.Parent;

        while (current is not null) {
            switch (current) {
                // Dictionary-initializer key (`["foo"] = value`) is lookup-table data, not a tag write.
                // Short-circuit before the InitializerExpressionSyntax case below would spuriously match.
                case ImplicitElementAccessSyntax:
                    return false;
                case ElementAccessExpressionSyntax elementAccess
                    when IsLikelyTelemetryContainer(GetIdentifierName(elementAccess.Expression)):
                case InvocationExpressionSyntax invocation
                    when IsLikelyTelemetryMethod(GetMethodName(invocation)):
                case InitializerExpressionSyntax:
                case AnonymousObjectMemberDeclaratorSyntax:
                    return true;
            }

            current = current.Parent;
        }

        return false;
    }

    private static bool IsLikelyTelemetryContainer(string? identifier) {
        if (identifier is null or { Length: 0 }) {
            return false;
        }

        var upper = identifier.ToUpperInvariant();
        return upper.ContainsOrdinal("TAG") ||
               upper.ContainsOrdinal("ATTR") ||
               upper.ContainsOrdinal("PROPERTY") ||
               upper.ContainsOrdinal("METADATA") ||
               upper.ContainsOrdinal("SPAN") ||
               upper.ContainsOrdinal("ACTIVITY");
    }

    private static bool IsLikelyTelemetryMethod(string? methodName) {
        if (methodName is null or { Length: 0 }) {
            return false;
        }

        return methodName switch {
            "SetTag" or "AddTag" or "SetAttribute" or "AddAttribute" => true,
            "SetStatus" or "RecordException" => true,
            _ when methodName.EndsWithOrdinal("Tag") => true,
            _ when methodName.EndsWithOrdinal("Attribute") => true,
            _ when methodName.StartsWithOrdinal("SetTag") => true,
            _ when methodName.StartsWithOrdinal("AddTag") => true,
            _ when methodName.StartsWithOrdinal("Record") => true,
            _ => false
        };
    }

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
