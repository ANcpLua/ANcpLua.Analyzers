
namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL1220: Suggests using <c>Guard.*</c> helpers from <c>ANcpLua.Roslyn.Utilities</c> instead of
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
public sealed partial class Al1220UseGuardForThrowIfAnalyzer : AlAnalyzer {
    /// <summary>The diagnostic identifier for AL1220.</summary>
    public const string DiagnosticId = "AL1220";

    /// <summary>Property key carrying the target Guard.* method name (e.g. <c>NotNull</c>).</summary>
    public const string PropertyGuardMethod = "GuardMethod";

    private const string GuardMetadataName = "ANcpLua.Roslyn.Utilities.Guard";

    /// <summary>The fully-qualified namespace of MAF's Throw helper class.</summary>
    private const string MafThrowNamespace = "Microsoft.Shared.Diagnostics";

    private static readonly DiagnosticDescriptor s_rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.RoslynUtilities,
        DiagnosticSeverities.Suggestion);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_rule];

    /// <summary>Registers operation actions for static-method invocations.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterCompilationStartAction(OnCompilationStart);

    private static void OnCompilationStart(CompilationStartAnalysisContext context) {
        // Guard lives in ANcpLua.Roslyn.Utilities. Only fire when present and callable from this
        // compilation; otherwise the code fix would rewrite to a symbol the consumer cannot resolve.
        if (context.Compilation.GetTypeByMetadataName(GuardMetadataName) is not { } gateType) {
            return;
        }

        if (!context.Compilation.IsSymbolAccessibleWithin(gateType, context.Compilation.Assembly)) {
            return;
        }

        context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
    }

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
            s_rule,
            context.Operation.Syntax.GetLocation(),
            properties.ToImmutable(),
            sourceName,
            guardName));
    }

    private static string? Map(IMethodSymbol method) {
        if (!method.IsStatic) {
            return null;
        }

        // BCL System.* — ContainingType is one of the three Argument*Exception types.
        if (OperationHelper.IsArgumentNullException(method.ContainingType)) {
            return method.Name is "ThrowIfNull" ? "NotNull" : null;
        }

        if (OperationHelper.IsArgumentException(method.ContainingType)) {
            return method.Name switch {
                "ThrowIfNullOrEmpty" => "NotNullOrEmpty",
                "ThrowIfNullOrWhiteSpace" => "NotNullOrWhiteSpace",
                _ => null
            };
        }

        if (OperationHelper.IsArgumentOutOfRangeException(method.ContainingType)) {
            return MapBclOutOfRange(method);
        }

        // MAF Microsoft.Shared.Diagnostics.Throw — separate detection path.
        if (method.ContainingType.Name is "Throw" &&
            method.ContainingType.ContainingNamespace?.ToDisplayString() is MafThrowNamespace) {
            return MapMaf(method);
        }

        return null;
    }

    private static string? MapBclOutOfRange(IMethodSymbol method) {
        // Guard.* covers int / long / double / decimal / TimeSpan but NOT uint / ulong, so
        // reject the unsigned BCL overloads — the auto-fix would emit code that doesn't compile.
        if (HasUnsupportedNumericFirstParam(method)) {
            return null;
        }

        return method.Name switch {
            "ThrowIfZero" => "NotZero",
            "ThrowIfNegative" => "NotNegative",
            "ThrowIfNegativeOrZero" => "Positive",
            "ThrowIfGreaterThan" => "NotGreaterThan",
            "ThrowIfGreaterThanOrEqual" => "LessThan",
            "ThrowIfLessThan" => "NotLessThan",
            "ThrowIfLessThanOrEqual" => "GreaterThan",
            _ => null
        };
    }

    // MAF Microsoft.Shared.Diagnostics.Throw.* — the validating subset.
    // Cold-path-only helpers (ArgumentNullException, ArgumentOutOfRangeException,
    // ArgumentException, InvalidOperationException) are NOT mapped — they don't validate,
    // they just throw, and have no Guard.* counterpart.
    private static string? MapMaf(IMethodSymbol method) {
        // Reject uint/ulong overloads — Guard.* doesn't cover unsigned numerics.
        if (HasUnsupportedNumericFirstParam(method)) {
            return null;
        }

        // IfOutOfRange has TWO unrelated overloads:
        //   - Generic enum check:  IfOutOfRange<T>(T arg) where T : struct, Enum  →  Guard.DefinedEnum
        //   - Numeric range check: IfOutOfRange(int v, int min, int max)          →  Guard.InRange
        if (method.Name is "IfOutOfRange") {
            return method.IsGenericMethod && method.Parameters.Length is 2
                ? "DefinedEnum"
                : "InRange";
        }

        // IfNullOrEmpty has TWO overloads:
        //   - string variant:     IfNullOrEmpty(string? arg)
        //   - collection variant: IfNullOrEmpty<T>(IEnumerable<T>? arg)
        // Guard.NotNullOrEmpty covers `string` and `IReadOnlyCollection<T>`. The MAF collection
        // overload takes `IEnumerable<T>?` — broader than what Guard accepts. Skip if the call
        // site argument doesn't satisfy IReadOnlyCollection<T> (we can't tell without semantic
        // model on the argument, so be conservative and only map the string overload here; the
        // collection case can fall through to "no auto-fix" rather than emit broken code).
        if (method.Name is "IfNullOrEmpty") {
            if (method.Parameters.Length >= 1 &&
                method.Parameters[0].Type.SpecialType is SpecialType.System_String) {
                return "NotNullOrEmpty";
            }
            // Collection overload — Guard requires IReadOnlyCollection<T>; can't safely auto-fix
            // a generic IEnumerable<T> call without compile-time risk. Leave for human review.
            return null;
        }

        return method.Name switch {
            "IfNull" => "NotNull",
            "IfNullOrMemberNull" => "NotNullWithMember",
            "IfMemberNull" => "MemberNotNull",
            "IfNullOrWhitespace" => "NotNullOrWhiteSpace",
            "IfZero" => "NotZero",
            "IfLessThan" => "NotLessThan",
            "IfGreaterThan" => "NotGreaterThan",
            "IfLessThanOrEqual" => "GreaterThan",
            "IfGreaterThanOrEqual" => "LessThan",
            _ => null
        };
    }

    private static bool HasUnsupportedNumericFirstParam(IMethodSymbol method) =>
        method.Parameters.Length >= 1 && method.Parameters[0].Type.SpecialType is
            SpecialType.System_UInt32 or SpecialType.System_UInt64;
}
