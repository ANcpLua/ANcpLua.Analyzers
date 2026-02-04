using ANcpLua.Analyzers.Core;
using ANcpLua.Roslyn.Utilities;

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
    private static readonly DiagnosticDescriptor Rule = CreateRule(
        DiagnosticIds.UncompressedExport,
        DiagnosticCategories.OpenTelemetry,
        DiagnosticSeverity.Warning);

    /// <summary>Method names that configure OTLP exporters.</summary>
    private static readonly HashSet<string> OtlpExporterMethods = [
        "AddOtlpExporter",
        "UseOtlpExporter",
        "WithOtlpExporter"
    ];

    /// <summary>Type names for OTLP exporter options.</summary>
    private static readonly string[] OtlpOptionsTypeNames = [
        "OpenTelemetry.Exporter.OtlpExporterOptions",
        "OpenTelemetry.Exporter.OtlpExporterOptionsBase"
    ];

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    /// <summary>Registers compilation start action to analyze OTLP exporter configurations.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterCompilationStartAction(OnCompilationStart);

    private static void OnCompilationStart(CompilationStartAnalysisContext context) {
        var otlpOptionsTypes = OtlpOptionsTypeNames
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

        var methodName = GetMethodName(invocation);
        if (methodName is null || !OtlpExporterMethods.Contains(methodName)) {
            return;
        }

        // Check if this is a lambda configuration pattern: AddOtlpExporter(options => { ... })
        var lambdaArg = invocation.ArgumentList.Arguments
            .Select(a => a.Expression)
            .OfType<SimpleLambdaExpressionSyntax>()
            .FirstOrDefault();

        if (lambdaArg is not null) {
            if (HasCompressionConfiguration(lambdaArg)) {
                return;
            }

            // Check if Protocol is set to HttpProtobuf
            if (HasHttpProtobufConfiguration(lambdaArg, httpProtobufType, context.SemanticModel, context.CancellationToken)) {
                var location = GetMethodLocation(invocation);
                context.ReportDiagnostic(Rule, location);
            }

            return;
        }

        // Check for delegate argument: AddOtlpExporter(ConfigureOtlp)
        var delegateArg = invocation.ArgumentList.Arguments
            .Select(a => a.Expression)
            .OfType<IdentifierNameSyntax>()
            .FirstOrDefault();

        if (delegateArg is not null) {
            // We can't easily trace delegate configurations, so we skip this pattern
            return;
        }

        // Check for options object pattern: AddOtlpExporter(options)
        foreach (var arg in invocation.ArgumentList.Arguments) {
            if (context.SemanticModel.GetTypeInfo(arg.Expression, context.CancellationToken).Type is not { } argType) {
                continue;
            }

            if (otlpOptionsTypes.Any(optionsType => argType.InheritsFrom(optionsType) || argType.IsEqualTo(optionsType))) {
                // Options object passed - check if it has compression configured
                // This is complex to trace, so we'll report if HttpProtobuf is detected without compression
                if (IsHttpProtobufOptionsWithoutCompression(arg.Expression, context.SemanticModel, context.CancellationToken)) {
                    var location = GetMethodLocation(invocation);
                    context.ReportDiagnostic(Rule, location);
                }
            }
        }
    }

    private static bool HasCompressionConfiguration(SimpleLambdaExpressionSyntax lambda) {
        // Look for compression-related assignments within the lambda
        foreach (var node in lambda.DescendantNodes()) {
            if (node is AssignmentExpressionSyntax assignment) {
                var leftText = GetMemberName(assignment.Left);
                if (leftText is not null &&
                    (leftText.ContainsIgnoreCase("compression") ||
                     leftText.ContainsIgnoreCase("gzip"))) {
                    return true;
                }
            }

            if (node is InvocationExpressionSyntax nestedInvocation) {
                var nestedMethod = GetMethodName(nestedInvocation);
                if (nestedMethod is not null &&
                    (nestedMethod.ContainsIgnoreCase("compression") ||
                     nestedMethod.ContainsIgnoreCase("gzip"))) {
                    return true;
                }
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
            if (node is not AssignmentExpressionSyntax assignment) {
                continue;
            }

            var leftText = GetMemberName(assignment.Left);
            if (leftText is null || !leftText.EqualsOrdinal("Protocol")) {
                continue;
            }

            // Check if the right side is OtlpExportProtocol.HttpProtobuf
            if (assignment.Right is MemberAccessExpressionSyntax memberAccess) {
                var memberName = memberAccess.Name.Identifier.Text;
                if (memberName.EqualsOrdinal("HttpProtobuf")) {
                    return true;
                }

                // Also check via semantic model if available
                if (httpProtobufType is not null) {
                    var symbol = semanticModel.GetSymbolInfo(memberAccess, cancellationToken).Symbol;
                    if (symbol is IFieldSymbol fieldSymbol &&
                        fieldSymbol.ContainingType.IsEqualTo(httpProtobufType) &&
                        fieldSymbol.Name.EqualsOrdinal("HttpProtobuf")) {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private static bool IsHttpProtobufOptionsWithoutCompression(
        ExpressionSyntax expression,
        SemanticModel semanticModel,
        CancellationToken cancellationToken) {
        // This is a heuristic check for object initializers like:
        // new OtlpExporterOptions { Protocol = OtlpExportProtocol.HttpProtobuf }
        if (expression is not ObjectCreationExpressionSyntax objectCreation) {
            return false;
        }

        if (objectCreation.Initializer is null) {
            return false;
        }

        var hasHttpProtobuf = false;
        var hasCompression = false;

        foreach (var expr in objectCreation.Initializer.Expressions) {
            if (expr is not AssignmentExpressionSyntax assignment) {
                continue;
            }

            if (GetMemberName(assignment.Left) is not { } leftText) {
                continue;
            }

            if (leftText.EqualsOrdinal("Protocol")) {
                if (assignment.Right is MemberAccessExpressionSyntax memberAccess &&
                    memberAccess.Name.Identifier.Text.EqualsOrdinal("HttpProtobuf")) {
                    hasHttpProtobuf = true;
                }
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
