
namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0088: Detects potential PII or credential data in span attributes.
/// </summary>
/// <remarks>
///     <para>
///         Span attributes containing sensitive data (passwords, secrets, tokens, API keys,
///         SSNs, credit card numbers) can leak sensitive information to telemetry backends
///         where it may be stored, logged, or exposed to unauthorized users.
///     </para>
///     <para>
///         The analyzer detects sensitive patterns in two ways:
///         <list type="bullet">
///             <item>Attribute names containing sensitive keywords (password, secret, token, etc.)</item>
///             <item>Values coming from variables with sensitive names</item>
///         </list>
///     </para>
///     <para>
///         Context detection uses heuristics: the analyzer looks for patterns like
///         SetTag, AddTag, dictionary indexers on telemetry containers, and invocations
///         of methods containing "Attribute" or "Tag".
///     </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0088SensitiveDataInAttributeAnalyzer : AlAnalyzer {
    /// <summary>
    ///     Patterns in attribute names that indicate sensitive data.
    /// </summary>
    private static readonly string[] s_sensitiveAttributeNamePatterns = [
        // Credentials
        "password",
        "passwd",
        "pwd",
        "secret",
        "credential",
        "credentials",
        "auth",
        "authorization",
        "bearer",

        // Tokens and keys
        "token",
        "api_key",
        "apikey",
        "api.key",
        "private_key",
        "privatekey",
        "private.key",
        "access_key",
        "accesskey",
        "access.key",
        "secret_key",
        "secretkey",
        "secret.key",
        "encryption_key",
        "encryptionkey",

        // PII
        "ssn",
        "social_security",
        "socialsecurity",
        "social.security",
        "credit_card",
        "creditcard",
        "credit.card",
        "card_number",
        "cardnumber",
        "card.number",
        "cvv",
        "pin",

        // Connection strings
        "connection_string",
        "connectionstring",
        "connection.string",
        "conn_str"
    ];

    /// <summary>
    ///     Known telemetry method patterns.
    /// </summary>
    private static readonly HashSet<string> s_telemetryMethodPatterns =
        new(StringComparer.OrdinalIgnoreCase) {
            "SetTag",
            "AddTag",
            "SetAttribute",
            "AddAttribute",
            "SetCustomProperty"
        };

    /// <summary>The diagnostic identifier for AL0088.</summary>
    private const string DiagnosticId = "AL0088";

    private static readonly DiagnosticDescriptor s_rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.OpenTelemetry,
        DiagnosticSeverity.Warning);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_rule];

    /// <summary>Registers syntax node actions to analyze string literals for sensitive attribute names.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterSyntaxNodeAction(AnalyzeStringLiteral, SyntaxKind.StringLiteralExpression);

    private static void AnalyzeStringLiteral(SyntaxNodeAnalysisContext context) {
        var literal = (LiteralExpressionSyntax)context.Node;
        var value = literal.Token.ValueText;

        if (string.IsNullOrEmpty(value)
            || !IsLikelyAttributeName(literal)
            || !IsInTelemetryContext(literal)
            || !ContainsSensitivePattern(value)) {
            return;
        }

        context.ReportDiagnostic(s_rule, literal.GetLocation(), value);
    }

    private static bool IsLikelyAttributeName(LiteralExpressionSyntax literal) =>
        literal.Parent switch {
            // First argument in a method call (key position)
            ArgumentSyntax { Parent: ArgumentListSyntax argumentList } argument
                when argumentList.Arguments.FirstOrDefault() == argument => true,
            // Dictionary/indexer access
            ArgumentSyntax { Parent: BracketedArgumentListSyntax } => true,
            // Key in object initializer
            AssignmentExpressionSyntax { Parent: InitializerExpressionSyntax } => true,
            _ => false
        };

    private static bool IsInTelemetryContext(SyntaxNode node) {
        var current = node.Parent;

        while (current is not null) {
            if (IsTelemetryElementAccess(current) ||
                IsTelemetryInvocation(current) ||
                IsTelemetryInitializer(current)) {
                return true;
            }

            current = current.Parent;
        }

        return false;
    }

    private static bool IsTelemetryElementAccess(SyntaxNode node) =>
        node is ElementAccessExpressionSyntax elementAccess &&
        GetIdentifierName(elementAccess.Expression) is { } identifier &&
        IsLikelyTelemetryContainer(identifier);

    private static bool IsTelemetryInvocation(SyntaxNode node) =>
        node is InvocationExpressionSyntax invocation
        && GetMethodName(invocation) is { } methodName
        && (s_telemetryMethodPatterns.Contains(methodName)
            || methodName.ContainsIgnoreCase("ATTRIBUTE")
            || methodName.ContainsIgnoreCase("TAG"));

    private static bool IsTelemetryInitializer(SyntaxNode node) =>
        node is InitializerExpressionSyntax { Parent: ObjectCreationExpressionSyntax creation } &&
        IsTelemetryTypeName(creation.Type.ToString());

    private static bool IsTelemetryTypeName(string typeName) =>
        typeName.ContainsOrdinal("Tag") ||
        typeName.ContainsOrdinal("Attribute") ||
        typeName.ContainsOrdinal("KeyValuePair");

    private static bool IsLikelyTelemetryContainer(string identifier) =>
        identifier.ContainsIgnoreCase("ATTRIBUTE") ||
        identifier.ContainsIgnoreCase("TAG") ||
        identifier.ContainsIgnoreCase("ATTR") ||
        identifier.EqualsIgnoreCase("ATTRS") ||
        identifier.ContainsIgnoreCase("SPAN") ||
        identifier.ContainsIgnoreCase("ACTIVITY");

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

    private static bool ContainsSensitivePattern(string attributeName) {
        var normalizedName = attributeName.ToUpperInvariant();

        foreach (var pattern in s_sensitiveAttributeNamePatterns) {
            var normalizedPattern = pattern.ToUpperInvariant();

            if (normalizedName == normalizedPattern || normalizedName.ContainsOrdinal(normalizedPattern)) {
                return true;
            }

            // Handle separator variations (dot vs underscore)
            if (normalizedName.ContainsOrdinal(normalizedPattern.ReplaceOrdinal(".", "_"))
                || normalizedName.ContainsOrdinal(normalizedPattern.ReplaceOrdinal("_", "."))) {
                return true;
            }
        }

        return false;
    }
}
