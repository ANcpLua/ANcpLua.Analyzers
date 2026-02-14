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

#pragma warning disable AL0074 // These strings are the deprecated names we detect — not telemetry usage
    private static readonly Dictionary<string, string> DeprecatedAttributes = new(StringComparer.Ordinal) {
        // Old token counting attributes (pre-1.27.0 style)
        ["gen_ai.prompt.tokens"] = "gen_ai.usage.input_tokens",
        ["gen_ai.completion.tokens"] = "gen_ai.usage.output_tokens",
        ["gen_ai.response.tokens"] = "gen_ai.usage.output_tokens",

        // Very old style (some early implementations)
        ["prompt_tokens"] = "gen_ai.usage.input_tokens",
        ["completion_tokens"] = "gen_ai.usage.output_tokens",
        ["total_tokens"] = "gen_ai.usage.input_tokens + gen_ai.usage.output_tokens",

        // Deprecated model naming
        ["gen_ai.model"] = "gen_ai.request.model",
        ["model"] = "gen_ai.request.model",

        // Deprecated operation naming
        ["gen_ai.operation"] = "gen_ai.operation.name",
        ["operation"] = "gen_ai.operation.name",

        // Old request/response naming
        ["gen_ai.request.prompt"] = "gen_ai.prompt",
        ["gen_ai.response.completion"] = "gen_ai.completion"
    };
#pragma warning restore AL0074

    private static readonly DiagnosticDescriptor Rule = CreateRule(
        DiagnosticIds.DeprecatedGenAiAttribute,
        DiagnosticCategories.GenAI,
        DiagnosticSeverities.Suggestion);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    /// <summary>Registers syntax node actions to analyze string literals for deprecated GenAI attributes.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterSyntaxNodeAction(AnalyzeStringLiteral, SyntaxKind.StringLiteralExpression);

    private static void AnalyzeStringLiteral(SyntaxNodeAnalysisContext context) {
        var literal = (LiteralExpressionSyntax)context.Node;
        var value = literal.Token.ValueText;

        if (string.IsNullOrEmpty(value)) {
            return;
        }

        // Check if this is a deprecated attribute name
        if (!DeprecatedAttributes.TryGetValue(value, out var replacement)) {
            return;
        }

        // Only flag if in a telemetry context (avoid false positives)
        if (!IsInTelemetryContext(literal)) {
            return;
        }

        // Include replacement in properties for code fix
        var properties = ImmutableDictionary.CreateBuilder<string, string?>();
        properties.Add("Replacement", replacement);

        context.ReportDiagnostic(Diagnostic.Create(
            Rule,
            literal.GetLocation(),
            properties.ToImmutable(),
            value,
            replacement));
    }

    private static bool IsInTelemetryContext(SyntaxNode node) {
        var current = node.Parent;

        while (current is not null) {
            switch (current) {
                // Dictionary/collection indexers: tags["gen_ai.prompt.tokens"]
                case ElementAccessExpressionSyntax elementAccess:
                    if (IsLikelyTelemetryContainer(GetIdentifierName(elementAccess.Expression))) {
                        return true;
                    }

                    break;

                // Method invocations: SetTag("gen_ai.prompt.tokens", value)
                case InvocationExpressionSyntax invocation:
                    if (IsLikelyTelemetryMethod(GetMethodName(invocation))) {
                        return true;
                    }

                    break;

                // Assignment in initializers: { "gen_ai.prompt.tokens", value }
                case InitializerExpressionSyntax:
                    return true;

                // KeyValuePair or anonymous types
                case AnonymousObjectMemberDeclaratorSyntax:
                    return true;
            }

            current = current.Parent;
        }

        return false;
    }

    private static bool IsLikelyTelemetryContainer(string? identifier) {
        if (string.IsNullOrEmpty(identifier)) {
            return false;
        }

        var upper = identifier!.ToUpperInvariant();
        return upper.ContainsOrdinal("TAG") ||
               upper.ContainsOrdinal("ATTR") ||
               upper.ContainsOrdinal("PROPERTY") ||
               upper.ContainsOrdinal("METADATA") ||
               upper.ContainsOrdinal("SPAN") ||
               upper.ContainsOrdinal("ACTIVITY");
    }

    private static bool IsLikelyTelemetryMethod(string? methodName) {
        if (string.IsNullOrEmpty(methodName)) {
            return false;
        }

        // More precise matching to reduce false positives
        // Match exact method names or those with Tag/Attribute suffixes
        var name = methodName!;
        return name switch {
            // Exact matches for common telemetry methods
            "SetTag" or "AddTag" or "SetAttribute" or "AddAttribute" => true,
            "SetStatus" or "RecordException" => true,

            // Pattern matches for telemetry-related methods
            _ when name.EndsWithOrdinal("Tag") => true,
            _ when name.EndsWithOrdinal("Attribute") => true,
            _ when name.StartsWithOrdinal("SetTag") => true,
            _ when name.StartsWithOrdinal("AddTag") => true,
            _ when name.StartsWithOrdinal("Record") => true,

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
