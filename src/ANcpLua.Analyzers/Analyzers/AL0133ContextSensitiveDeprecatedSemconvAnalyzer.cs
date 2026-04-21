namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0133: Detects deprecated semantic convention attributes that require context-sensitive migration.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0133ContextSensitiveDeprecatedSemconvAnalyzer : AlAnalyzer {
    /// <summary>The diagnostic identifier for AL0133.</summary>
    private const string DiagnosticId = "AL0133";

    private static readonly DiagnosticDescriptor Rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.OpenTelemetry,
        DiagnosticSeverity.Warning);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    /// <summary>Registers syntax node actions to analyze string literals in telemetry contexts.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterSyntaxNodeAction(AnalyzeStringLiteral, SyntaxKind.StringLiteralExpression);

    private static void AnalyzeStringLiteral(SyntaxNodeAnalysisContext context) {
        var assemblyName = context.SemanticModel.Compilation.AssemblyName;
        if (assemblyName is not null && assemblyName.StartsWithOrdinal("ANcpLua.Analyzers")) {
            return;
        }

        var literal = (LiteralExpressionSyntax)context.Node;
        var value = literal.Token.ValueText;

        if (string.IsNullOrEmpty(value)
            || !TryGetDeprecatedGuidance(literal, value, out var guidance)
            || guidance is null) {
            return;
        }

        context.ReportDiagnostic(Rule, literal.GetLocation(), value, guidance);
    }

    private static bool TryGetDeprecatedGuidance(LiteralExpressionSyntax literal, string value, out string? guidance) {
        guidance = null;

        if (!OpenTelemetryDeprecatedSemconvCatalog.TryGetContextSensitiveDeprecatedName(value, out var resolved)) {
            return false;
        }

        if (!IsInTelemetryContext(literal) && !IsInTelemetryEventContext(literal)) {
            return false;
        }

        guidance = resolved;
        return true;
    }

    private static bool IsInTelemetryContext(SyntaxNode node) {
        var current = node.Parent;

        while (current is not null) {
            if (IsTelemetryElementAccess(current) ||
                IsTelemetryInvocation(current) ||
                IsTelemetryInitializer(current) ||
                IsAssignmentInTelemetryInitializer(current)) {
                return true;
            }

            current = current.Parent;
        }

        return false;
    }

    // Dictionary-initializer key assignments (e.g. `["gen_ai.prompt"] = value`) count as a telemetry
    // context only when the CONTAINING object-creation is itself a telemetry type — otherwise plain
    // migration maps like `new Dictionary<string,string> { ["gen_ai.prompt"] = "..." }` produce
    // false positives. The previous check accepted any dictionary initializer unconditionally.
    private static bool IsAssignmentInTelemetryInitializer(SyntaxNode node) =>
        node is AssignmentExpressionSyntax {
            Parent: InitializerExpressionSyntax { Parent: ObjectCreationExpressionSyntax creation }
        } && IsTelemetryTypeName(creation.Type.ToString());


    private static bool IsInTelemetryEventContext(SyntaxNode node) {
        for (var current = node.Parent; current is not null; current = current.Parent) {
            if (current is ObjectCreationExpressionSyntax creation &&
                creation.Type.ToString().ContainsOrdinal("ActivityEvent")) {
                return true;
            }

            if (current is InvocationExpressionSyntax invocation &&
                GetMethodName(invocation) is { } methodName &&
                methodName.EqualsOrdinal("AddEvent")) {
                return true;
            }
        }

        return false;
    }

    private static bool IsTelemetryElementAccess(SyntaxNode node) =>
        node is ElementAccessExpressionSyntax elementAccess &&
        GetIdentifierName(elementAccess.Expression) is { } identifier &&
        IsLikelyTelemetryContainer(identifier);

    private static bool IsTelemetryInvocation(SyntaxNode node) {
        if (node is not InvocationExpressionSyntax invocation || GetMethodName(invocation) is not { } methodName) {
            return false;
        }

        if (methodName.ContainsIgnoreCase("ATTRIBUTE")
            || methodName.ContainsIgnoreCase("TAG")) {
            return true;
        }

        return invocation.Expression is MemberAccessExpressionSyntax memberAccess
               && memberAccess.Name.Identifier.Text == "Add"
               && GetIdentifierName(memberAccess.Expression) is { } identifier
               && IsLikelyTelemetryContainer(identifier);
    }

    private static bool IsTelemetryInitializer(SyntaxNode node) =>
        node is InitializerExpressionSyntax { Parent: ObjectCreationExpressionSyntax creation } &&
        IsTelemetryTypeName(creation.Type.ToString());

    private static bool IsTelemetryTypeName(string typeName) =>
        typeName.ContainsOrdinal("Tag")
        || typeName.ContainsOrdinal("Attribute")
        || typeName.ContainsOrdinal("KeyValuePair");

    private static bool IsLikelyTelemetryContainer(string identifier) =>
        identifier.ContainsIgnoreCase("ATTRIBUTE")
        || identifier.ContainsIgnoreCase("TAG")
        || identifier.ContainsIgnoreCase("ATTR")
        || identifier.EqualsIgnoreCase("ATTRS");

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
