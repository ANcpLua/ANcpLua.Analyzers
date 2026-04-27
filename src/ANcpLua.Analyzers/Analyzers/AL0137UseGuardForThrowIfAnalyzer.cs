
namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0137: Suggests using <c>Guard.*</c> helpers from <c>ANcpLua.Roslyn.Utilities</c> instead of
///     the BCL <c>System.*Exception.ThrowIf*</c> helpers and Microsoft Agent Framework's
///     <c>Microsoft.Shared.Diagnostics.Throw.If*</c> helpers.
/// </summary>
/// <remarks>
///     <para>BCL throw helpers (System namespace):</para>
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
///     <para>Microsoft Agent Framework helpers (<c>Microsoft.Shared.Diagnostics.Throw</c>):</para>
///     <list type="bullet">
///         <item><c>Throw.IfNull(x)</c> → <c>Guard.NotNull(x)</c></item>
///         <item><c>Throw.IfNullOrMemberNull(p, m)</c> → <c>Guard.NotNullWithMember(p, m)</c></item>
///         <item><c>Throw.IfNullOrEmpty(s)</c> → <c>Guard.NotNullOrEmpty(s)</c> (string and collection variants)</item>
///         <item><c>Throw.IfNullOrWhitespace(s)</c> → <c>Guard.NotNullOrWhiteSpace(s)</c></item>
///         <item><c>Throw.IfZero(v)</c> → <c>Guard.NotZero(v)</c></item>
///         <item><c>Throw.IfLessThan(v, min)</c> → <c>Guard.NotLessThan(v, min)</c></item>
///         <item><c>Throw.IfGreaterThan(v, max)</c> → <c>Guard.NotGreaterThan(v, max)</c></item>
///         <item><c>Throw.IfLessThanOrEqual(v, min)</c> → <c>Guard.GreaterThan(v, min)</c></item>
///         <item><c>Throw.IfGreaterThanOrEqual(v, max)</c> → <c>Guard.LessThan(v, max)</c></item>
///         <item><c>Throw.IfOutOfRange(v, min, max)</c> → <c>Guard.InRange(v, min, max)</c></item>
///     </list>
///     <para>
///         The <c>Guard.*</c> variants return the validated value (so they compose into expressions and
///         property initializers) and reuse the same <c>[CallerArgumentExpression]</c> machinery for
///         parameter naming. The cold-path-only <c>Throw.ArgumentNullException</c> /
///         <c>Throw.ArgumentOutOfRangeException</c> / <c>Throw.ArgumentException</c> /
///         <c>Throw.InvalidOperationException</c> methods are out of scope — they don't validate
///         anything, they just throw, and have no Guard equivalent.
///     </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0137UseGuardForThrowIfAnalyzer : AlAnalyzer {
    /// <summary>The diagnostic identifier for AL0137.</summary>
    public const string DiagnosticId = "AL0137";

    /// <summary>Property key carrying the target Guard.* method name (e.g. <c>NotNull</c>).</summary>
    public const string PropertyGuardMethod = "GuardMethod";

    /// <summary>The fully-qualified namespace of MAF's Throw helper class.</summary>
    private const string MafThrowNamespace = "Microsoft.Shared.Diagnostics";

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

        var sourceName = $"{method.ContainingType.Name}.{method.Name}";
        var guardName = $"Guard.{guardMethod}";

        context.ReportDiagnostic(Diagnostic.Create(
            Rule,
            context.Operation.Syntax.GetLocation(),
            properties.ToImmutable(),
            sourceName,
            guardName));
    }

    private static string? Map(IMethodSymbol method) {
        if (!method.IsStatic) {
            return null;
        }

        var ns = method.ContainingType.ContainingNamespace?.ToDisplayString();

        return ns switch {
            "System" => MapBcl(method.ContainingType.Name, method.Name),
            MafThrowNamespace when method.ContainingType.Name is "Throw" => MapMaf(method.Name),
            _ => null
        };
    }

    private static string? MapBcl(string typeName, string methodName) =>
        (typeName, methodName) switch {
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

    // MAF Microsoft.Shared.Diagnostics.Throw.* — the validating subset.
    // The cold-path-only helpers (ArgumentNullException, ArgumentOutOfRangeException,
    // ArgumentException, InvalidOperationException) are intentionally NOT mapped — those don't
    // validate, they just throw, and have no Guard.* counterpart.
    private static string? MapMaf(string methodName) =>
        methodName switch {
            "IfNull" => "NotNull",
            "IfNullOrMemberNull" => "NotNullWithMember",
            "IfNullOrEmpty" => "NotNullOrEmpty",
            "IfNullOrWhitespace" => "NotNullOrWhiteSpace",
            "IfZero" => "NotZero",
            "IfLessThan" => "NotLessThan",
            "IfGreaterThan" => "NotGreaterThan",
            "IfLessThanOrEqual" => "GreaterThan",
            "IfGreaterThanOrEqual" => "LessThan",
            "IfOutOfRange" => "InRange",
            _ => null
        };
}
