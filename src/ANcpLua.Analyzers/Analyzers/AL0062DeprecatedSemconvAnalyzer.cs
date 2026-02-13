using ANcpLua.Analyzers.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0062: Detects usage of deprecated OpenTelemetry semantic convention attributes.
/// </summary>
/// <remarks>
///     <para>
///         Some semantic convention attribute names have been deprecated and replaced
///         with newer names. This analyzer helps migrate to the current conventions.
///     </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0062DeprecatedSemconvAnalyzer : AlAnalyzer {
    // Map of deprecated attribute names to their replacements and deprecation version
    private static readonly Dictionary<string, (string Replacement, string Version)> DeprecatedAttributes =
        new(StringComparer.OrdinalIgnoreCase) {
            // HTTP semantic conventions (1.21.0 -> 1.23.0)
            ["http.method"] = ("http.request.method", "1.21.0"),
            ["http.url"] = ("url.full", "1.21.0"),
            ["http.target"] = ("url.path and url.query", "1.21.0"),
            ["http.host"] = ("server.address and server.port", "1.21.0"),
            ["http.scheme"] = ("url.scheme", "1.21.0"),
            ["http.status_code"] = ("http.response.status_code", "1.21.0"),
            ["http.request_content_length"] = ("http.request.body.size", "1.21.0"),
            ["http.response_content_length"] = ("http.response.body.size", "1.21.0"),
            ["http.flavor"] = ("network.protocol.version", "1.21.0"),
            ["http.user_agent"] = ("user_agent.original", "1.21.0"),
            ["http.client_ip"] = ("client.address", "1.21.0"),
            // Net semantic conventions
            ["net.peer.name"] = ("server.address", "1.21.0"),
            ["net.peer.port"] = ("server.port", "1.21.0"),
            ["net.host.name"] = ("server.address", "1.21.0"),
            ["net.host.port"] = ("server.port", "1.21.0"),
            ["net.transport"] = ("network.transport", "1.21.0"),
            // Database semantic conventions
            ["db.statement"] = ("db.query.text", "1.24.0"),
            ["db.operation"] = ("db.operation.name", "1.24.0")
        };

    private static readonly DiagnosticDescriptor Rule = CreateRule(
        DiagnosticIds.DeprecatedSemconv,
        DiagnosticCategories.OpenTelemetry,
        DiagnosticSeverities.Suggestion);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    /// <summary>Registers operation actions to analyze SetTag calls.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);

    private static void AnalyzeInvocation(OperationAnalysisContext context) {
        var invocation = (IInvocationOperation)context.Operation;

        // Look for SetTag, AddTag, or similar methods
        if (!IsTagMethod(invocation.TargetMethod.Name) || invocation.Arguments.Length is 0) {
            return;
        }

        // Check if the first argument is a deprecated attribute name
        if (invocation.Arguments[0].Value.ConstantValue is not { HasValue: true, Value: string attributeName }) {
            return;
        }

        if (DeprecatedAttributes.TryGetValue(attributeName, out var info)) {
            context.ReportDiagnostic(Diagnostic.Create(
                Rule,
                invocation.Arguments[0].Syntax.GetLocation(),
                attributeName,
                info.Version,
                info.Replacement));
        }
    }

    private static bool IsTagMethod(string methodName) =>
        methodName is "SetTag" or "AddTag" or "SetAttribute" or "Add";
}
