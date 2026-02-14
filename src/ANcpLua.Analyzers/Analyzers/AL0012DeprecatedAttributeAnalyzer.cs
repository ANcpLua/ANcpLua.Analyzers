using ANcpLua.Analyzers.Core;
using ANcpLua.Roslyn.Utilities;
using ANcpLua.Roslyn.Utilities.Contexts;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0012: Detects usage of deprecated OpenTelemetry semantic convention attributes.
/// </summary>
/// <remarks>
///     <para>
///         OpenTelemetry semantic conventions evolve over time, with attribute names
///         being renamed or deprecated across specification versions. Using deprecated
///         attributes reduces interoperability with modern telemetry backends and tools
///         that expect current conventions.
///     </para>
///     <para>
///         The analyzer identifies string literals containing deprecated attribute names
///         when used in telemetry contexts such as tag assignments, attribute dictionaries,
///         or telemetry method calls. It provides the replacement attribute name and the
///         specification version where the change occurred.
///     </para>
///     <para>
///         Context detection uses heuristics: the analyzer looks for patterns like
///         dictionary indexers on variables named "tags" or "attributes", invocations
///         of methods containing "Attribute" or "Tag", and object initializers for
///         telemetry-related types. This reduces false positives on unrelated string
///         literals that happen to match deprecated attribute names.
///     </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0012DeprecatedAttributeAnalyzer : AlAnalyzer {
    /// <summary>The diagnostic identifier for AL0012.</summary>
    public const string DiagnosticId = "AL0012";

    private static readonly DiagnosticDescriptor Rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.OpenTelemetry,
        DiagnosticSeverity.Warning);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    /// <summary>Registers syntax node actions to analyze string literals for deprecated OTel attributes.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterSyntaxNodeAction(AnalyzeStringLiteral, SyntaxKind.StringLiteralExpression);

    private static void AnalyzeStringLiteral(SyntaxNodeAnalysisContext context) {
        // Skip analysis of the analyzer's own assembly (contains mapping dictionary of deprecated names)
        var assemblyName = context.SemanticModel.Compilation.AssemblyName;
        if (assemblyName is not null && assemblyName.StartsWithOrdinal("ANcpLua.Analyzers")) {
            return;
        }

        var literal = (LiteralExpressionSyntax)context.Node;
        var value = literal.Token.ValueText;

        if (string.IsNullOrEmpty(value)) {
            return;
        }

        if (!DeprecatedOtelAttributes.Renames.TryGetValue(value, out var replacement)) {
            return;
        }

        if (!IsInTelemetryContext(literal)) {
            return;
        }

        context.ReportDiagnostic(Rule, literal.GetLocation(), value, replacement.Version, replacement.Replacement);
    }

    private static bool IsInTelemetryContext(SyntaxNode node) {
        var current = node.Parent;

        while (current is not null) {
            if (IsTelemetryElementAccess(current) ||
                IsTelemetryInvocation(current) ||
                IsTelemetryInitializer(current) ||
                current is AssignmentExpressionSyntax { Parent: InitializerExpressionSyntax }) {
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
        node is InvocationExpressionSyntax invocation &&
        GetMethodName(invocation) is { } methodName &&
        DeprecatedOtelAttributes.AttributeKeyPatterns.Any(methodName.ContainsIgnoreCase);

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
        identifier.EqualsIgnoreCase("ATTRS");

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
