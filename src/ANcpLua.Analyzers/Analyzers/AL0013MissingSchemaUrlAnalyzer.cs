using ANcpLua.Analyzers.Core;

namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0013: Detects OpenTelemetry configurations that don't set the schema URL.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AL0013MissingSchemaUrlAnalyzer : ALAnalyzer {
    private static readonly LocalizableResourceString Title = new(
        nameof(Resources.AL0013AnalyzerTitle), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableResourceString MessageFormat = new(
        nameof(Resources.AL0013AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableResourceString Description = new(
        nameof(Resources.AL0013AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.MissingTelemetrySchemaUrl,
        Title, MessageFormat, DiagnosticCategories.OpenTelemetry,
        DiagnosticSeverity.Info, isEnabledByDefault: true, Description,
        HelpLinkBase + "AL0013.md");

    private static readonly HashSet<string> ResourceConfigMethods = [
        "ConfigureResource",
        "SetResourceBuilder",
        "AddResource",
        "WithResource",
        "ConfigureOpenTelemetry"
    ];

    private static readonly string[] OtelBuilderTypeNames = [
        "OpenTelemetry.Trace.TracerProviderBuilder",
        "OpenTelemetry.Metrics.MeterProviderBuilder",
        "OpenTelemetry.Logs.LoggerProviderBuilder",
        "OpenTelemetry.OpenTelemetryBuilder",
        "OpenTelemetry.IOpenTelemetryBuilder"
    ];

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    protected override void RegisterActions(AnalysisContext context) {
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    private static void OnCompilationStart(CompilationStartAnalysisContext context)
    {
        var otelBuilderTypes = OtelBuilderTypeNames
            .Select(name => context.Compilation.GetTypeByMetadataName(name))
            .Where(type => type is not null)
            .Cast<INamedTypeSymbol>()
            .ToImmutableArray();

        if (otelBuilderTypes.IsEmpty)
            return;

        context.RegisterSyntaxNodeAction(
            ctx => AnalyzeInvocation(ctx, otelBuilderTypes),
            SyntaxKind.InvocationExpression);
    }

    private static void AnalyzeInvocation(
        SyntaxNodeAnalysisContext context,
        ImmutableArray<INamedTypeSymbol> otelBuilderTypes)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;

        var methodName = GetMethodName(invocation);
        if (methodName is null || !ResourceConfigMethods.Contains(methodName))
            return;

        if (!IsOtelBuilderCall(invocation, context.SemanticModel, otelBuilderTypes, context.CancellationToken))
            return;

        if (CheckForSchemaUrl(invocation))
            return;

        var location = GetMethodLocation(invocation);
        context.ReportDiagnostic(Rule, location);
    }

    private static bool IsOtelBuilderCall(
        InvocationExpressionSyntax invocation,
        SemanticModel semanticModel,
        ImmutableArray<INamedTypeSymbol> otelBuilderTypes,
        CancellationToken cancellationToken)
    {
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            return false;

        var receiverType = semanticModel.GetTypeInfo(memberAccess.Expression, cancellationToken).Type;
        if (receiverType is null)
            return false;

        // Walk the type hierarchy (self and base types)
        var currentType = receiverType;
        while (currentType is not null)
        {
            if (currentType is INamedTypeSymbol namedCurrent &&
                otelBuilderTypes.Any(t => SymbolEqualityComparer.Default.Equals(t, namedCurrent)))
                return true;

            currentType = currentType.BaseType;
        }

        // Check interfaces (handles IOpenTelemetryBuilder and DI patterns)
        if (receiverType is INamedTypeSymbol namedType)
        {
            foreach (var iface in namedType.AllInterfaces)
            {
                if (otelBuilderTypes.Any(t => SymbolEqualityComparer.Default.Equals(t, iface)))
                    return true;
            }
        }

        return false;
    }

    private static bool CheckForSchemaUrl(InvocationExpressionSyntax invocation) {
        foreach (var node in invocation.DescendantNodes()) {
            if (node is LiteralExpressionSyntax literal) {
                var value = literal.Token.ValueText;
                if (value.Contains("schema", StringComparison.OrdinalIgnoreCase) ||
                    value.Contains("telemetry.schema_url", StringComparison.OrdinalIgnoreCase) ||
                    value.Contains("opentelemetry.io/schemas", StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            if (node is InvocationExpressionSyntax nestedInvocation) {
                var nestedMethod = GetMethodName(nestedInvocation);
                if (nestedMethod?.Contains("Schema", StringComparison.OrdinalIgnoreCase) == true)
                    return true;
            }
        }

        return false;
    }

    private static Location GetMethodLocation(InvocationExpressionSyntax invocation) {
        return invocation.Expression switch {
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name.GetLocation(),
            IdentifierNameSyntax identifier => identifier.GetLocation(),
            _ => invocation.GetLocation()
        };
    }

    private static string? GetMethodName(InvocationExpressionSyntax invocation) {
        return invocation.Expression switch {
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.Text,
            IdentifierNameSyntax identifier => identifier.Identifier.Text,
            _ => null
        };
    }
}
