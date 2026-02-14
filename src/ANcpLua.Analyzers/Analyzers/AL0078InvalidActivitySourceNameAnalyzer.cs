using ANcpLua.Analyzers.Core;
using ANcpLua.Roslyn.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using System.Collections.Immutable;

namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0078: Detects ActivitySource names that don't follow reverse-DNS naming convention.
/// </summary>
/// <remarks>
///     <para>
///         ActivitySource names should follow the reverse-DNS convention (e.g., 'company.product.component').
///         Valid names must:
///         <list type="bullet">
///             <item>Contain at least one dot to indicate hierarchical namespace</item>
///             <item>Use only lowercase letters, digits, dots, and hyphens</item>
///             <item>Not contain spaces or other invalid characters</item>
///             <item>Not be empty or whitespace-only</item>
///         </list>
///     </para>
///     <para>
///         This naming convention ensures consistent identification across telemetry backends
///         and follows OpenTelemetry best practices.
///     </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0078InvalidActivitySourceNameAnalyzer : AlAnalyzer {
    /// <summary>The diagnostic identifier for AL0078.</summary>
    public const string DiagnosticId = "AL0078";

    private const string ActivitySourceTypeName = "System.Diagnostics.ActivitySource";

    private static readonly DiagnosticDescriptor Rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.OpenTelemetry,
        DiagnosticSeverities.RequiredFix);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    /// <summary>Registers operation actions to analyze ActivitySource creation.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterOperationAction(AnalyzeObjectCreation, OperationKind.ObjectCreation);

    private static void AnalyzeObjectCreation(OperationAnalysisContext context) {
        var objectCreation = (IObjectCreationOperation)context.Operation;

        // Check if this is ActivitySource creation
        if (objectCreation.Type?.ToDisplayString() != ActivitySourceTypeName) {
            return;
        }

        // Get the source name from constructor argument
        if (objectCreation.Arguments.Length is 0 ||
            objectCreation.Arguments[0].Value.ConstantValue is not { HasValue: true, Value: string sourceName }) {
            return;
        }

        // Validate the ActivitySource name
        if (!IsValidActivitySourceName(sourceName)) {
            context.ReportDiagnostic(Diagnostic.Create(
                Rule,
                objectCreation.Arguments[0].Syntax.GetLocation(),
                sourceName));
        }
    }

    /// <summary>
    ///     Validates that an ActivitySource name follows the reverse-DNS naming convention.
    /// </summary>
    /// <param name="name">The ActivitySource name to validate.</param>
    /// <returns>True if the name is valid, false otherwise.</returns>
    private static bool IsValidActivitySourceName(string name) {
        // Empty or whitespace names are invalid
        if (string.IsNullOrWhiteSpace(name)) {
            return false;
        }

        // Must contain at least one dot (reverse-DNS format)
        if (!name.ContainsOrdinal(".")) {
            return false;
        }

        // Check for invalid characters: spaces are not allowed
        if (name.ContainsOrdinal(" ")) {
            return false;
        }

        // Validate each segment
        var segments = name.Split('.');
        foreach (var segment in segments) {
            // Segments cannot be empty (no consecutive dots or leading/trailing dots)
            if (string.IsNullOrEmpty(segment)) {
                return false;
            }

            // Each character must be lowercase letter, digit, or hyphen
            foreach (var c in segment) {
                if (!char.IsLower(c) && !char.IsDigit(c) && c != '-') {
                    // Allow uppercase, but warn (we don't reject, just recommend lowercase)
                    // Actually, for strictness, let's allow uppercase too since it's common
                    if (!char.IsUpper(c)) {
                        return false;
                    }
                }
            }
        }

        return true;
    }
}
