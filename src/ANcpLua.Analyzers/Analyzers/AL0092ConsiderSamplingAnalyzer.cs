
namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0092: Detects OpenTelemetry tracing configurations without sampling configured.
/// </summary>
/// <remarks>
///     <para>
///         High-volume services can generate excessive telemetry data without proper
///         sampling configuration. This analyzer flags tracing configurations that:
///     </para>
///     <list type="bullet">
///         <item>Do not call SetSampler at all (uses default AlwaysOnSampler)</item>
///         <item>Explicitly use AlwaysOnSampler (captures every span)</item>
///     </list>
///     <para>
///         For production environments, consider using:
///     </para>
///     <list type="bullet">
///         <item>ParentBasedSampler - Respects parent sampling decisions</item>
///         <item>TraceIdRatioBasedSampler - Probabilistic sampling</item>
///         <item>Tail-based sampling at the collector level</item>
///     </list>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0092ConsiderSamplingAnalyzer : AlAnalyzer {
    /// <summary>The diagnostic identifier for AL0092.</summary>
    private const string DiagnosticId = "AL0092";

    /// <summary>Array of known OTel tracer builder type names.</summary>
    private static readonly string[] s_tracerBuilderTypeNames = [
        "OpenTelemetry.Trace.TracerProviderBuilder",
        "OpenTelemetry.IOpenTelemetryBuilder"
    ];

    /// <summary>Methods that configure tracing and should have sampling configured.</summary>
    private static readonly HashSet<string> s_tracingConfigMethods = [
        "WithTracing",
        "AddTracing",
        "ConfigureTracing"
    ];

    /// <summary>Sampler types that capture all spans (problematic for high-volume services).</summary>
    private static readonly HashSet<string> s_alwaysOnSamplerTypes = [
        "AlwaysOnSampler",
        "OpenTelemetry.Trace.AlwaysOnSampler"
    ];

    private static readonly DiagnosticDescriptor s_rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.OpenTelemetry,
        DiagnosticSeverities.HiddenByDefault);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_rule];

    /// <summary>Registers compilation start action to analyze tracing configurations.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterCompilationStartAction(OnCompilationStart);

    private static void OnCompilationStart(CompilationStartAnalysisContext context) {
        var tracerBuilderTypes = s_tracerBuilderTypeNames
            .Select(context.Compilation.GetTypeByMetadataName)
            .WhereNotNull()
            .ToImmutableArray();

        if (tracerBuilderTypes.IsEmpty) {
            return;
        }

        context.RegisterSyntaxNodeAction(
            ctx => AnalyzeInvocation(ctx, tracerBuilderTypes),
            SyntaxKind.InvocationExpression);
    }

    private static void AnalyzeInvocation(
        SyntaxNodeAnalysisContext context,
        ImmutableArray<INamedTypeSymbol> tracerBuilderTypes) {
        var invocation = (InvocationExpressionSyntax)context.Node;

        var methodName = GetMethodName(invocation);
        if (methodName is null || !s_tracingConfigMethods.Contains(methodName)) {
            return;
        }

        if (!IsTracerBuilderCall(invocation, context.SemanticModel, tracerBuilderTypes, context.CancellationToken)) {
            return;
        }

        if (HasSamplerConfiguration(invocation, out var usesAlwaysOn)) {
            if (usesAlwaysOn) {
                context.ReportDiagnostic(Diagnostic.Create(s_rule, GetMethodLocation(invocation)));
            }

            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(s_rule, GetMethodLocation(invocation)));
    }

    private static bool IsTracerBuilderCall(
        InvocationExpressionSyntax invocation,
        SemanticModel semanticModel,
        ImmutableArray<INamedTypeSymbol> tracerBuilderTypes,
        CancellationToken cancellationToken) {
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess
            || ModelExtensions.GetTypeInfo(semanticModel, memberAccess.Expression, cancellationToken).Type is not { } receiverType) {
            return false;
        }

        return tracerBuilderTypes.Any(builderType =>
            receiverType.InheritsFrom(builderType) || receiverType.Implements(builderType));
    }

    private static bool HasSamplerConfiguration(SyntaxNode invocation, out bool usesAlwaysOnSampler) {
        usesAlwaysOnSampler = false;

        foreach (var node in invocation.DescendantNodes()) {
            switch (node) {
                case InvocationExpressionSyntax nestedInvocation: {
                    var nestedMethod = GetMethodName(nestedInvocation);
                    if (nestedMethod?.EqualsOrdinal("SetSampler") == true) {
                        usesAlwaysOnSampler = CheckForAlwaysOnSampler(nestedInvocation);
                        return true;
                    }

                    break;
                }
                case IdentifierNameSyntax identifier: {
                    var name = identifier.Identifier.Text;
                    if (s_alwaysOnSamplerTypes.Any(s => s.ContainsIgnoreCase(name))) {
                        usesAlwaysOnSampler = true;
                    }

                    break;
                }
            }
        }

        return false;
    }

    private static bool CheckForAlwaysOnSampler(InvocationExpressionSyntax setSamplerInvocation) {
        foreach (var descendant in setSamplerInvocation.DescendantNodes()) {
            switch (descendant) {
                case IdentifierNameSyntax identifier
                    when s_alwaysOnSamplerTypes.Contains(identifier.Identifier.Text):
                    return true;
                case ObjectCreationExpressionSyntax { Type: IdentifierNameSyntax typeName } when s_alwaysOnSamplerTypes.Contains(typeName.Identifier.Text):
                    return true;
                case ObjectCreationExpressionSyntax { Type: QualifiedNameSyntax qualifiedName } when s_alwaysOnSamplerTypes.Contains(qualifiedName.ToString()):
                    return true;
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
