
namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0036: Suggests using Guard.NotNull() instead of null-coalescing throw patterns.
/// </summary>
/// <remarks>
///     <list type="bullet">
///         <item><c>value ?? throw new ArgumentNullException(nameof(value))</c> → <c>Guard.NotNull(value)</c></item>
///         <item><c>value ?? throw new ArgumentNullException("value")</c> → <c>Guard.NotNull(value)</c></item>
///     </list>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0036UseGuardNotNullAnalyzer : AlAnalyzer {
    /// <summary>The diagnostic identifier for AL0036.</summary>
    public const string DiagnosticId = "AL0036";

    private static readonly DiagnosticDescriptor Rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.RoslynUtilities,
        DiagnosticSeverities.Suggestion);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    /// <summary>Registers syntax or operation actions for analysis.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterOperationAction(AnalyzeCoalesce, OperationKind.Coalesce);

    private static void AnalyzeCoalesce(OperationAnalysisContext context) {
        if (context.Operation is not ICoalesceOperation coalesce ||
            !IsArgumentNullExceptionThrow(coalesce.WhenNull)) {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(Rule, coalesce.Syntax.GetLocation(), coalesce.Value.GetOperandName()));
    }

    private static bool IsArgumentNullExceptionThrow(IOperation? operation) {
        // Fast path: only conversions and throws are relevant
        if (operation is not IConversionOperation and not IThrowOperation and null) {
            return false;
        }

        while (operation is IConversionOperation conversion) {
            operation = conversion.Operand;
        }

        return operation is IThrowOperation { Exception: { } exception } &&
               exception.UnwrapAllConversions() is IObjectCreationOperation { Type: { } exceptionType } &&
               OperationHelper.IsArgumentNullException(exceptionType);
    }
}
