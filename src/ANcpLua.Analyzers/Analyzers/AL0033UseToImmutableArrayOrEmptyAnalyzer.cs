using ANcpLua.Analyzers.Core;

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

        // Check if the right side is ImmutableArray<T>.Empty
        if (!IsImmutableArrayEmpty(coalesce.WhenNull)) {
            return;
        }

        // Check if the left side is a null-conditional ToImmutableArray() call
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

        // Unwrap conversions
        operation = operation.UnwrapAllConversions();

        switch (operation) {
            // Check for ImmutableArray<T>.Empty field access
            // Use display string check since GetTypeByMetadataName can fail for some compilation contexts
            case IFieldReferenceOperation {
                Field: {
                    Name: "Empty",
                    IsStatic: true
                }
            } fieldRef when IsImmutableArrayType(fieldRef.Field.ContainingType):
            // Also check for default(ImmutableArray<T>) or ImmutableArray<T>.default or just default
            case IDefaultValueOperation defaultOp when IsImmutableArrayType(defaultOp.Type):
                return true;
            default:
                return false;
        }
    }

    private static bool IsImmutableArrayType(ITypeSymbol? type) {
        if (type is null) {
            return false;
        }

        // Check via display string (more reliable across compilation contexts)
        var displayName = type.OriginalDefinition.ToDisplayString();
        return displayName == "System.Collections.Immutable.ImmutableArray<T>";
    }

    private static bool TryGetToImmutableArraySource(IOperation? operation, out string sourceName) {
        sourceName = "collection";

        if (operation is null) {
            return false;
        }

        // Unwrap conversions
        operation = operation.UnwrapAllConversions();

        // Check for null-conditional invocation: source?.ToImmutableArray()
        if (operation is IConditionalAccessOperation {
            WhenNotNull: IInvocationOperation {
                TargetMethod.Name: "ToImmutableArray"
            }
        } conditionalAccess) {
            sourceName = GetOperandDisplayName(conditionalAccess.Operation);
            return true;
        }

        return false;
    }

    private static string GetOperandDisplayName(IOperation operation) =>
        operation.GetOperandName("collection");
}
