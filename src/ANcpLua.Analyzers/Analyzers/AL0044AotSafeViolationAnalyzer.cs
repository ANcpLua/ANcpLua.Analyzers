
namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0044: Code marked [AotSafe] must not call methods with [RequiresDynamicCode].
/// </summary>
/// <remarks>
///     <para>
///         The [AotSafe] attribute indicates that code is safe to use in AOT-compiled applications.
///         Calling methods with [RequiresDynamicCode] violates this guarantee because those methods
///         rely on runtime code generation which is not available in AOT scenarios.
///     </para>
///     <para>
///         This analyzer checks both direct calls and transitive calls within the same compilation.
///         It reports warnings when [AotSafe] code calls methods that require dynamic code generation.
///     </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0044AotSafeViolationAnalyzer : AlAnalyzer {
    /// <summary>The diagnostic identifier for AL0044.</summary>
    private const string DiagnosticId = "AL0044";

    private const string AotSafeAttributeName = "AotSafe";
    private const string RequiresDynamicCodeAttributeName = "RequiresDynamicCode";

    private static readonly DiagnosticDescriptor Rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.AotTesting,
        DiagnosticSeverity.Warning);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    /// <inheritdoc />
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterCompilationStartAction(OnCompilationStart);

    private static void OnCompilationStart(CompilationStartAnalysisContext context) =>
        context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);

    private static void AnalyzeInvocation(OperationAnalysisContext context) {
        var invocation = (IInvocationOperation)context.Operation;

        if (context.ContainingSymbol is not IMethodSymbol callingMethod) {
            return;
        }

        if (!callingMethod.HasAttributeByShortName(AotSafeAttributeName) &&
            callingMethod.ContainingType?.HasAttributeByShortName(AotSafeAttributeName) is not true) {
            return;
        }

        if (!invocation.TargetMethod.HasAttributeByShortName(RequiresDynamicCodeAttributeName)) {
            return;
        }

        context.ReportDiagnostic(Rule, invocation.Syntax.GetLocation(),
            callingMethod.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
            invocation.TargetMethod.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
    }
}
