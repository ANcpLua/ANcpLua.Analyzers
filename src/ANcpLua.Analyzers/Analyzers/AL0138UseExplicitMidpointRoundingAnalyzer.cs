
namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0138: Suggests using <c>Math.Round</c> / <c>MathF.Round</c> overloads with an explicit
///     <c>MidpointRounding</c> argument instead of relying on the default banker's-rounding behavior.
/// </summary>
/// <remarks>
///     <list type="bullet">
    ///         <item><c>Math.Round(x)</c> -> <c>Math.Round(x, MidpointRounding.ToEven)</c></item>
    ///         <item><c>Math.Round(x, digits)</c> -> <c>Math.Round(x, digits, MidpointRounding.ToEven)</c></item>
    ///         <item><c>MathF.Round(x)</c> -> <c>MathF.Round(x, MidpointRounding.ToEven)</c></item>
    ///         <item><c>MathF.Round(x, digits)</c> -> <c>MathF.Round(x, digits, MidpointRounding.ToEven)</c></item>
///     </list>
///     <para>
    ///         Without an explicit MidpointRounding mode, .NET defaults to <c>ToEven</c> (banker's
    ///         rounding). The auto-fix preserves that behavior while making it explicit; callers can
    ///         choose <c>AwayFromZero</c> or another mode when the domain requires it.
///     </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0138UseExplicitMidpointRoundingAnalyzer : AlAnalyzer {
    /// <summary>The diagnostic identifier for AL0138.</summary>
    public const string DiagnosticId = "AL0138";

    private static readonly DiagnosticDescriptor s_rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.Reliability,
        DiagnosticSeverities.Suggestion);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_rule];

    /// <summary>Registers operation actions for static-method invocations on Math/MathF.Round.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);

    private static void AnalyzeInvocation(OperationAnalysisContext context) {
        if (context.Operation is not IInvocationOperation { TargetMethod: var method }) {
            return;
        }

        if (!IsRoundWithoutMidpointRounding(method)) {
            return;
        }

        var fullName = $"{method.ContainingType.Name}.{method.Name}";

        context.ReportDiagnostic(Diagnostic.Create(
            s_rule,
            context.Operation.Syntax.GetLocation(),
            fullName));
    }

    private static bool IsRoundWithoutMidpointRounding(IMethodSymbol method) {
        if (!method.IsStatic || method.Name != "Round") {
            return false;
        }

        if (method.ContainingType.ContainingNamespace?.ToDisplayString() is not "System") {
            return false;
        }

        if (method.ContainingType.Name is not ("Math" or "MathF")) {
            return false;
        }

        // The dangerous overloads are the ones WITHOUT MidpointRounding — i.e., where no parameter
        // is of type MidpointRounding. The safe overloads accept it as their last parameter.
        foreach (var parameter in method.Parameters) {
            if (parameter.Type.Name == "MidpointRounding" &&
                parameter.Type.ContainingNamespace?.ToDisplayString() == "System") {
                return false;
            }
        }

        return true;
    }
}
