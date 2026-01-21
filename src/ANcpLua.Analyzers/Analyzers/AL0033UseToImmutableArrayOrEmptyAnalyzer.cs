using ANcpLua.Analyzers.Core;

namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0033: Suggests using ToImmutableArrayOrEmpty() instead of null-conditional with fallback.
/// </summary>
/// <remarks>
///     <c>collection?.ToImmutableArray() ?? ImmutableArray&lt;T&gt;.Empty</c> → <c>collection.ToImmutableArrayOrEmpty()</c>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0033UseToImmutableArrayOrEmptyAnalyzer : AlAnalyzer {
    private static readonly LocalizableResourceString Title = new(
        nameof(Resources.AL0033AnalyzerTitle), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableResourceString MessageFormat = new(
        nameof(Resources.AL0033AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableResourceString Description = new(
        nameof(Resources.AL0033AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.UseToImmutableArrayOrEmpty,
        Title, MessageFormat, DiagnosticCategories.RoslynUtilities,
        DiagnosticSeverity.Info, true, Description,
        HelpLinkBase);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

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
        while (operation is IConversionOperation conversion) {
            operation = conversion.Operand;
        }

        // Check for ImmutableArray<T>.Empty field access
        // Use display string check since GetTypeByMetadataName can fail for some compilation contexts
        if (operation is IFieldReferenceOperation fieldRef) {
            if (fieldRef.Field.Name == "Empty" &&
                fieldRef.Field.IsStatic &&
                IsImmutableArrayType(fieldRef.Field.ContainingType)) {
                return true;
            }
        }

        // Also check for default(ImmutableArray<T>) or ImmutableArray<T>.default or just default
        if (operation is IDefaultValueOperation defaultOp) {
            if (IsImmutableArrayType(defaultOp.Type)) {
                return true;
            }
        }

        return false;
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
        while (operation is IConversionOperation conversion) {
            operation = conversion.Operand;
        }

        // Check for null-conditional invocation: source?.ToImmutableArray()
        if (operation is IConditionalAccessOperation conditionalAccess) {
            if (conditionalAccess.WhenNotNull is IInvocationOperation invocation) {
                if (invocation.TargetMethod.Name == "ToImmutableArray") {
                    sourceName = GetOperandDisplayName(conditionalAccess.Operation);
                    return true;
                }
            }
        }

        return false;
    }

    private static string GetOperandDisplayName(IOperation operation) {
        // Unwrap conversions
        while (operation is IConversionOperation conversion) {
            operation = conversion.Operand;
        }

        return operation switch {
            ILocalReferenceOperation local => local.Local.Name,
            IParameterReferenceOperation param => param.Parameter.Name,
            IPropertyReferenceOperation prop => prop.Property.Name,
            IFieldReferenceOperation field => field.Field.Name,
            IInvocationOperation inv => $"{inv.TargetMethod.Name}()",
            _ => "collection"
        };
    }
}
