using MsOperationExtensions = Microsoft.CodeAnalysis.Operations.OperationExtensions;

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
///         This analyzer suppresses false positives when materialization is load-bearing:
///         <list type="bullet">
///             <item>Result stored in a local that is read ≥ 2 times (multi-enumeration).</item>
///             <item>Result passed to an argument whose parameter type is a concrete collection
///                 (<c>T[]</c>, <c>List&lt;T&gt;</c>, <c>IList&lt;T&gt;</c>, <c>IReadOnlyList&lt;T&gt;</c>,
///                 <c>ICollection&lt;T&gt;</c>, <c>IReadOnlyCollection&lt;T&gt;</c>).</item>
///             <item>Result returned from a method whose return type is a concrete collection.</item>
///             <item>Result boxed to <c>System.Object</c> (original behavior).</item>
///         </list>
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

    private static readonly string[] StrictCollectionMetadataNames = [
        "System.Collections.Generic.List`1",
        "System.Collections.Generic.IList`1",
        "System.Collections.Generic.IReadOnlyList`1",
        "System.Collections.Generic.ICollection`1",
        "System.Collections.Generic.IReadOnlyCollection`1",
        "System.Collections.Generic.HashSet`1",
        "System.Collections.Generic.IReadOnlySet`1",
        "System.Collections.Immutable.ImmutableArray`1",
        "System.Collections.Immutable.ImmutableList`1",
        "System.Collections.Immutable.IImmutableList`1",
        "System.Collections.Immutable.ImmutableHashSet`1",
        "System.Collections.Frozen.FrozenSet`1"
    ];

    private static void OnCompilationStart(CompilationStartAnalysisContext context) {
        if (context.Compilation.GetTypeByMetadataName("System.Linq.Enumerable") is not { } enumerableType) {
            return;
        }

        var strict = StrictCollectionMetadataNames
            .Select(name => context.Compilation.GetTypeByMetadataName(name))
            .OfType<INamedTypeSymbol>()
            .ToImmutableArray();

        context.RegisterOperationAction(
            ctx => AnalyzeInvocation(ctx, enumerableType, strict),
            OperationKind.Invocation);
    }

    private static void AnalyzeInvocation(
        OperationAnalysisContext context,
        INamedTypeSymbol enumerableType,
        ImmutableArray<INamedTypeSymbol> strictCollectionTypes) {
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

        // Skip when the materialized result is boxed to System.Object.
        if (IsBoxedToObject(invocation)) {
            return;
        }

        // Skip when the materialization is load-bearing (multi-use local, concrete-collection
        // parameter, or concrete-collection return type).
        if (IsMaterializationLoadBearing(invocation, context.ContainingSymbol, strictCollectionTypes)) {
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

    private static bool IsMaterializationLoadBearing(
        IInvocationOperation materialization,
        ISymbol? containingSymbol,
        ImmutableArray<INamedTypeSymbol> strictCollectionTypes) {
        var parent = materialization.Parent;
        while (parent is IParenthesizedOperation parenthesized) {
            parent = parenthesized.Parent;
        }

        // Strip away identity/implicit conversions that don't change the semantic type
        // (e.g., List<T> -> List<T> nullable-annotation conversions). Keep conversions
        // that widen (e.g., T[] -> IEnumerable<T>) because those indicate the consumer
        // really wants the lazy type.
        if (parent is IConversionOperation conversion
            && conversion.IsImplicit
            && conversion.Type is { } convType
            && IsStrictCollectionType(convType, strictCollectionTypes)) {
            return true;
        }

        // Argument to a method: check if parameter type is a concrete collection.
        if (parent is IArgumentOperation argument
            && argument.Parameter?.Type is { } paramType
            && IsStrictCollectionType(paramType, strictCollectionTypes)) {
            return true;
        }

        // Return statement: check enclosing method's return type.
        if (parent is IReturnOperation
            && containingSymbol is IMethodSymbol enclosingMethod
            && IsStrictCollectionType(UnwrapTask(enclosingMethod.ReturnType), strictCollectionTypes)) {
            return true;
        }

        // Local variable initializer: suppress if the local is read more than once.
        if (parent is IVariableInitializerOperation { Parent: IVariableDeclaratorOperation declarator }
            && CountLocalReads(materialization, declarator.Symbol) >= 2) {
            return true;
        }

        return false;
    }

    private static int CountLocalReads(IOperation materialization, ILocalSymbol local) {
        // Walk up to the root of the operation tree (method body / field initializer / etc.)
        var root = materialization;
        while (root.Parent is not null) {
            root = root.Parent;
        }

        var count = 0;
        foreach (var descendant in MsOperationExtensions.DescendantsAndSelf(root)) {
            if (descendant is ILocalReferenceOperation localRef
                && SymbolEqualityComparer.Default.Equals(localRef.Local, local)) {
                count++;
            }
        }

        return count;
    }

    private static ITypeSymbol UnwrapTask(ITypeSymbol type) {
        // Treat Task<T> / ValueTask<T> / IAsyncEnumerable<T> returns against their T payload.
        if (type is INamedTypeSymbol { IsGenericType: true } named
            && named.TypeArguments.Length == 1
            && named.ConstructedFrom.ToDisplayString() is
                "System.Threading.Tasks.Task<TResult>" or
                "System.Threading.Tasks.ValueTask<TResult>") {
            return named.TypeArguments[0];
        }

        return type;
    }

    private static bool IsStrictCollectionType(
        ITypeSymbol? type,
        ImmutableArray<INamedTypeSymbol> strictCollectionTypes) {
        if (type is null) {
            return false;
        }

        if (type is IArrayTypeSymbol) {
            return true;
        }

        if (type is not INamedTypeSymbol named || !named.IsGenericType) {
            return false;
        }

        var definition = named.OriginalDefinition;
        foreach (var strict in strictCollectionTypes) {
            if (SymbolEqualityComparer.Default.Equals(definition, strict)) {
                return true;
            }
        }

        return false;
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
