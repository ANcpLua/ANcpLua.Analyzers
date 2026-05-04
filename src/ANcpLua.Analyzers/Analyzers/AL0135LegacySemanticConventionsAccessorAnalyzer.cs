namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0135: Flags usage of the legacy aggregated semantic-convention accessor types
///     (<c>SemanticConventions</c>, <c>TraceSemanticConventions</c>, <c>ResourceSemanticConventions</c>)
///     and steers callers toward the per-domain grouped classes published under
///     <c>OpenTelemetry.SemanticConventions.Attributes.*</c>.
/// </summary>
/// <remarks>
///     The aggregated classes were the initial surface shape of the
///     <c>OpenTelemetry.SemanticConventions</c> package. Newer releases split the catalog
///     into one static class per convention namespace (HttpAttributes, ServerAttributes,
///     DbAttributes, …). Keeping code on the aggregator blocks structured refactors on
///     convention upgrades and produces noisy diffs because every convention change lands
///     in the same file. No code fix is offered because the legacy-to-grouped field
///     mapping is not a simple rename (for example <c>AttributeHttpMethod</c> →
///     <c>HttpAttributes.AttributeHttpRequestMethod</c>); operators must choose the right
///     grouped class per field.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0135LegacySemanticConventionsAccessorAnalyzer : AlAnalyzer {
    private const string DiagnosticId = "AL0135";

    private static readonly DiagnosticDescriptor s_rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.OpenTelemetry,
        DiagnosticSeverity.Warning);

    private static readonly string[] s_legacyAccessorTypes = [
        "OpenTelemetry.SemanticConventions.SemanticConventions",
        "OpenTelemetry.Trace.TraceSemanticConventions",
        "OpenTelemetry.Resource.ResourceSemanticConventions",
        "OpenTelemetry.Resources.ResourceSemanticConventions"
    ];

    private const string GroupedReplacement = "OpenTelemetry.SemanticConventions.Attributes.*Attributes";

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_rule];

    /// <summary>Registers a compilation-start action that resolves legacy accessor types.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterCompilationStartAction(OnCompilationStart);

    private static void OnCompilationStart(CompilationStartAnalysisContext context) {
        var legacyTypes = s_legacyAccessorTypes
            .Select(context.Compilation.GetTypeByMetadataName)
            .WhereNotNull()
            .ToImmutableArray();

        if (legacyTypes.IsEmpty) {
            return;
        }

        context.RegisterOperationAction(
            ctx => AnalyzeFieldReference(ctx, legacyTypes),
            OperationKind.FieldReference);
    }

    private static void AnalyzeFieldReference(
        OperationAnalysisContext context,
        ImmutableArray<INamedTypeSymbol> legacyTypes) {
        var fieldReference = (IFieldReferenceOperation)context.Operation;

        if (fieldReference.Field.ContainingType is not { } container
            || !legacyTypes.Any(container.IsEqualTo)) {
            return;
        }

        var qualifiedMember = $"{container.Name}.{fieldReference.Field.Name}";
        context.ReportDiagnostic(s_rule, fieldReference.Syntax.GetLocation(), qualifiedMember, GroupedReplacement);
    }
}
