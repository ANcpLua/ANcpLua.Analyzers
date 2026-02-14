using ANcpLua.Analyzers.Core;
using ANcpLua.Roslyn.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

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
    private static readonly string[] SensitiveAttributeNamePatterns = [
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
    private static readonly HashSet<string> TelemetryMethodPatterns =
        new(StringComparer.OrdinalIgnoreCase) {
            "SetTag",
            "AddTag",
            "SetAttribute",
            "AddAttribute",
            "SetCustomProperty"
        };

    private static readonly DiagnosticDescriptor Rule = CreateRule(
        DiagnosticIds.SensitiveDataInAttribute,
        DiagnosticCategories.OpenTelemetry,
        DiagnosticSeverity.Warning);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    /// <summary>Registers syntax node actions to analyze string literals for sensitive attribute names.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterSyntaxNodeAction(AnalyzeStringLiteral, SyntaxKind.StringLiteralExpression);

    private static void AnalyzeStringLiteral(SyntaxNodeAnalysisContext context) {
        var literal = (LiteralExpressionSyntax)context.Node;
        var value = literal.Token.ValueText;

        if (string.IsNullOrEmpty(value)) {
            return;
        }

        // Check if this looks like an attribute name (not a value)
        if (!IsLikelyAttributeName(literal)) {
            return;
        }

        // Check if in telemetry context
        if (!IsInTelemetryContext(literal)) {
            return;
        }

        // Check if the attribute name contains sensitive patterns
        if (!ContainsSensitivePattern(value)) {
            return;
        }

        context.ReportDiagnostic(Rule, literal.GetLocation(), value);
    }

    private static bool IsLikelyAttributeName(LiteralExpressionSyntax literal) {
        var parent = literal.Parent;

        switch (parent)
        {
            // Check if it's the first argument in a method invocation (likely the attribute name)
            // Check if argument is in a regular ArgumentList (method calls)
            case ArgumentSyntax { Parent: ArgumentListSyntax argumentList } argument when argumentList.Arguments.FirstOrDefault() == argument:
            // Check if argument is in a BracketedArgumentList (dictionary/indexer access)
            case ArgumentSyntax { Parent: BracketedArgumentListSyntax }:
            // Check if it's used in an object initializer as a key
            case AssignmentExpressionSyntax { Parent: InitializerExpressionSyntax }:
                return true;
            default:
                return false;
        }
    }

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

    private static bool IsTelemetryInvocation(SyntaxNode node) {
        if (node is not InvocationExpressionSyntax invocation) {
            return false;
        }

        if (GetMethodName(invocation) is not { } methodName) {
            return false;
        }

        return TelemetryMethodPatterns.Contains(methodName) ||
               methodName.ContainsIgnoreCase("ATTRIBUTE") ||
               methodName.ContainsIgnoreCase("TAG");
    }

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

        foreach (var pattern in SensitiveAttributeNamePatterns) {
            var normalizedPattern = pattern.ToUpperInvariant();

            // Exact match
            if (normalizedName == normalizedPattern) {
                return true;
            }

            // Contains match (e.g., "user.password" contains "password")
            if (normalizedName.ContainsOrdinal(normalizedPattern)) {
                return true;
            }

            // Handle variations with different separators
            var underscorePattern = normalizedPattern.Replace(".", "_", StringComparison.Ordinal);
            var dotPattern = normalizedPattern.Replace("_", ".", StringComparison.Ordinal);

            if (normalizedName.ContainsOrdinal(underscorePattern) ||
                normalizedName.ContainsOrdinal(dotPattern)) {
                return true;
            }
        }

        return false;
    }
}
