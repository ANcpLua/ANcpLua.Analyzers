
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

        // Check if the right side is an empty collection pattern
        if (!IsEmptyCollectionExpression(coalesce.WhenNull)) {
            return;
        }

        // Check the coalesce result type — this is the type the ?? expression evaluates to.
        // OrEmpty() returns IEnumerable<T>, so only fire when the result type IS IEnumerable<T>.
        // If the result is a concrete type (Dictionary, List, string[], IList, etc.),
        // .OrEmpty() would lose that type information.
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

        // Unwrap conversions
        operation = operation.UnwrapAllConversions();

        switch (operation) {
            // Check for collection expression [] (empty)
            case ICollectionExpressionOperation { Elements.Length: 0 }:
                return true;
            // Check for Array.Empty<T>() or Enumerable.Empty<T>()
            // Use name-based comparison since these are well-known types
            case IInvocationOperation invocation: {
                var method = invocation.TargetMethod;
                if (method is {
                    Name: "Empty",
                    IsStatic: true,
                    Parameters.Length: 0
                }) {
                    var containingType = method.ContainingType;
                    if (containingType is not null) {
                        var typeName = containingType.Name;
                        var namespaceName = containingType.ContainingNamespace?.ToDisplayString();
                        if (typeName == "Array" && namespaceName == "System" ||
                            typeName == "Enumerable" && namespaceName == "System.Linq") {
                            return true;
                        }
                    }
                }

                break;
            }
            // Check for new T[0] or new T[] { }
            // Check if it's an empty array (size 0 or empty initializer)
            case IArrayCreationOperation {
                DimensionSizes: [
                    {
                        ConstantValue: { HasValue: true, Value: 0 }
                    }
                ]
            }:
            case IArrayCreationOperation {
                Initializer.ElementValues.Length: 0
            }:
                return true;
        }

        return false;
    }

    private static bool IsEnumerableType(ITypeSymbol type) {
        // Only match IEnumerable<T> itself — not concrete types that implement it.
        // OrEmpty() returns IEnumerable<T>, so replacing `dict ?? []` with `dict.OrEmpty()`
        // would lose the concrete type (Dictionary, List, string[], IList, etc.).
        return type is INamedTypeSymbol { IsGenericType: true } named
               && named.OriginalDefinition.ToDisplayString() == "System.Collections.Generic.IEnumerable<T>";
    }

    private static string GetOperandDisplayName(IOperation operation) =>
        operation.GetOperandName("collection");
}
