
namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0091: Detects usage of SimpleSpanProcessor or SimpleActivityExportProcessor which exports
///     spans one at a time instead of batching.
/// </summary>
/// <remarks>
///     <para>
///         SimpleSpanProcessor and SimpleActivityExportProcessor export each span immediately as it
///         completes, which can cause significant performance overhead in production applications.
///     </para>
///     <para>
///         BatchSpanProcessor and BatchActivityExportProcessor collect spans and export them in
///         configurable batches, reducing network overhead and improving application performance.
///     </para>
///     <para>
///         Simple processors are useful for debugging and development scenarios where immediate
///         export is desired, but batch processors are recommended for production use.
///     </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0091BatchExportDisabledAnalyzer : AlAnalyzer {
    /// <summary>The diagnostic identifier for AL0091.</summary>
    private const string DiagnosticId = "AL0091";

    private static readonly string[] s_simpleProcessorTypeNames = [
        "OpenTelemetry.Trace.SimpleSpanProcessor",
        "OpenTelemetry.Trace.SimpleActivityExportProcessor",
        "SimpleSpanProcessor",
        "SimpleActivityExportProcessor"
    ];

    private static readonly DiagnosticDescriptor s_rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.OpenTelemetry,
        DiagnosticSeverities.Suggestion);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_rule];

    /// <summary>Registers operation actions to analyze object creation.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterOperationAction(AnalyzeObjectCreation, OperationKind.ObjectCreation);

    private static void AnalyzeObjectCreation(OperationAnalysisContext context) {
        if (context.Operation is not IObjectCreationOperation { Type: { } type }) {
            return;
        }

        if (IsSimpleProcessor(type.ToDisplayString())) {
            context.ReportDiagnostic(Diagnostic.Create(s_rule, context.Operation.Syntax.GetLocation()));
        }
    }

    private static bool IsSimpleProcessor(string typeName) =>
        s_simpleProcessorTypeNames.Any(s => typeName.EndsWithOrdinal(s) || typeName.EqualsOrdinal(s));
}
