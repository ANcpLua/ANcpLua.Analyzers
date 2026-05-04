using MsOperationExtensions = Microsoft.CodeAnalysis.Operations.OperationExtensions;

namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0030: Suggests using type hierarchy extensions instead of manual loops.
/// </summary>
/// <remarks>
///     <list type="bullet">
///         <item>
///             <c>foreach (var iface in type.AllInterfaces) if (Equals(iface, target))</c> →
///             <c>type.Implements(target)</c>
///         </item>
///         <item>
///             <c>while (baseType != null) { if (Equals(baseType, target)) ... baseType = baseType.BaseType; }</c> →
///             <c>type.InheritsFrom(target)</c>
///         </item>
///     </list>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0030UseTypeHierarchyAnalyzer : AlAnalyzer {
    /// <summary>The diagnostic identifier for AL0030.</summary>
    public const string DiagnosticId = "AL0030";

    private enum KnownType { ITypeSymbol, SymbolEqualityComparer }

    private static readonly string[] s_knownTypeNames = [
        "Microsoft.CodeAnalysis.ITypeSymbol",
        "Microsoft.CodeAnalysis.SymbolEqualityComparer"
    ];

    private static readonly DiagnosticDescriptor s_rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.RoslynUtilities,
        DiagnosticSeverity.Info);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_rule];

    /// <summary>Registers compilation start action to analyze type hierarchy iteration patterns.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterCompilationStartAction(OnCompilationStart);

    private static void OnCompilationStart(CompilationStartAnalysisContext context) {
        var cache = new TypeCache<KnownType>(type => context.Compilation.GetTypeByMetadataName(s_knownTypeNames[(int)type]));

        if (cache.Get(KnownType.ITypeSymbol) is null) {
            return;
        }

        context.RegisterOperationAction(
            ctx => AnalyzeLoop(ctx, cache),
            OperationKind.Loop);
    }

    private static void AnalyzeLoop(OperationAnalysisContext context, TypeCache<KnownType> cache) {
        if (context.Operation is IForEachLoopOperation forEachLoop) {
            var collectionName = forEachLoop.Collection.GetCollectionSourceName();

            if (collectionName is "AllInterfaces" &&
                ContainsSymbolEqualityComparison(forEachLoop.Body, cache)) {
                context.ReportDiagnostic(s_rule, forEachLoop.Syntax.GetLocation(),
                    "type.Implements(interfaceType)", "foreach over AllInterfaces");
                return;
            }
        }

        if (context.Operation is IWhileLoopOperation whileLoop &&
            IsBaseTypeWalkingLoop(whileLoop, cache)) {
            context.ReportDiagnostic(s_rule, whileLoop.Syntax.GetLocation(),
                "type.InheritsFrom(baseType)", "while loop walking BaseType");
        }
    }

    private static bool ContainsSymbolEqualityComparison(IOperation? body, TypeCache<KnownType> cache) {
        if (body is null) {
            return false;
        }

        foreach (var descendant in MsOperationExtensions.Descendants(body)) {
            if (descendant is not IInvocationOperation invocation) {
                continue;
            }

            if (IsSymbolEqualityComparerEquals(invocation, cache)) {
                return true;
            }

            if (invocation.TargetMethod is {
                Name: "IsEqualTo",
                IsExtensionMethod: true,
                Parameters.Length: 2
            }) {
                return true;
            }
        }

        return false;
    }

    private static bool IsBaseTypeWalkingLoop(IWhileLoopOperation whileLoop, TypeCache<KnownType> cache) {
        var hasBaseTypeAccess = false;
        var hasBaseTypeAssignment = false;
        var hasEqualityCheck = false;

        foreach (var operation in MsOperationExtensions.Descendants(whileLoop)) {
            switch (operation) {
                case IPropertyReferenceOperation { Property.Name: "BaseType" }:
                    hasBaseTypeAccess = true;
                    break;
                case ISimpleAssignmentOperation {
                    Value: IPropertyReferenceOperation { Property.Name: "BaseType" }
                }:
                    hasBaseTypeAssignment = true;
                    break;
                case IInvocationOperation invocation: {
                    if (IsSymbolEqualityComparerEquals(invocation, cache)) {
                        hasEqualityCheck = true;
                    }

                    if (invocation.TargetMethod is {
                        Name: "IsEqualTo",
                        IsExtensionMethod: true,
                        Parameters.Length: 2
                    }) {
                        hasEqualityCheck = true;
                    }

                    break;
                }
            }
        }

        return hasBaseTypeAccess && hasBaseTypeAssignment && hasEqualityCheck;
    }

    private static bool IsSymbolEqualityComparerEquals(IInvocationOperation invocation, TypeCache<KnownType> cache) {
        var method = invocation.TargetMethod;
        if (method.Name != "Equals" || method.Parameters.Length != 2) {
            return false;
        }

        return cache.IsType(method.ContainingType, KnownType.SymbolEqualityComparer);
    }
}
