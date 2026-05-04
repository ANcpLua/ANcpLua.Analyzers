
namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0096: Enable EventSourceSupport for AOT with telemetry.
/// </summary>
/// <remarks>
///     <para>
///         Native AOT trims EventSource/EventPipe infrastructure by default.
///         If the application uses OpenTelemetry, dotnet-trace, dotnet-counters, or other
///         EventPipe-based diagnostics, <c>&lt;EventSourceSupport&gt;true&lt;/EventSourceSupport&gt;</c>
///         must be set in the project file to preserve this infrastructure.
///     </para>
///     <para>
///         This analyzer checks MSBuild properties exposed via <c>CompilerVisibleProperty</c> to detect
///         when <c>PublishAot</c> is true but <c>EventSourceSupport</c> is not explicitly enabled.
///     </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0096EnableEventSourceSupportAnalyzer : AlAnalyzer {
    /// <summary>The diagnostic identifier for AL0096.</summary>
    private const string DiagnosticId = "AL0096";

    private const string PublishAotProperty = "build_property.PublishAot";
    private const string EventSourceSupportProperty = "build_property.EventSourceSupport";

    private static readonly DiagnosticDescriptor s_rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.Configuration,
        DiagnosticSeverities.Suggestion);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_rule];

    /// <summary>Registers a compilation action to check MSBuild properties.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterCompilationAction(AnalyzeCompilation);

    private static void AnalyzeCompilation(CompilationAnalysisContext context) {
        var globalOptions = context.Options.AnalyzerConfigOptionsProvider.GlobalOptions;

        if (!globalOptions.TryGetValue(PublishAotProperty, out var publishAot)
            || !string.Equals(publishAot, "true", StringComparison.OrdinalIgnoreCase)) {
            return;
        }

        if (globalOptions.TryGetValue(EventSourceSupportProperty, out var eventSourceSupport)
            && string.Equals(eventSourceSupport, "true", StringComparison.OrdinalIgnoreCase)) {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(s_rule, Location.None));
    }
}
