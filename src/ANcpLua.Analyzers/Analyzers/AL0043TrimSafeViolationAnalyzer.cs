
namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0043: Code marked [TrimSafe] must not call methods with [RequiresUnreferencedCode].
/// </summary>
/// <remarks>
///     <para>
///         The [TrimSafe] attribute indicates that code is safe to use in trimmed applications.
///         Calling methods with [RequiresUnreferencedCode] violates this guarantee because those
///         methods may fail at runtime when types are trimmed away.
///     </para>
///     <para>
///         This analyzer checks both direct calls and transitive calls within the same compilation.
///         It reports warnings when [TrimSafe] code calls unsafe methods.
///     </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0043TrimSafeViolationAnalyzer : AlAnalyzer {
    /// <summary>The diagnostic identifier for AL0043.</summary>
    private const string DiagnosticId = "AL0043";

    private const string TrimSafeAttributeName = "TrimSafe";
    private const string RequiresUnreferencedCodeAttributeName = "RequiresUnreferencedCode";

    private static readonly DiagnosticDescriptor s_rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.AotTesting,
        DiagnosticSeverity.Warning);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_rule];

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

        if (!callingMethod.HasAttributeByShortName(TrimSafeAttributeName) &&
            callingMethod.ContainingType?.HasAttributeByShortName(TrimSafeAttributeName) is not true) {
            return;
        }

        if (!invocation.TargetMethod.HasAttributeByShortName(RequiresUnreferencedCodeAttributeName)) {
            return;
        }

        context.ReportDiagnostic(s_rule, invocation.Syntax.GetLocation(),
            callingMethod.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
            invocation.TargetMethod.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
    }
}
