using ANcpLua.Analyzers.Core;

namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0013: Detects OpenTelemetry configurations that don't set the schema URL.
/// </summary>
/// <remarks>
///     <para>
///         The OpenTelemetry specification recommends setting a schema URL on resources
///         to indicate which version of the semantic conventions is being used. Without
///         a schema URL, telemetry backends cannot automatically transform attributes
///         between convention versions, potentially causing data inconsistencies.
///     </para>
///     <para>
///         The analyzer flags resource configuration calls like <c>ConfigureResource</c>,
///         <c>SetResourceBuilder</c>, and <c>AddResource</c> on OpenTelemetry builder
///         types when they do not include a schema URL reference.
///     </para>
///     <para>
///         Schema URL detection is heuristic: the analyzer looks for string literals
///         containing "schema", "telemetry.schema_url", or "opentelemetry.io/schemas",
///         as well as method calls with "Schema" in the name. This may not catch all
///         indirect configurations but covers common patterns.
///     </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0013MissingSchemaUrlAnalyzer : AlAnalyzer {
    private static readonly LocalizableResourceString Title = new(
        nameof(Resources.AL0013AnalyzerTitle), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableResourceString MessageFormat = new(
        nameof(Resources.AL0013AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableResourceString Description = new(
        nameof(Resources.AL0013AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.MissingTelemetrySchemaUrl,
        Title, MessageFormat, DiagnosticCategories.OpenTelemetry,
        DiagnosticSeverity.Info, true, Description,
        HelpLinkBase);

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

    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterCompilationStartAction(OnCompilationStart);

    private static void OnCompilationStart(CompilationStartAnalysisContext context) {
        var otelBuilderTypes = OtelBuilderTypeNames
            .Select(context.Compilation.GetTypeByMetadataName)
            .WhereNotNull()
            .ToImmutableArray();

        if (otelBuilderTypes.IsEmpty) {
            return;
        }

        context.RegisterSyntaxNodeAction(
            ctx => AnalyzeInvocation(ctx, otelBuilderTypes),
            SyntaxKind.InvocationExpression);
    }

    private static void AnalyzeInvocation(
        SyntaxNodeAnalysisContext context,
        ImmutableArray<INamedTypeSymbol> otelBuilderTypes) {
        var invocation = (InvocationExpressionSyntax)context.Node;

        var methodName = GetMethodName(invocation);
        if (methodName is null || !ResourceConfigMethods.Contains(methodName)) {
            return;
        }

        if (!IsOtelBuilderCall(invocation, context.SemanticModel, otelBuilderTypes, context.CancellationToken)) {
            return;
        }

        if (CheckForSchemaUrl(invocation)) {
            return;
        }

        var location = GetMethodLocation(invocation);
        context.ReportDiagnostic(Rule, location);
    }

    private static bool IsOtelBuilderCall(
        InvocationExpressionSyntax invocation,
        SemanticModel semanticModel,
        ImmutableArray<INamedTypeSymbol> otelBuilderTypes,
        CancellationToken cancellationToken) {
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess) {
            return false;
        }

        if (semanticModel.GetTypeInfo(memberAccess.Expression, cancellationToken).Type is not { } receiverType) {
            return false;
        }

        // Check if receiver type inherits from or implements any OTel builder type
        return otelBuilderTypes.Any(builderType =>
            receiverType.InheritsFrom(builderType) || receiverType.Implements(builderType));
    }

    private static bool CheckForSchemaUrl(SyntaxNode invocation) {
        foreach (var node in invocation.DescendantNodes()) {
            switch (node) {
                case LiteralExpressionSyntax literal: {
                    var value = literal.Token.ValueText;
                    if (value.Contains("schema", StringComparison.OrdinalIgnoreCase) ||
                        value.Contains("telemetry.schema_url", StringComparison.OrdinalIgnoreCase) ||
                        value.Contains("opentelemetry.io/schemas", StringComparison.OrdinalIgnoreCase)) {
                        return true;
                    }

                    break;
                }
                case InvocationExpressionSyntax nestedInvocation: {
                    var nestedMethod = GetMethodName(nestedInvocation);
                    if (nestedMethod?.Contains("Schema", StringComparison.OrdinalIgnoreCase) == true) {
                        return true;
                    }

                    break;
                }
            }
        }

        return false;
    }

    private static Location GetMethodLocation(InvocationExpressionSyntax invocation) =>
        invocation.Expression switch {
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name.GetLocation(),
            IdentifierNameSyntax identifier => identifier.GetLocation(),
            _ => invocation.GetLocation()
        };

    private static string? GetMethodName(InvocationExpressionSyntax invocation) =>
        invocation.Expression switch {
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.Text,
            IdentifierNameSyntax identifier => identifier.Identifier.Text,
            _ => null
        };
}
