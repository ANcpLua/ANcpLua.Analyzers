using ANcpLua.Analyzers.Core;
using ANcpLua.Roslyn.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0085: Detects attribute values that violate OTel semantic convention specifications.
/// </summary>
/// <remarks>
///     <para>
///         Validates that known semantic convention attributes have correct value formats:
///         <list type="bullet">
///             <item>http.response.status_code - must be an integer (100-599)</item>
///             <item>gen_ai.system - must be a known provider (openai, anthropic, etc.)</item>
///             <item>error.type - should be an exception type or error code</item>
///         </list>
///     </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0085InvalidAttributeValueAnalyzer : AlAnalyzer {
    /// <summary>Known gen_ai.system provider values from semantic conventions.</summary>
    private static readonly HashSet<string> ValidGenAiSystems = new(StringComparer.OrdinalIgnoreCase) {
        "openai",
        "anthropic",
        "azure_ai_inference",
        "vertex_ai",
        "cohere",
        "aws_bedrock",
        "watsonx",
        "az.ai.inference"
    };

    /// <summary>Known gen_ai.operation.name values from semantic conventions.</summary>
    private static readonly HashSet<string> ValidGenAiOperations = new(StringComparer.OrdinalIgnoreCase) {
        "chat",
        "text_completion",
        "embeddings",
        "create_agent",
        "invoke_agent",
        "execute_tool"
    };

    /// <summary>Attribute validators by attribute name.</summary>
    private static readonly Dictionary<string, AttributeValidator> Validators = new(StringComparer.OrdinalIgnoreCase) {
        ["http.response.status_code"] = new AttributeValidator(
            ValidateHttpStatusCode,
            "integer between 100-599"),
        ["http.request.method"] = new AttributeValidator(
            ValidateHttpMethod,
            "valid HTTP method (GET, POST, PUT, DELETE, PATCH, HEAD, OPTIONS, TRACE, CONNECT, _OTHER)"),
        ["gen_ai.system"] = new AttributeValidator(
            ValidateGenAiSystem,
            "one of: openai, anthropic, azure_ai_inference, vertex_ai, cohere, aws_bedrock, watsonx, az.ai.inference"),
        ["gen_ai.operation.name"] = new AttributeValidator(
            ValidateGenAiOperation,
            "one of: chat, text_completion, embeddings, create_agent, invoke_agent, execute_tool"),
        ["gen_ai.request.max_tokens"] = new AttributeValidator(
            ValidatePositiveInteger,
            "positive integer"),
        ["gen_ai.request.temperature"] = new AttributeValidator(
            ValidateTemperature,
            "number between 0.0 and 2.0"),
        ["gen_ai.request.top_p"] = new AttributeValidator(
            ValidateProbability,
            "number between 0.0 and 1.0"),
        ["gen_ai.response.finish_reasons"] = new AttributeValidator(
            ValidateFinishReason,
            "one of: stop, length, content_filter, tool_calls, error"),
        ["gen_ai.usage.input_tokens"] = new AttributeValidator(
            ValidateNonNegativeInteger,
            "non-negative integer"),
        ["gen_ai.usage.output_tokens"] = new AttributeValidator(
            ValidateNonNegativeInteger,
            "non-negative integer"),
        ["rpc.grpc.status_code"] = new AttributeValidator(
            ValidateGrpcStatusCode,
            "integer between 0-16"),
        ["url.scheme"] = new AttributeValidator(
            ValidateUrlScheme,
            "one of: http, https, ftp, ws, wss")
    };

    private static readonly DiagnosticDescriptor Rule = CreateRule(
        DiagnosticIds.InvalidAttributeValue,
        DiagnosticCategories.OpenTelemetry,
        DiagnosticSeverities.RequiredFix);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    /// <summary>Registers operation actions to analyze SetTag and SetAttribute calls.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);

    private static void AnalyzeInvocation(OperationAnalysisContext context) {
        var invocation = (IInvocationOperation)context.Operation;

        // Look for SetTag or SetAttribute calls
        var methodName = invocation.TargetMethod.Name;
        if (methodName != "SetTag" && methodName != "SetAttribute" && methodName != "Add") {
            return;
        }

        // Need at least 2 arguments (key, value)
        if (invocation.Arguments.Length < 2) {
            return;
        }

        // Get the attribute name
        if (invocation.Arguments[0].Value.ConstantValue is not { HasValue: true, Value: string attributeName }) {
            return;
        }

        // Check if we have a validator for this attribute
        if (!Validators.TryGetValue(attributeName, out var validator)) {
            return;
        }

        // Get the value - could be a constant or we need to check the type
        var valueArg = invocation.Arguments[1].Value;

        // If we have a constant value, validate it
        if (valueArg.ConstantValue.HasValue) {
            var value = valueArg.ConstantValue.Value;
            var valueString = value?.ToString() ?? string.Empty;

            if (!validator.Validate(valueString)) {
                context.ReportDiagnostic(Diagnostic.Create(
                    Rule,
                    invocation.Arguments[1].Syntax.GetLocation(),
                    attributeName,
                    valueString,
                    validator.ExpectedFormat));
            }
        }
    }

    private static bool ValidateHttpStatusCode(string value) {
        if (!int.TryParse(value, out var code)) {
            return false;
        }

        return code >= 100 && code <= 599;
    }

    private static bool ValidateHttpMethod(string value) {
        return value.EqualsOrdinal("GET") ||
               value.EqualsOrdinal("POST") ||
               value.EqualsOrdinal("PUT") ||
               value.EqualsOrdinal("DELETE") ||
               value.EqualsOrdinal("PATCH") ||
               value.EqualsOrdinal("HEAD") ||
               value.EqualsOrdinal("OPTIONS") ||
               value.EqualsOrdinal("TRACE") ||
               value.EqualsOrdinal("CONNECT") ||
               value.EqualsOrdinal("_OTHER");
    }

    private static bool ValidateGenAiSystem(string value) =>
        ValidGenAiSystems.Contains(value);

    private static bool ValidateGenAiOperation(string value) =>
        ValidGenAiOperations.Contains(value);

    private static bool ValidatePositiveInteger(string value) {
        if (!int.TryParse(value, out var num)) {
            return false;
        }

        return num > 0;
    }

    private static bool ValidateNonNegativeInteger(string value) {
        if (!int.TryParse(value, out var num)) {
            return false;
        }

        return num >= 0;
    }

    private static bool ValidateTemperature(string value) {
        if (!double.TryParse(value, out var temp)) {
            return false;
        }

        return temp >= 0.0 && temp <= 2.0;
    }

    private static bool ValidateProbability(string value) {
        if (!double.TryParse(value, out var prob)) {
            return false;
        }

        return prob >= 0.0 && prob <= 1.0;
    }

    private static bool ValidateFinishReason(string value) {
        return value.EqualsIgnoreCase("stop") ||
               value.EqualsIgnoreCase("length") ||
               value.EqualsIgnoreCase("content_filter") ||
               value.EqualsIgnoreCase("tool_calls") ||
               value.EqualsIgnoreCase("error");
    }

    private static bool ValidateGrpcStatusCode(string value) {
        if (!int.TryParse(value, out var code)) {
            return false;
        }

        return code >= 0 && code <= 16;
    }

    private static bool ValidateUrlScheme(string value) {
        return value.EqualsIgnoreCase("http") ||
               value.EqualsIgnoreCase("https") ||
               value.EqualsIgnoreCase("ftp") ||
               value.EqualsIgnoreCase("ws") ||
               value.EqualsIgnoreCase("wss");
    }

    /// <summary>Encapsulates validation logic and expected format message.</summary>
    private sealed partial class AttributeValidator(Func<string, bool> validate, string expectedFormat) {
        public string ExpectedFormat { get; } = expectedFormat;

        public bool Validate(string value) => validate(value);
    }
}
