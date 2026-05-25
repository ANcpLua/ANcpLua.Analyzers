using MsOperationExtensions = Microsoft.CodeAnalysis.Operations.OperationExtensions;

namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL1311: Flags unnecessary ToList()/ToArray() calls immediately after LINQ operators.
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
public sealed partial class Al1311UnnecessaryLinqMaterializationAnalyzer : AlAnalyzer {
    /// <summary>The diagnostic identifier for AL1311.</summary>
    private const string DiagnosticId = "AL1311";

    private static readonly DiagnosticDescriptor s_rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.Usage,
        DiagnosticSeverities.HiddenByDefault);

    private static readonly ImmutableHashSet<string> s_materializationMethods =
        ImmutableHashSet.Create(StringComparer.Ordinal, "ToList", "ToArray");

    private static readonly ImmutableHashSet<string> s_linqOperators =
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

    private static readonly ImmutableHashSet<string> s_sourceMutationMethods =
        ImmutableHashSet.Create(StringComparer.Ordinal, "Clear", "Add", "AddRange", "Insert", "InsertRange",
            "Remove", "RemoveAt", "RemoveRange", "RemoveAll", "TrimExcess");

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_rule];

    /// <summary>Registers a compilation start action to resolve System.Linq.Enumerable.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterCompilationStartAction(OnCompilationStart);

    private static readonly string[] s_strictCollectionMetadataNames = [
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

        var strict = s_strictCollectionMetadataNames
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
        if (!s_materializationMethods.Contains(method.Name)) {
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
        if (!s_linqOperators.Contains(sourceMethod.TargetMethod.Name)) {
            return;
        }

        if (!sourceMethod.TargetMethod.ContainingType.IsEqualTo(enumerableType)) {
            return;
        }

        // Skip when the materialized result is boxed to System.Object.
        if (IsBoxedToObject(invocation)) {
            return;
        }

        // Skip when the materialization is load-bearing (multi-use local, concrete-collection
        // parameter, or concrete-collection return type).
        if (IsMaterializationLoadBearing(invocation, sourceMethod, context.ContainingSymbol, strictCollectionTypes)) {
            return;
        }

        // Report on the materialization method name location
        var location = GetMethodNameLocation(invocation);
        context.ReportDiagnostic(Diagnostic.Create(s_rule, location, method.Name, sourceMethod.TargetMethod.Name));
    }

    private static bool IsBoxedToObject(IInvocationOperation materialization) {
        var parent = materialization.Parent;
        while (parent is IParenthesizedOperation parenthesized) {
            parent = parenthesized.Parent;
        }

        return parent is IConversionOperation { Type.SpecialType: SpecialType.System_Object };
    }

    private static bool IsMaterializationLoadBearing(
        IInvocationOperation materialization,
        IInvocationOperation sourceMethod,
        ISymbol? containingSymbol,
        ImmutableArray<INamedTypeSymbol> strictCollectionTypes) {
        var parent = materialization.Parent;
        while (parent is IParenthesizedOperation parenthesized) {
            parent = parenthesized.Parent;
        }

        switch (parent)
        {
            // Strip away identity/implicit conversions that don't change the semantic type
            // (e.g., List<T> -> List<T> nullable-annotation conversions). Keep conversions
            // that widen (e.g., T[] -> IEnumerable<T>) because those indicate the consumer
            // really wants the lazy type.
            case IConversionOperation { IsImplicit: true, Type: { } convType } when IsStrictCollectionType(convType, strictCollectionTypes):
            // Argument to a method: check if parameter type is a concrete collection.
            case IArgumentOperation { Parameter.Type: { } paramType } when IsStrictCollectionType(paramType, strictCollectionTypes):
            // Return statement: check enclosing method's return type.
            case IReturnOperation
                when containingSymbol is IMethodSymbol enclosingMethod
                     && IsStrictCollectionType(UnwrapTask(enclosingMethod.ReturnType), strictCollectionTypes):
            // Local variable initializer: suppress if the local is read more than once,
            // or if the single read consumes it in a context that requires a concrete collection.
            case IVariableInitializerOperation { Parent: IVariableDeclaratorOperation declarator }
                when CountLocalReads(materialization, declarator.Symbol) >= 2
                     || IsSourceMutatedAfterMaterialization(materialization, declarator.Symbol, sourceMethod)
                     || IsSingleReadInConcreteCollectionContext(materialization, declarator.Symbol, strictCollectionTypes):
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    ///     Returns true when the local is read exactly once and that read site passes it as an
    ///     argument to a method or constructor whose parameter requires a concrete collection
    ///     (e.g. <c>var arr = xs.Select(…).ToArray(); Foo(arr)</c> where <c>Foo</c> accepts
    ///     <c>IReadOnlyList&lt;T&gt;</c>).  Without this check the analyzer fires even though
    ///     removing <c>ToArray()</c> would not compile.
    ///     <para>
    ///         Deliberately restricted to <see cref="IArgumentOperation"/> as the final
    ///         consumer so that <c>foreach</c> over a <c>List&lt;T&gt;</c> (which Roslyn
    ///         models with an implicit <c>List&lt;T&gt;</c> conversion on the collection
    ///         expression) does not accidentally suppress the diagnostic.
    ///     </para>
    /// </summary>
    private static bool IsSingleReadInConcreteCollectionContext(
        IOperation materialization,
        ILocalSymbol local,
        ImmutableArray<INamedTypeSymbol> strictCollectionTypes) {
        var root = materialization;
        while (root.Parent is not null) {
            root = root.Parent;
        }

        ILocalReferenceOperation? singleRead = null;
        foreach (var descendant in MsOperationExtensions.DescendantsAndSelf(root)) {
            if (descendant is ILocalReferenceOperation localRef
                && SymbolEqualityComparer.Default.Equals(localRef.Local, local)) {
                if (singleRead is not null) {
                    return false; // more than one read — handled by CountLocalReads >= 2
                }

                singleRead = localRef;
            }
        }

        if (singleRead is null) {
            return false;
        }

        // Walk up through parentheses and at most one implicit conversion to reach the
        // argument context.  Checking only IArgumentOperation (not the conversion target
        // directly) prevents foreach-on-List<T> from being a false negative: Roslyn emits
        // an implicit List<T>→List<T> conversion on foreach collection expressions, which
        // would match IsStrictCollectionType if we checked convType alone.
        var parent = singleRead.Parent;
        while (parent is IParenthesizedOperation paren) {
            parent = paren.Parent;
        }

        if (parent is IConversionOperation { IsImplicit: true }) {
            parent = parent.Parent;
            while (parent is IParenthesizedOperation paren2) {
                parent = paren2.Parent;
            }
        }

        return parent is IArgumentOperation { Parameter.Type: { } paramType }
               && IsStrictCollectionType(paramType, strictCollectionTypes);
    }

    private static bool IsSourceMutatedAfterMaterialization(
        IInvocationOperation materialization,
        ILocalSymbol materializedLocal,
        IInvocationOperation sourceMethod) {
        var sourceArgument = sourceMethod.Arguments.Length > 0
            ? sourceMethod.Arguments[0].Value
            : null;

        if (TryGetSourceMutationSymbol(sourceArgument) is not { } sourceSymbol) {
            return false;
        }

        // Find the single read site for the local variable and only suppress when that read is
        // after a source mutation.
        var (readSiteSyntax, materializationSpanEnd) = GetReadSiteAndMaterializationSpan(materializedLocal, materialization);
        if (readSiteSyntax is null) {
            return false;
        }

        IOperation root = materialization;
        while (root.Parent is not null) {
            root = root.Parent;
        }

        foreach (var op in MsOperationExtensions.DescendantsAndSelf(root)) {
            if (op == materialization) {
                continue;
            }

            if (IsMutatingInvocation(op, sourceSymbol) is { } location
                && location > materializationSpanEnd
                && location < readSiteSyntax.SpanStart) {
                return true;
            }

            if (IsSourceAssignment(op, sourceSymbol) is { } assignmentLocation
                && assignmentLocation > materializationSpanEnd
                && assignmentLocation < readSiteSyntax.SpanStart) {
                return true;
            }
        }

        return false;
    }

    private static ISymbol? TryGetSourceMutationSymbol(IOperation? sourceArgument) {
        return sourceArgument switch {
            ILocalReferenceOperation { Local: { } local } => local,
            IParameterReferenceOperation { Parameter: { } parameter } => parameter,
            IConversionOperation { Operand: ILocalReferenceOperation { Local: { } local } } => local,
            IConversionOperation { Operand: IParameterReferenceOperation { Parameter: { } parameter } } => parameter,
            _ => null,
        };
    }

    private static (SyntaxNode? readSite, int materializationSpanEnd) GetReadSiteAndMaterializationSpan(
        ILocalSymbol local,
        IInvocationOperation materialization) {
        IOperation root = materialization;
        while (root.Parent is not null) {
            root = root.Parent;
        }

        var read = (SyntaxNode?)null;
        foreach (var desc in MsOperationExtensions.DescendantsAndSelf(root)) {
            if (desc is ILocalReferenceOperation localRef
                && SymbolEqualityComparer.Default.Equals(localRef.Local, local)) {
                read = localRef.Syntax;
                break;
            }
        }

        return (read, materialization.Syntax.Span.End);
    }

    private static int? IsMutatingInvocation(IOperation operation, ISymbol sourceSymbol) {
        if (operation is not IInvocationOperation invocation) {
            return null;
        }

        if (!s_sourceMutationMethods.Contains(invocation.TargetMethod.Name)) {
            return null;
        }

        return invocation.Instance switch {
            ILocalReferenceOperation { Local: { } local } when SymbolEqualityComparer.Default.Equals(local, sourceSymbol) =>
                invocation.Syntax.SpanStart,
            IParameterReferenceOperation { Parameter: { } parameter }
                when SymbolEqualityComparer.Default.Equals(parameter, sourceSymbol) =>
                invocation.Syntax.Span.Start,
            IConversionOperation { Operand: ILocalReferenceOperation { Local: { } local } }
                when invocation.TargetMethod.IsExtensionMethod is false
                     && SymbolEqualityComparer.Default.Equals(local, sourceSymbol)
                => invocation.Syntax.Span.Start,
            IConversionOperation { Operand: IParameterReferenceOperation { Parameter: { } parameter } }
                when SymbolEqualityComparer.Default.Equals(parameter, sourceSymbol)
                => invocation.Syntax.Span.Start,
            _ => null,
        };
    }

    private static int? IsSourceAssignment(IOperation operation, ISymbol sourceSymbol) {
        if (operation is not IAssignmentOperation assignment) {
            return null;
        }

        if (assignment.Target is ILocalReferenceOperation { Local: { } local }
            && SymbolEqualityComparer.Default.Equals(local, sourceSymbol)) {
            return assignment.Syntax.Span.Start;
        }

        if (assignment.Target is IParameterReferenceOperation { Parameter: { } parameter }
            && SymbolEqualityComparer.Default.Equals(parameter, sourceSymbol)) {
            return assignment.Syntax.Span.Start;
        }

        return null;
    }

    private static int CountLocalReads(IOperation materialization, ILocalSymbol local) {
        // Walk up to the root of the operation tree (method body / field initializer / etc.)
        IOperation root = materialization;
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
        if (type is INamedTypeSymbol { IsGenericType: true, TypeArguments.Length: 1 } named
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
        switch (type)
        {
            case null:
                return false;
            case IArrayTypeSymbol:
                return true;
        }

        if (type is not INamedTypeSymbol { IsGenericType: true } named) {
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
        out IInvocationOperation sourceMethod) {
        sourceMethod = default!;

        // For reduced extension methods (instance-style), the receiver is on Instance
        if (materialization.Instance is IInvocationOperation instanceInvocation) {
                sourceMethod = instanceInvocation;
                return true;
        }

        switch (materialization.Arguments.Length)
        {
            // For non-reduced extension methods (static-style), the source is the first argument
            case > 0 when
                materialization.Arguments[0].Value is IInvocationOperation argInvocation:
                sourceMethod = argInvocation;
                return true;
            // Unwrap conversions on the first argument
            case > 0 when
                UnwrapConversions(materialization.Arguments[0].Value) is IInvocationOperation unwrappedInvocation:
                sourceMethod = unwrappedInvocation;
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
