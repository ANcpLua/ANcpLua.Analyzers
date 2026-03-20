
namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0033: Suggests using ToImmutableArrayOrEmpty() instead of null-conditional with fallback.
/// </summary>
/// <remarks>
///     <c>collection?.ToImmutableArray() ?? ImmutableArray&lt;T&gt;.Empty</c> →
///     <c>collection.ToImmutableArrayOrEmpty()</c>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0033UseToImmutableArrayOrEmptyAnalyzer : AlAnalyzer {
    /// <summary>The diagnostic identifier for AL0033.</summary>
    public const string DiagnosticId = "AL0033";

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

        if (!HasToImmutableArrayOrEmptyExtension(context.Compilation)) {
            return;
        }

        if (!IsImmutableArrayEmpty(coalesce.WhenNull)) {
            return;
        }

        if (!TryGetToImmutableArraySource(coalesce.Value, out var sourceName)) {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(Rule, coalesce.Syntax.GetLocation(),
            $"{sourceName}.ToImmutableArrayOrEmpty()", "?.ToImmutableArray() ?? ImmutableArray.Empty"));
    }

    private static bool IsImmutableArrayEmpty(IOperation? operation) {
        if (operation is null) {
            return false;
        }

        operation = operation.UnwrapAllConversions();

        return operation switch {
            IFieldReferenceOperation { Field: { Name: "Empty", IsStatic: true } } fieldRef
                when IsImmutableArrayType(fieldRef.Field.ContainingType) => true,
            IDefaultValueOperation defaultOp
                when IsImmutableArrayType(defaultOp.Type) => true,
            _ => false
        };
    }

    private static bool IsImmutableArrayType(ITypeSymbol? type) =>
        type is not null &&
        type.OriginalDefinition.ToDisplayString() == "System.Collections.Immutable.ImmutableArray<T>";

    private static bool TryGetToImmutableArraySource(IOperation? operation, out string sourceName) {
        sourceName = "collection";

        if (operation is null) {
            return false;
        }

        operation = operation.UnwrapAllConversions();

        if (operation is IConditionalAccessOperation {
            WhenNotNull: IInvocationOperation { TargetMethod.Name: "ToImmutableArray" }
        } conditionalAccess) {
            sourceName = GetOperandDisplayName(conditionalAccess.Operation);
            return true;
        }

        return false;
    }

    private static bool HasToImmutableArrayOrEmptyExtension(Compilation compilation) {
        foreach (var reference in compilation.References) {
            if (compilation.GetAssemblyOrModuleSymbol(reference) is IAssemblySymbol assembly
                && HasMethodInAssembly(assembly)) {
                return true;
            }
        }

        return HasMethodInAssembly(compilation.Assembly);
    }

    private static bool HasMethodInAssembly(IAssemblySymbol assembly) {
        var stack = new Stack<INamespaceSymbol>();
        stack.Push(assembly.GlobalNamespace);

        while (stack.Count > 0) {
            var ns = stack.Pop();
            foreach (var type in ns.GetTypeMembers()) {
                if (!type.IsStatic || !type.MightContainExtensionMethods) {
                    continue;
                }

                foreach (var member in type.GetMembers("ToImmutableArrayOrEmpty")) {
                    if (member is IMethodSymbol { IsExtensionMethod: true, Parameters.Length: 1 }) {
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
