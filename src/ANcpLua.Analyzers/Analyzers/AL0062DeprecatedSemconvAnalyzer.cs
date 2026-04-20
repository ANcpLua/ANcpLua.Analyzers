
namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0062: Detects usage of deprecated OpenTelemetry semantic convention attributes.
/// </summary>
/// <remarks>
///     <para>
///         Some semantic convention attribute names have been deprecated and replaced
///         with newer names. This analyzer helps migrate to the current conventions.
///     </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0062DeprecatedSemconvAnalyzer : AlAnalyzer {
    /// <summary>The diagnostic identifier for AL0062.</summary>
    private const string DiagnosticId = "AL0062";

    private static readonly DiagnosticDescriptor Rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.OpenTelemetry,
        DiagnosticSeverities.Suggestion);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    internal static bool TryGetDeprecatedAttribute(string attributeName, out (string Replacement, string Version) info) =>
        OpenTelemetryDeprecatedSemconvCatalog.TryGetDeprecatedAttribute(attributeName, out info);

    /// <summary>Registers operation actions to analyze SetTag calls.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);

    private static void AnalyzeInvocation(OperationAnalysisContext context) {
        var invocation = (IInvocationOperation)context.Operation;

        if (invocation.TargetMethod.Name is not ("SetTag" or "AddTag" or "SetAttribute" or "Add") ||
            invocation.Arguments.Length is 0 ||
            !invocation.Arguments[0].Value.TryGetConstantValue(out string? attributeName) ||
            attributeName is null ||
            !TryGetDeprecatedAttribute(attributeName, out var info)) {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            Rule,
            invocation.Arguments[0].Syntax.GetLocation(),
            attributeName,
            info.Version,
            info.Replacement));
    }
}
