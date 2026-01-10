using ANcpLua.Analyzers.Core;

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
public sealed class AL0012DeprecatedAttributeAnalyzer : ALAnalyzer {
    private static readonly LocalizableResourceString Title = new(
        nameof(Resources.AL0012AnalyzerTitle), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableResourceString MessageFormat = new(
        nameof(Resources.AL0012AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableResourceString Description = new(
        nameof(Resources.AL0012AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.DeprecatedSemanticConventionAttribute,
        Title, MessageFormat, DiagnosticCategories.OpenTelemetry,
        DiagnosticSeverity.Warning, true, Description,
        HelpLinkBase);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterSyntaxNodeAction(AnalyzeStringLiteral, SyntaxKind.StringLiteralExpression);

    private static void AnalyzeStringLiteral(SyntaxNodeAnalysisContext context) {
        // Skip analysis of the analyzer's own assembly (contains mapping dictionary of deprecated names)
        var assemblyName = context.SemanticModel.Compilation.AssemblyName;
        if (assemblyName is not null && assemblyName.StartsWith("ANcpLua.Analyzers", StringComparison.Ordinal)) {
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
        DeprecatedOtelAttributes.AttributeKeyPatterns.Any(p =>
            methodName.Contains(p, StringComparison.OrdinalIgnoreCase));

    private static bool IsTelemetryInitializer(SyntaxNode node) =>
        node is InitializerExpressionSyntax { Parent: ObjectCreationExpressionSyntax creation } &&
        IsTelemetryTypeName(creation.Type.ToString());

    private static bool IsTelemetryTypeName(string typeName) =>
        typeName.Contains("Tag", StringComparison.Ordinal) ||
        typeName.Contains("Attribute", StringComparison.Ordinal) ||
        typeName.Contains("KeyValuePair", StringComparison.Ordinal);

    private static bool IsLikelyTelemetryContainer(string identifier) {
        var upperIdentifier = identifier.ToUpperInvariant();
        return upperIdentifier.Contains("ATTRIBUTE", StringComparison.Ordinal) ||
               upperIdentifier.Contains("TAG", StringComparison.Ordinal) ||
               upperIdentifier.Contains("ATTR", StringComparison.Ordinal) ||
               upperIdentifier == "ATTRS";
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
