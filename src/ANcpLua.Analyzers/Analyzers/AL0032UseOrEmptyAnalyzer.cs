using ANcpLua.Analyzers.Core;
using ANcpLua.Roslyn.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using System.Collections.Immutable;

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
    private static readonly DiagnosticDescriptor Rule = CreateRule(
        DiagnosticIds.UseOrEmpty,
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

        // Check if the left side is an IEnumerable<T> type (excluding strings)
        var leftType = coalesce.Value.Type;
        if (leftType is null || leftType.SpecialType == SpecialType.System_String) {
            return;
        }

        // Must be IEnumerable<T> or implement it (check via display string to avoid compilation lookup issues)
        if (!IsEnumerableType(leftType)) {
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
        // Direct match - check if it IS IEnumerable<T>
        var displayName = type.OriginalDefinition.ToDisplayString();
        if (displayName == "System.Collections.Generic.IEnumerable<T>") {
            return true;
        }

        // Implements IEnumerable<T> - check interfaces via display string
        foreach (var iface in type.AllInterfaces) {
            var ifaceDisplayName = iface.OriginalDefinition.ToDisplayString();
            if (ifaceDisplayName == "System.Collections.Generic.IEnumerable<T>") {
                return true;
            }
        }

        return false;
    }

    private static string GetOperandDisplayName(IOperation operation) =>
        operation.GetOperandName("collection");
}
