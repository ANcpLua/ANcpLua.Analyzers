
namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0090: Detects OTLP exporter configurations using HTTP protocol without compression.
/// </summary>
/// <remarks>
///     <para>
///         OTLP supports two transport protocols: gRPC and HTTP/protobuf. While gRPC
///         automatically handles compression via its underlying HTTP/2 transport,
///         HTTP/protobuf exports use uncompressed payloads by default.
///     </para>
///     <para>
///         For services emitting large telemetry payloads (especially those using
///         gen_ai.content attributes with full request/response text), enabling
///         gzip compression can reduce bandwidth usage by 70-90% and significantly
///         decrease export latency.
///     </para>
///     <para>
///         The analyzer identifies OTLP exporter configurations that:
///         1. Explicitly set Protocol to HttpProtobuf without enabling compression
///         2. Use AddOtlpExporter/UseOtlpExporter without compression configuration
///     </para>
///     <para>
///         gRPC protocol (OtlpExportProtocol.Grpc) is not flagged as it handles
///         compression automatically.
///     </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0090UncompressedExportAnalyzer : AlAnalyzer {
    /// <summary>The diagnostic identifier for AL0090.</summary>
    private const string DiagnosticId = "AL0090";

    private static readonly DiagnosticDescriptor s_rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.OpenTelemetry,
        DiagnosticSeverity.Warning);

    /// <summary>Method names that configure OTLP exporters.</summary>
    private static readonly HashSet<string> s_otlpExporterMethods = [
        "AddOtlpExporter",
        "UseOtlpExporter",
        "WithOtlpExporter"
    ];

    /// <summary>Type names for OTLP exporter options.</summary>
    private static readonly string[] s_otlpOptionsTypeNames = [
        "OpenTelemetry.Exporter.OtlpExporterOptions",
        "OpenTelemetry.Exporter.OtlpExporterOptionsBase"
    ];

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_rule];

    /// <summary>Registers compilation start action to analyze OTLP exporter configurations.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterCompilationStartAction(OnCompilationStart);

    private static void OnCompilationStart(CompilationStartAnalysisContext context) {
        var otlpOptionsTypes = s_otlpOptionsTypeNames
            .Select(context.Compilation.GetTypeByMetadataName)
            .WhereNotNull()
            .ToImmutableArray();

        if (otlpOptionsTypes.IsEmpty) {
            return;
        }

        var httpProtobufType = context.Compilation.GetTypeByMetadataName(
            "OpenTelemetry.Exporter.OtlpExportProtocol");

        context.RegisterSyntaxNodeAction(
            ctx => AnalyzeInvocation(ctx, otlpOptionsTypes, httpProtobufType),
            SyntaxKind.InvocationExpression);
    }

    private static void AnalyzeInvocation(
        SyntaxNodeAnalysisContext context,
        ImmutableArray<INamedTypeSymbol> otlpOptionsTypes,
        INamedTypeSymbol? httpProtobufType) {
        var invocation = (InvocationExpressionSyntax)context.Node;

        if (GetMethodName(invocation) is not { } methodName || !s_otlpExporterMethods.Contains(methodName)) {
            return;
        }

        var lambdaArg = invocation.ArgumentList.Arguments
            .Select(a => a.Expression)
            .OfType<SimpleLambdaExpressionSyntax>()
            .FirstOrDefault();

        if (lambdaArg is not null) {
            if (!HasCompressionConfiguration(lambdaArg)
                && HasHttpProtobufConfiguration(lambdaArg, httpProtobufType, context.SemanticModel, context.CancellationToken)) {
                context.ReportDiagnostic(s_rule, GetMethodLocation(invocation));
            }

            return;
        }

        // Delegate arguments (AddOtlpExporter(ConfigureOtlp)) cannot be traced statically
        if (invocation.ArgumentList.Arguments.Any(a => a.Expression is IdentifierNameSyntax)) {
            return;
        }

        foreach (var arg in invocation.ArgumentList.Arguments) {
            if (ModelExtensions.GetTypeInfo(context.SemanticModel, arg.Expression, context.CancellationToken).Type is not { } argType) {
                continue;
            }

            if (otlpOptionsTypes.Any(optionsType => argType.InheritsFrom(optionsType) || argType.IsEqualTo(optionsType))
                && IsHttpProtobufOptionsWithoutCompression(arg.Expression)) {
                context.ReportDiagnostic(s_rule, GetMethodLocation(invocation));
            }
        }
    }

    private static bool HasCompressionConfiguration(SimpleLambdaExpressionSyntax lambda) {
        foreach (var node in lambda.DescendantNodes()) {
            var name = node switch {
                AssignmentExpressionSyntax assignment => GetMemberName(assignment.Left),
                InvocationExpressionSyntax invocation => GetMethodName(invocation),
                _ => null
            };

            if (name is not null && (name.ContainsIgnoreCase("compression") || name.ContainsIgnoreCase("gzip"))) {
                return true;
            }
        }

        return false;
    }

    private static bool HasHttpProtobufConfiguration(
        SimpleLambdaExpressionSyntax lambda,
        INamedTypeSymbol? httpProtobufType,
        SemanticModel semanticModel,
        CancellationToken cancellationToken) {
        foreach (var node in lambda.DescendantNodes()) {
            if (node is not AssignmentExpressionSyntax { Right: MemberAccessExpressionSyntax memberAccess } assignment
                || GetMemberName(assignment.Left) is not "Protocol") {
                continue;
            }

            if (memberAccess.Name.Identifier.Text.EqualsOrdinal("HttpProtobuf")) {
                return true;
            }

            // Fall back to semantic model for aliased/renamed references
            if (httpProtobufType is not null
                && ModelExtensions.GetSymbolInfo(semanticModel, memberAccess, cancellationToken).Symbol
                    is IFieldSymbol { Name: "HttpProtobuf" } fieldSymbol
                && fieldSymbol.ContainingType.IsEqualTo(httpProtobufType)) {
                return true;
            }
        }

        return false;
    }

    private static bool IsHttpProtobufOptionsWithoutCompression(
        ExpressionSyntax expression) {
        if (expression is not ObjectCreationExpressionSyntax { Initializer: { } initializer }) {
            return false;
        }

        var hasHttpProtobuf = false;
        var hasCompression = false;

        foreach (var expr in initializer.Expressions) {
            if (expr is not AssignmentExpressionSyntax assignment
                || GetMemberName(assignment.Left) is not { } leftText) {
                continue;
            }

            if (leftText.EqualsOrdinal("Protocol")
                && assignment.Right is MemberAccessExpressionSyntax { Name.Identifier.Text: "HttpProtobuf" }) {
                hasHttpProtobuf = true;
            }

            if (leftText.ContainsIgnoreCase("compression") || leftText.ContainsIgnoreCase("gzip")) {
                hasCompression = true;
            }
        }

        return hasHttpProtobuf && !hasCompression;
    }

    private static string? GetMemberName(ExpressionSyntax expression) =>
        expression switch {
            IdentifierNameSyntax identifier => identifier.Identifier.Text,
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.Text,
            _ => null
        };

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
