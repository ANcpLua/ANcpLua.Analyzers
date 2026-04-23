
namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0032: Suggests using OrEmpty() extension instead of null-coalescing with empty collections.
/// </summary>
/// <remarks>
///     <list type="bullet">
///         <item><c>collection ?? Array.Empty&lt;T&gt;()</c> → <c>collection.OrEmpty()</c></item>
///         <item><c>collection ?? Enumerable.Empty&lt;T&gt;()</c> → <c>collection.OrEmpty()</c></item>
///         <item><c>collection ?? []</c> → <c>collection.OrEmpty()</c></item>
///     </list>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0032UseOrEmptyAnalyzer : AlAnalyzer {
    /// <summary>The diagnostic identifier for AL0032.</summary>
    public const string DiagnosticId = "AL0032";

    private static readonly DiagnosticDescriptor Rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.RoslynUtilities,
        DiagnosticSeverity.Info);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    /// <summary>Registers syntax or operation actions for analysis.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterOperationAction(AnalyzeCoalesce, OperationKind.Coalesce);

    private static void AnalyzeCoalesce(OperationAnalysisContext context) {
        if (context.Operation is not ICoalesceOperation coalesce) {
            return;
        }

        if (!HasOrEmptyExtension(context.Compilation)) {
            return;
        }

        if (!IsEmptyCollectionExpression(coalesce.WhenNull)) {
            return;
        }

        // OrEmpty() returns IEnumerable<T>, so only fire when the result type matches.
        // Concrete types (Dictionary, List, string[], etc.) would lose type information.
        var resultType = coalesce.Type;
        if (resultType is null || resultType.SpecialType == SpecialType.System_String) {
            return;
        }

        if (!IsEnumerableType(resultType)) {
            return;
        }

        var leftName = GetOperandDisplayName(coalesce.Value);
        context.ReportDiagnostic(Diagnostic.Create(Rule, coalesce.Syntax.GetLocation(),
            $"{leftName}.OrEmpty()", "null-coalescing with empty collection"));
    }

    private static bool IsEmptyCollectionExpression(IOperation? operation) {
        if (operation is null) {
            return false;
        }

        operation = operation.UnwrapAllConversions();

        switch (operation) {
            case ICollectionExpressionOperation { Elements.Length: 0 }:
                return true;
            case IInvocationOperation { TargetMethod: { Name: "Empty", IsStatic: true, Parameters.Length: 0, ContainingType: { } containingType } }
                when (containingType.ContainingNamespace?.ToDisplayString() is { } ns
                      && (containingType.Name == "Array" && ns == "System" ||
                          containingType.Name == "Enumerable" && ns == "System.Linq")):
                return true;
            case IArrayCreationOperation {
                DimensionSizes: [{ ConstantValue: { HasValue: true, Value: 0 } }]
            }:
            case IArrayCreationOperation {
                Initializer.ElementValues.Length: 0
            }:
                return true;
            default:
                return false;
        }
    }

    private static bool IsEnumerableType(ITypeSymbol type) =>
        type is INamedTypeSymbol { IsGenericType: true } named
        && named.OriginalDefinition.ToDisplayString() == "System.Collections.Generic.IEnumerable<T>";

    private static bool HasOrEmptyExtension(Compilation compilation) {
        foreach (var reference in compilation.References) {
            if (compilation.GetAssemblyOrModuleSymbol(reference) is IAssemblySymbol assembly
                && HasOrEmptyInAssembly(assembly)) {
                return true;
            }
        }

        return HasOrEmptyInAssembly(compilation.Assembly);
    }

    private static bool HasOrEmptyInAssembly(IAssemblySymbol assembly) {
        var stack = new Stack<INamespaceSymbol>();
        stack.Push(assembly.GlobalNamespace);

        while (stack.Count > 0) {
            var ns = stack.Pop();
            foreach (var type in ns.GetTypeMembers()) {
                if (!type.IsStatic || !type.MightContainExtensionMethods) {
                    continue;
                }

                foreach (var member in type.GetMembers("OrEmpty")) {
                    if (member is IMethodSymbol { IsExtensionMethod: true, Parameters.Length: 1 } method
                        && method.Parameters[0].Type.OriginalDefinition.ToDisplayString()
                            == "System.Collections.Generic.IEnumerable<T>") {
                        return true;
                    }
                }
            }

            foreach (var child in ns.GetNamespaceMembers()) {
                stack.Push(child);
            }
        }

        return false;
    }

    private static string GetOperandDisplayName(IOperation operation) =>
        operation.GetOperandName("collection");
}
