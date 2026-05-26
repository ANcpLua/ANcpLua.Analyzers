using System.Collections.Concurrent;
using ANcpLua.Roslyn.Utilities.Matching;

namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL1500: Exhaustive matching for closed type hierarchies.
/// </summary>
/// <remarks>
///     <para>
///         A closed hierarchy is an <c>abstract</c> class/record where all concrete descendants are sealed.
///         This analyzer validates both <c>switch</c> constructs and generic <c>Match&lt;T&gt;</c> calls.
///     </para>
///     <para>
///         Guarded patterns (<c>when</c>) are treated as conditional and do not count toward exhaustiveness.
///     </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al1500ClosedTypeHierarchySwitchAnalyzer : AlAnalyzer {
    /// <summary>AL1500: Exhaustive matching for closed type hierarchies.</summary>
    public const string DiagnosticId = "AL1500";

    /// <summary>Diagnostic property key for pipe-separated missing type names.</summary>
    public const string MissingTypesProperty = "MissingTypes";

    static readonly DiagnosticDescriptor s_rule = new(
        DiagnosticId,
        "Closed hierarchy match is not exhaustive",
        "Closed hierarchy '{0}' does not handle: {1}",
        DiagnosticCategories.Design,
        DiagnosticSeverity.Warning,
        true,
        "All sealed subtypes of a closed hierarchy should be explicitly handled in switch expressions, " +
        "switch statements, and Match<T> calls.",
        RuleDocs.HelpLinkAuto(DiagnosticId));

    static readonly InvocationMatcher s_matchInvocation = Invoke.Method("Match").Generic();

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_rule];

    /// <inheritdoc />
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterCompilationStartAction(OnCompilationStart);

    static void OnCompilationStart(CompilationStartAnalysisContext context) {
        var compilation = context.Compilation;
        var cache = new ConcurrentDictionary<string, ImmutableArray<INamedTypeSymbol>>(StringComparer.Ordinal);

        context.RegisterOperationAction(
            ctx => AnalyzeSwitch(ctx, cache, compilation),
            OperationKind.SwitchExpression,
            OperationKind.Switch);

        context.RegisterOperationAction(
            ctx => AnalyzeMatchInvocation(ctx, cache, compilation),
            OperationKind.Invocation);
    }

    static void AnalyzeSwitch(
        OperationAnalysisContext context,
        ConcurrentDictionary<string, ImmutableArray<INamedTypeSymbol>> cache,
        Compilation compilation) {
        if (GetSwitchedType(context.Operation) is not { } switchedType) {
            return;
        }

        var sealedSubtypes = GetSealedSubtypes(switchedType, cache, compilation);
        if (sealedSubtypes.IsDefaultOrEmpty) {
            return;
        }

        var handledTypes = CollectHandledTypes(context.Operation);
        ReportMissingCases(
            context,
            GetSwitchLocation(context.Operation),
            switchedType,
            sealedSubtypes,
            handledTypes);
    }

    static void AnalyzeMatchInvocation(
        OperationAnalysisContext context,
        ConcurrentDictionary<string, ImmutableArray<INamedTypeSymbol>> cache,
        Compilation compilation) {
        if (context.Operation is not IInvocationOperation invocation ||
            !s_matchInvocation.Matches(invocation)) {
            return;
        }

        if (GetMatchedType(invocation) is not { } matchedType) {
            return;
        }

        var sealedSubtypes = GetSealedSubtypes(matchedType, cache, compilation);
        if (sealedSubtypes.IsDefaultOrEmpty) {
            return;
        }

        var handledTypes = CollectHandledTypes(invocation);
        if (!HasSubtypeSpecificHandler(handledTypes, matchedType)) {
            return;
        }

        ReportMissingCases(
            context,
            invocation.Syntax.GetLocation(),
            matchedType,
            sealedSubtypes,
            handledTypes);
    }

    static void ReportMissingCases(
        OperationAnalysisContext context,
        Location location,
        INamedTypeSymbol rootType,
        ImmutableArray<INamedTypeSymbol> requiredTypes,
        IReadOnlyCollection<INamedTypeSymbol> handledTypes) {
        var missing = requiredTypes
            .Where(required => !ContainsType(handledTypes, required))
            .ToImmutableArray();

        if (missing.IsEmpty) {
            return;
        }

        var displayFormat = SymbolDisplayFormat.MinimallyQualifiedFormat;
        var fullyQualifiedFormat = new SymbolDisplayFormat(
            globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);

        var missingNames = string.Join(", ", missing.Select(type => type.ToDisplayString(displayFormat)));
        var missingFqNames = string.Join("|", missing.Select(type => type.ToDisplayString(fullyQualifiedFormat)));

        var properties = ImmutableDictionary.CreateRange([
            new KeyValuePair<string, string?>(MissingTypesProperty, missingFqNames)
        ]);

        context.ReportDiagnostic(Diagnostic.Create(
            s_rule,
            location,
            properties,
            rootType.ToDisplayString(displayFormat),
            missingNames));
    }

    static ImmutableArray<INamedTypeSymbol> GetSealedSubtypes(
        INamedTypeSymbol rootType,
        ConcurrentDictionary<string, ImmutableArray<INamedTypeSymbol>> cache,
        Compilation compilation) {
        if (!IsSupportedHierarchyRoot(rootType)) {
            return ImmutableArray<INamedTypeSymbol>.Empty;
        }

        var cacheKey = GetTypeKey(rootType);
        return cache.GetOrAdd(cacheKey, _ => FindSealedSubtypes(rootType, compilation));
    }

    static bool IsSupportedHierarchyRoot(INamedTypeSymbol type) =>
        type.Locations.Any(static location => location.IsInSource) &&
        type is {
            TypeKind: TypeKind.Class,
            IsAbstract: true,
            IsSealed: false,
            IsGenericType: false
        };

    static INamedTypeSymbol? GetSwitchedType(IOperation operation) {
        var type = operation switch {
            ISwitchExpressionOperation expression => expression.Value.Type,
            ISwitchOperation statement => statement.Value.Type,
            _ => null
        };

        return type as INamedTypeSymbol;
    }

    static INamedTypeSymbol? GetMatchedType(IInvocationOperation invocation) {
        if (invocation.Instance?.Type is INamedTypeSymbol instanceType) {
            return instanceType;
        }

        if (!invocation.TargetMethod.IsExtensionMethod) {
            return null;
        }

        foreach (var argument in invocation.Arguments) {
            if (argument is { Parameter: { IsThis: true }, Value.Type: INamedTypeSymbol thisType }) {
                return thisType;
            }
        }

        return null;
    }

    static ImmutableArray<INamedTypeSymbol> FindSealedSubtypes(
        INamedTypeSymbol abstractBase,
        Compilation compilation) {
        var allTypes = new List<INamedTypeSymbol>();
        CollectAllTypes(abstractBase.ContainingAssembly.GlobalNamespace, allTypes);

        if (!compilation.Assembly.IsEqualTo(abstractBase.ContainingAssembly)) {
            CollectAllTypes(compilation.Assembly.GlobalNamespace, allTypes);
        }

        var derivedByBaseType = BuildDerivedTypeMap(allTypes);
        var baseKey = GetTypeKey(abstractBase);
        if (!derivedByBaseType.TryGetValue(baseKey, out var directSubtypes) ||
            directSubtypes.Count is 0) {
            return ImmutableArray<INamedTypeSymbol>.Empty;
        }

        var sealedDescendants = new List<INamedTypeSymbol>();
        var pending = new Stack<INamedTypeSymbol>(directSubtypes);
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var isClosed = true;

        while (pending.Count > 0) {
            var current = pending.Pop();
            var currentKey = GetTypeKey(current);
            if (!visited.Add(currentKey)) {
                continue;
            }

            if (current.TypeKind is not TypeKind.Class) {
                continue;
            }

            if (current.IsAbstract) {
                if (!derivedByBaseType.TryGetValue(currentKey, out var nestedSubtypes) ||
                    nestedSubtypes.Count is 0) {
                    isClosed = false;
                    break;
                }

                foreach (var nestedSubtype in nestedSubtypes) {
                    pending.Push(nestedSubtype);
                }

                continue;
            }

            if (!current.IsSealed) {
                isClosed = false;
                break;
            }

            AddType(current, sealedDescendants);
        }

        if (!isClosed || sealedDescendants.Count < 2) {
            return ImmutableArray<INamedTypeSymbol>.Empty;
        }

        sealedDescendants.Sort(static (left, right) => {
            var byName = StringComparer.Ordinal.Compare(left.Name, right.Name);
            return byName is not 0
                ? byName
                : StringComparer.Ordinal.Compare(left.GetFullyQualifiedName(), right.GetFullyQualifiedName());
        });

        return [..sealedDescendants];
    }

    static Dictionary<string, List<INamedTypeSymbol>> BuildDerivedTypeMap(IEnumerable<INamedTypeSymbol> allTypes) {
        var map = new Dictionary<string, List<INamedTypeSymbol>>(StringComparer.Ordinal);

        foreach (var type in allTypes) {
            if (type.BaseType is not { } baseType) {
                continue;
            }

            var key = GetTypeKey(baseType);
            if (!map.TryGetValue(key, out var derivedTypes)) {
                derivedTypes = [];
                map[key] = derivedTypes;
            }

            AddType(type, derivedTypes);
        }

        return map;
    }

    static string GetTypeKey(INamedTypeSymbol type) =>
        $"{type.ContainingAssembly.Identity}|{type.GetFullyQualifiedName()}";

    static void CollectAllTypes(INamespaceSymbol ns, ICollection<INamedTypeSymbol> types) {
        foreach (var member in ns.GetMembers()) {
            switch (member) {
                case INamedTypeSymbol type:
                    types.Add(type);
                    CollectNestedTypes(type, types);
                    break;

                case INamespaceSymbol child:
                    CollectAllTypes(child, types);
                    break;
            }
        }
    }

    static void CollectNestedTypes(INamedTypeSymbol type, ICollection<INamedTypeSymbol> types) {
        foreach (var nested in type.GetTypeMembers()) {
            types.Add(nested);
            CollectNestedTypes(nested, types);
        }
    }

    static ImmutableArray<INamedTypeSymbol> CollectHandledTypes(IOperation operation) {
        var types = new List<INamedTypeSymbol>();

        switch (operation) {
            case ISwitchExpressionOperation expression:
                foreach (var arm in expression.Arms) {
                    if (arm.Guard is not null) {
                        continue;
                    }

                    CollectTypesFromPattern(arm.Pattern, types);
                }

                break;

            case ISwitchOperation statement:
                foreach (var switchCase in statement.Cases) {
                    foreach (var clause in switchCase.Clauses) {
                        if (clause is IPatternCaseClauseOperation { Guard: null } patternClause) {
                            CollectTypesFromPattern(patternClause.Pattern, types);
                        }
                    }
                }

                break;

            case IInvocationOperation invocation:
                foreach (var argument in invocation.Arguments) {
                    if (argument.Parameter is { IsThis: true }) {
                        continue;
                    }

                    if (TryGetHandledMatchType(argument.Parameter?.Type, out var handledType) &&
                        handledType is not null) {
                        AddType(handledType, types);
                    }
                }

                break;
        }

        return [..types];
    }

    static bool TryGetHandledMatchType(ITypeSymbol? delegateType, out INamedTypeSymbol? handledType) {
        handledType = null;

        if (delegateType is not INamedTypeSymbol { DelegateInvokeMethod: { Parameters.Length: > 0 } invokeMethod } ||
            invokeMethod.Parameters[0].Type is not INamedTypeSymbol matchType) {
            return false;
        }

        handledType = matchType;
        return true;
    }

    static void CollectTypesFromPattern(IPatternOperation pattern, ICollection<INamedTypeSymbol> types) {
        while (true) {
            switch (pattern) {
                case IBinaryPatternOperation binary:
                    CollectTypesFromPattern(binary.LeftPattern, types);
                    pattern = binary.RightPattern;
                    continue;

                case INegatedPatternOperation:
                case IDiscardPatternOperation:
                    break;

                default:
                    if (pattern.NarrowedType is INamedTypeSymbol narrowed) {
                        AddType(narrowed, types);
                    }

                    break;
            }

            break;
        }
    }

    static void AddType(INamedTypeSymbol type, ICollection<INamedTypeSymbol> types) {
        if (ContainsType(types, type)) {
            return;
        }

        types.Add(type);
    }

    static bool ContainsType(IEnumerable<INamedTypeSymbol> types, ISymbol expected) {
        foreach (var type in types) {
            if (AreEquivalentForCoverage(type, expected)) {
                return true;
            }
        }

        return false;
    }

    static bool AreEquivalentForCoverage(ISymbol left, ISymbol right) =>
        left.IsEqualTo(right) ||
        left.OriginalDefinition.IsEqualTo(right.OriginalDefinition);

    static bool HasSubtypeSpecificHandler(
        IEnumerable<INamedTypeSymbol> handledTypes,
        ITypeSymbol rootType) {
        foreach (var handledType in handledTypes) {
            if (!handledType.IsEqualTo(rootType) &&
                handledType.InheritsFrom(rootType)) {
                return true;
            }
        }

        return false;
    }

    static Location GetSwitchLocation(IOperation operation) =>
        operation.Syntax switch {
            SwitchExpressionSyntax switchExpression => switchExpression.SwitchKeyword.GetLocation(),
            SwitchStatementSyntax switchStatement => switchStatement.SwitchKeyword.GetLocation(),
            _ => operation.Syntax.GetLocation()
        };
}
