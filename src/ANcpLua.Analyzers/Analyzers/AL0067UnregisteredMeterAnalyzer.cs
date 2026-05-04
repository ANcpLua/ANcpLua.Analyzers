
namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0067: Detects Meter instances that are not registered with AddMeter() anywhere in the compilation.
/// </summary>
/// <remarks>
///     <para>
///         Meters must be registered via <c>MeterProviderBuilder.AddMeter(params string[] names)</c> in the
///         OpenTelemetry metrics configuration to export metrics. Unregistered meters silently discard data.
///     </para>
///     <para>
///         The analyzer resolves string arguments through constant propagation, so both literals
///         (<c>AddMeter("qyl.agent")</c>) and symbolic references (<c>AddMeter(ActivitySources.Agent)</c> where
///         <c>Agent</c> is a <c>const string</c>) are recognised as registrations.
///     </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0067UnregisteredMeterAnalyzer : AlAnalyzer {
    /// <summary>The diagnostic identifier for AL0067.</summary>
    private const string DiagnosticId = "AL0067";

    private const string MeterTypeName = "System.Diagnostics.Metrics.Meter";
    private const string AddMeterMethodName = "AddMeter";

    private static readonly DiagnosticDescriptor s_rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.Metrics,
        DiagnosticSeverities.Suggestion);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_rule];

    /// <summary>Registers compilation-wide analysis to correlate Meter creations with AddMeter registrations.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterCompilationStartAction(OnCompilationStart);

    private static void OnCompilationStart(CompilationStartAnalysisContext context) {
        var registered = new ConcurrentDictionary<string, byte>(StringComparer.Ordinal);
        var creations = new ConcurrentBag<(Location Location, string Name)>();

        context.RegisterOperationAction(
            ctx => CollectRegistrations((IInvocationOperation)ctx.Operation, registered),
            OperationKind.Invocation);

        context.RegisterOperationAction(
            ctx => CollectCreation((IObjectCreationOperation)ctx.Operation, creations),
            OperationKind.ObjectCreation);

        context.RegisterCompilationEndAction(end => {
            foreach (var (location, name) in creations) {
                if (!registered.ContainsKey(name)) {
                    end.ReportDiagnostic(Diagnostic.Create(s_rule, location, name));
                }
            }
        });
    }

    private static void CollectRegistrations(
        IInvocationOperation invocation,
        ConcurrentDictionary<string, byte> registered) {
        if (invocation.TargetMethod.Name is not AddMeterMethodName) {
            return;
        }

        foreach (var argument in invocation.Arguments) {
            CollectStringValues(argument.Value, registered);
        }
    }

    private static void CollectStringValues(IOperation op, ConcurrentDictionary<string, byte> collector) {
        if (op.ConstantValue is { HasValue: true, Value: string s }) {
            collector.TryAdd(s, 0);
            return;
        }

        // params string[] is lowered to an implicit array creation; descend into its elements.
        if (op is IArrayCreationOperation { Initializer: { } initializer }) {
            foreach (var element in initializer.ElementValues) {
                CollectStringValues(element, collector);
            }
        }
    }

    private static void CollectCreation(
        IObjectCreationOperation creation,
        ConcurrentBag<(Location Location, string Name)> creations) {
        if (creation.Type?.ToDisplayString() != MeterTypeName ||
            creation.Arguments.Length is 0 ||
            creation.Arguments[0].Value.ConstantValue is not { HasValue: true, Value: string meterName }) {
            return;
        }

        creations.Add((creation.Syntax.GetLocation(), meterName));
    }
}
