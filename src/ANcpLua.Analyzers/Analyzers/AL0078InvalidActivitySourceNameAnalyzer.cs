
namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0078: Detects ActivitySource names that don't follow reverse-DNS naming convention.
/// </summary>
/// <remarks>
///     <para>
///         ActivitySource names should follow the reverse-DNS convention (e.g., 'company.product.component').
///         Valid names must:
///         <list type="bullet">
///             <item>Contain at least one dot to indicate hierarchical namespace</item>
///             <item>Use only lowercase letters, digits, dots, and hyphens</item>
///             <item>Not contain spaces or other invalid characters</item>
///             <item>Not be empty or whitespace-only</item>
///         </list>
///     </para>
///     <para>
///         This naming convention ensures consistent identification across telemetry backends
///         and follows OpenTelemetry best practices.
///     </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0078InvalidActivitySourceNameAnalyzer : AlAnalyzer {
    /// <summary>The diagnostic identifier for AL0078.</summary>
    private const string DiagnosticId = "AL0078";

    private const string ActivitySourceTypeName = "System.Diagnostics.ActivitySource";

    private static readonly DiagnosticDescriptor s_rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.OpenTelemetry,
        DiagnosticSeverities.RequiredFix);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_rule];

    /// <summary>Registers operation actions to analyze ActivitySource creation.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterOperationAction(AnalyzeObjectCreation, OperationKind.ObjectCreation);

    private static void AnalyzeObjectCreation(OperationAnalysisContext context) {
        var objectCreation = (IObjectCreationOperation)context.Operation;

        if (objectCreation.Type?.ToDisplayString() != ActivitySourceTypeName
            || objectCreation.Arguments is not [{ Value.ConstantValue: { HasValue: true, Value: string sourceName } } firstArg, ..]) {
            return;
        }

        if (!IsValidActivitySourceName(sourceName)) {
            context.ReportDiagnostic(Diagnostic.Create(s_rule, firstArg.Syntax.GetLocation(), sourceName));
        }
    }

    private static bool IsValidActivitySourceName(string name) {
        if (string.IsNullOrWhiteSpace(name) || !name.ContainsOrdinal(".") || name.ContainsOrdinal(" ")) {
            return false;
        }

        var segments = name.Split('.');
        foreach (var segment in segments) {
            if (string.IsNullOrEmpty(segment)) {
                return false;
            }

            // Allow letters (upper and lower), digits, and hyphens
            foreach (var c in segment) {
                if (!char.IsLetter(c) && !char.IsDigit(c) && c != '-') {
                    return false;
                }
            }
        }

        return true;
    }
}
