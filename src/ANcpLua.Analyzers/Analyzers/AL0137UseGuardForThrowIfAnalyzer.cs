
namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0137: Suggests using <c>Guard.*</c> helpers from <c>ANcpLua.Roslyn.Utilities</c> instead
///     of the BCL throw helpers on <c>ArgumentNullException</c>, <c>ArgumentException</c>, and
///     <c>ArgumentOutOfRangeException</c>.
/// </summary>
/// <remarks>
///     <list type="bullet">
///         <item><c>ArgumentNullException.ThrowIfNull(x)</c> → <c>Guard.NotNull(x)</c></item>
///         <item><c>ArgumentException.ThrowIfNullOrEmpty(s)</c> → <c>Guard.NotNullOrEmpty(s)</c></item>
///         <item><c>ArgumentException.ThrowIfNullOrWhiteSpace(s)</c> → <c>Guard.NotNullOrWhiteSpace(s)</c></item>
///         <item><c>ArgumentOutOfRangeException.ThrowIfZero(x)</c> → <c>Guard.NotZero(x)</c></item>
///         <item><c>ArgumentOutOfRangeException.ThrowIfNegative(x)</c> → <c>Guard.NotNegative(x)</c></item>
///         <item><c>ArgumentOutOfRangeException.ThrowIfNegativeOrZero(x)</c> → <c>Guard.Positive(x)</c></item>
///         <item><c>ArgumentOutOfRangeException.ThrowIfGreaterThan(v, max)</c> → <c>Guard.NotGreaterThan(v, max)</c></item>
///         <item><c>ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(v, max)</c> → <c>Guard.LessThan(v, max)</c></item>
///         <item><c>ArgumentOutOfRangeException.ThrowIfLessThan(v, min)</c> → <c>Guard.NotLessThan(v, min)</c></item>
///         <item><c>ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(v, min)</c> → <c>Guard.GreaterThan(v, min)</c></item>
///     </list>
///     The <c>Guard.*</c> variants return the validated value (so they compose into expressions and
///     property initializers) and reuse the same <c>[CallerArgumentExpression]</c> machinery for
///     parameter naming. Kept as a separate diagnostic so it can be auto-fixed via the matching
///     <c>Al0137UseGuardForThrowIfCodeFixProvider</c>.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0137UseGuardForThrowIfAnalyzer : AlAnalyzer {
    /// <summary>The diagnostic identifier for AL0137.</summary>
    public const string DiagnosticId = "AL0137";

    /// <summary>Property key carrying the target Guard.* method name (e.g. <c>NotNull</c>).</summary>
    public const string PropertyGuardMethod = "GuardMethod";

    private static readonly DiagnosticDescriptor Rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.RoslynUtilities,
        DiagnosticSeverities.Suggestion);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    /// <summary>Registers operation actions for static-method invocations.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);

    private static void AnalyzeInvocation(OperationAnalysisContext context) {
        if (context.Operation is not IInvocationOperation { TargetMethod: var method }) {
            return;
        }

        if (Map(method) is not { } guardMethod) {
            return;
        }

        var properties = ImmutableDictionary.CreateBuilder<string, string?>();
        properties.Add(PropertyGuardMethod, guardMethod);

        var bclName = $"{method.ContainingType.Name}.{method.Name}";
        var guardName = $"Guard.{guardMethod}";

        context.ReportDiagnostic(Diagnostic.Create(
            Rule,
            context.Operation.Syntax.GetLocation(),
            properties.ToImmutable(),
            bclName,
            guardName));
    }

    private static string? Map(IMethodSymbol method) {
        if (!method.IsStatic) {
            return null;
        }

        if (method.ContainingType.ContainingNamespace?.ToDisplayString() is not "System") {
            return null;
        }

        return (method.ContainingType.Name, method.Name) switch {
            ("ArgumentNullException", "ThrowIfNull") => "NotNull",
            ("ArgumentException", "ThrowIfNullOrEmpty") => "NotNullOrEmpty",
            ("ArgumentException", "ThrowIfNullOrWhiteSpace") => "NotNullOrWhiteSpace",
            ("ArgumentOutOfRangeException", "ThrowIfZero") => "NotZero",
            ("ArgumentOutOfRangeException", "ThrowIfNegative") => "NotNegative",
            ("ArgumentOutOfRangeException", "ThrowIfNegativeOrZero") => "Positive",
            ("ArgumentOutOfRangeException", "ThrowIfGreaterThan") => "NotGreaterThan",
            ("ArgumentOutOfRangeException", "ThrowIfGreaterThanOrEqual") => "LessThan",
            ("ArgumentOutOfRangeException", "ThrowIfLessThan") => "NotLessThan",
            ("ArgumentOutOfRangeException", "ThrowIfLessThanOrEqual") => "GreaterThan",
            _ => null
        };
    }
}
