
namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0137: Suggests using <c>Guard.*</c> helpers from <c>ANcpLua.Roslyn.Utilities</c> instead
///     of the BCL throw helpers <c>ArgumentNullException.ThrowIfNull</c>,
///     <c>ArgumentException.ThrowIfNullOrEmpty</c>, and <c>ArgumentException.ThrowIfNullOrWhiteSpace</c>.
/// </summary>
/// <remarks>
///     <list type="bullet">
///         <item><c>ArgumentNullException.ThrowIfNull(x)</c> → <c>Guard.NotNull(x)</c></item>
///         <item><c>ArgumentException.ThrowIfNullOrEmpty(s)</c> → <c>Guard.NotNullOrEmpty(s)</c></item>
///         <item><c>ArgumentException.ThrowIfNullOrWhiteSpace(s)</c> → <c>Guard.NotNullOrWhiteSpace(s)</c></item>
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
            _ => null
        };
    }
}
