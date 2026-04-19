
namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0117: Flags unnecessary ToList()/ToArray() calls immediately after LINQ operators.
/// </summary>
/// <remarks>
///     <para>
///         Calling <c>ToList()</c> or <c>ToArray()</c> immediately after a LINQ operator such as
///         <c>Where()</c>, <c>Select()</c>, <c>OfType()</c>, etc. forces eager evaluation and
///         allocates an intermediate collection. If the result is only enumerated once or assigned
///         to <c>IEnumerable&lt;T&gt;</c>, deferred execution avoids the allocation entirely.
///     </para>
///     <para>
///         Examples of unnecessary materialization:
///         <list type="bullet">
///             <item><c>items.Where(x =&gt; x &gt; 0).ToList()</c></item>
///             <item><c>items.Select(x =&gt; x.Name).ToArray()</c></item>
///             <item><c>items.OfType&lt;string&gt;().ToList()</c></item>
///         </list>
///     </para>
///     <para>
///         This is an Info-severity diagnostic (IDE-only) because there are legitimate reasons
///         to materialize, such as avoiding multiple enumeration or capturing a snapshot. The
///         diagnostic surfaces the pattern for developer review.
///     </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0117UnnecessaryLinqMaterializationAnalyzer : AlAnalyzer {
    /// <summary>The diagnostic identifier for AL0117.</summary>
    private const string DiagnosticId = "AL0117";

    private static readonly DiagnosticDescriptor Rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.Usage,
        DiagnosticSeverities.HiddenByDefault);

    private static readonly ImmutableHashSet<string> MaterializationMethods =
        ImmutableHashSet.Create(StringComparer.Ordinal, "ToList", "ToArray");

    private static readonly ImmutableHashSet<string> LinqOperators =
        ImmutableHashSet.Create(StringComparer.Ordinal,
            "Where", "Select", "SelectMany",
            "OfType", "Cast",
            "Distinct", "DistinctBy",
            "OrderBy", "OrderByDescending", "ThenBy", "ThenByDescending",
            "GroupBy", "GroupJoin", "Join",
            "Skip", "SkipWhile", "Take", "TakeWhile",
            "Concat", "Union", "UnionBy", "Intersect", "IntersectBy", "Except", "ExceptBy",
            "Zip", "Reverse", "Append", "Prepend",
            "DefaultIfEmpty");

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    /// <summary>Registers a compilation start action to resolve System.Linq.Enumerable.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterCompilationStartAction(OnCompilationStart);

    private static void OnCompilationStart(CompilationStartAnalysisContext context) {
        if (context.Compilation.GetTypeByMetadataName("System.Linq.Enumerable") is not { } enumerableType) {
            return;
        }

        context.RegisterOperationAction(
            ctx => AnalyzeInvocation(ctx, enumerableType),
            OperationKind.Invocation);
    }

    private static void AnalyzeInvocation(OperationAnalysisContext context, INamedTypeSymbol enumerableType) {
        var invocation = (IInvocationOperation)context.Operation;
        var method = invocation.TargetMethod;

        // Must be ToList() or ToArray()
        if (!MaterializationMethods.Contains(method.Name)) {
            return;
        }

        // Must be from System.Linq.Enumerable
        if (!method.ContainingType.IsEqualTo(enumerableType)) {
            return;
        }

        // Get the source argument (first argument of the extension method)
        if (!TryGetSourceInvocation(invocation, out var sourceMethod)) {
            return;
        }

        // The source must also be a LINQ operator from System.Linq.Enumerable
        if (!LinqOperators.Contains(sourceMethod.Name)) {
            return;
        }

        if (!sourceMethod.ContainingType.IsEqualTo(enumerableType)) {
            return;
        }

        // Skip when the materialized result is boxed to System.Object (e.g. stored in
        // Dictionary<,object?>, passed as object?, assigned to an object? field). Consumers
        // that receive object have no way to know the value is lazy, and common paths
        // (JSON serializers, diagnostics, logging) re-enumerate, which re-allocates every
        // projected element. Materialization is the correct choice in that context.
        if (IsBoxedToObject(invocation)) {
            return;
        }

        // Report on the materialization method name location
        var location = GetMethodNameLocation(invocation);
        context.ReportDiagnostic(Diagnostic.Create(Rule, location, method.Name, sourceMethod.Name));
    }

    private static bool IsBoxedToObject(IInvocationOperation materialization) {
        var parent = materialization.Parent;
        while (parent is IParenthesizedOperation parenthesized) {
            parent = parenthesized.Parent;
        }

        return parent is IConversionOperation conversion
            && conversion.Type?.SpecialType == SpecialType.System_Object;
    }

    private static bool TryGetSourceInvocation(
        IInvocationOperation materialization,
        out IMethodSymbol sourceMethod) {
        sourceMethod = null!;

        // For reduced extension methods (instance-style), the receiver is on Instance
        if (materialization.Instance is IInvocationOperation instanceInvocation) {
            sourceMethod = instanceInvocation.TargetMethod;
            return true;
        }

        switch (materialization.Arguments.Length)
        {
            // For non-reduced extension methods (static-style), the source is the first argument
            case > 0 when
                materialization.Arguments[0].Value is IInvocationOperation argInvocation:
                sourceMethod = argInvocation.TargetMethod;
                return true;
            // Unwrap conversions on the first argument
            case > 0 when
                UnwrapConversions(materialization.Arguments[0].Value) is IInvocationOperation unwrappedInvocation:
                sourceMethod = unwrappedInvocation.TargetMethod;
                return true;
            default:
                return false;
        }
    }

    private static IOperation UnwrapConversions(IOperation operation) {
        while (operation is IConversionOperation conversion) {
            operation = conversion.Operand;
        }

        return operation;
    }

    private static Location GetMethodNameLocation(IInvocationOperation invocation) {
        // Try to find the method name token in the syntax for a precise location
        return invocation.Syntax is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax memberAccess } ? memberAccess.Name.GetLocation() : invocation.Syntax.GetLocation();
    }
}
