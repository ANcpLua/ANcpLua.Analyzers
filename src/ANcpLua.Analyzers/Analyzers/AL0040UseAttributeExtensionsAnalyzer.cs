using ANcpLua.Analyzers.Core;

namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0040: Suggests using attribute argument extraction extensions.
/// </summary>
/// <remarks>
///     <list type="bullet">
///         <item><c>attr.ConstructorArguments[0].Value</c> → <c>attr.GetConstructorArgument&lt;T&gt;(0)</c></item>
///         <item><c>attr.NamedArguments.FirstOrDefault(...).Value</c> → <c>attr.GetNamedArgument&lt;T&gt;("name")</c></item>
///         <item><c>(Type?)attr.ConstructorArguments[0].Value</c> → <c>attr.GetConstructorArgument&lt;Type&gt;(0)</c></item>
///     </list>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0040UseAttributeExtensionsAnalyzer : AlAnalyzer {
    private static readonly LocalizableResourceString Title = new(
        nameof(Resources.AL0040AnalyzerTitle), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableResourceString MessageFormat = new(
        nameof(Resources.AL0040AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableResourceString Description = new(
        nameof(Resources.AL0040AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.UseAttributeExtensions,
        Title, MessageFormat, DiagnosticCategories.RoslynUtilities,
        DiagnosticSeverity.Info, true, Description,
        HelpLinkBase);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterOperationAction(AnalyzePropertyReference, OperationKind.PropertyReference);

    private static void AnalyzePropertyReference(OperationAnalysisContext context) {
        if (context.Operation is not IPropertyReferenceOperation propRef) {
            return;
        }

        // Check for .Value access on TypedConstant
        if (propRef.Property.Name == "Value" && IsTypedConstantType(propRef.Instance?.Type)) {
            // Check if this is from ConstructorArguments[i] or NamedArguments access
            if (propRef.Instance is IPropertyReferenceOperation innerProp) {
                // ConstructorArguments[i].Value pattern - check parent
                if (innerProp.Instance is IArrayElementReferenceOperation arrayAccess) {
                    AnalyzeConstructorArgumentsAccess(context, propRef, arrayAccess);
                }
            }
            else if (propRef.Instance is IArrayElementReferenceOperation directArrayAccess) {
                // Direct array access pattern
                AnalyzeConstructorArgumentsAccess(context, propRef, directArrayAccess);
            }
        }

        // Check for ConstructorArguments[i] pattern (without .Value)
        if (propRef.Property.Name == "ConstructorArguments") {
            var parent = GetParentOperation(context.Operation);
            if (parent is IArrayElementReferenceOperation) {
                // This is the ConstructorArguments property being indexed
                AnalyzeConstructorArgumentsIndexing(context, propRef, parent);
            }
        }
    }

    private static void AnalyzeConstructorArgumentsAccess(
        OperationAnalysisContext context,
        IPropertyReferenceOperation valueAccess,
        IArrayElementReferenceOperation arrayAccess) {
        // Check if the array is ConstructorArguments
        if (arrayAccess.ArrayReference is not IPropertyReferenceOperation arrayProp) {
            return;
        }

        if (arrayProp.Property.Name != "ConstructorArguments") {
            return;
        }

        // Check if receiver is AttributeData
        if (!IsAttributeDataType(arrayProp.Instance?.Type)) {
            return;
        }

        // Get the index
        var indexStr = GetIndexValue(arrayAccess);
        var attrName = GetOperandName(arrayProp.Instance);
        var suggestion = $"{attrName}.GetConstructorArgument<T>({indexStr})";

        context.ReportDiagnostic(Diagnostic.Create(Rule, valueAccess.Syntax.GetLocation(), suggestion));
    }

    private static void AnalyzeConstructorArgumentsIndexing(
        OperationAnalysisContext context,
        IPropertyReferenceOperation propRef,
        IOperation arrayElementRef) {
        if (!IsAttributeDataType(propRef.Instance?.Type)) {
            return;
        }

        // Only suggest if the result is being used directly (not accessing .Value)
        var parent = GetParentOperation(arrayElementRef);
        if (parent is IPropertyReferenceOperation parentProp && parentProp.Property.Name == "Value") {
            // Will be handled by the other pattern
            return;
        }

        var indexStr = arrayElementRef is IArrayElementReferenceOperation arr ? GetIndexValue(arr) : "0";
        var attrName = GetOperandName(propRef.Instance);
        var suggestion = $"{attrName}.GetConstructorArgument<T>({indexStr})";

        context.ReportDiagnostic(Diagnostic.Create(Rule, arrayElementRef.Syntax.GetLocation(), suggestion));
    }

    private static bool IsTypedConstantType(ITypeSymbol? type) {
        if (type is null) {
            return false;
        }

        var typeName = type.ToDisplayString();
        return typeName is
            "Microsoft.CodeAnalysis.TypedConstant" or
            "TypedConstant";
    }

    private static bool IsAttributeDataType(ITypeSymbol? type) {
        if (type is null) {
            return false;
        }

        var typeName = type.ToDisplayString();
        return typeName is
            "Microsoft.CodeAnalysis.AttributeData" or
            "AttributeData";
    }

    private static string GetIndexValue(IArrayElementReferenceOperation arrayAccess) {
        if (arrayAccess.Indices.Length is 0) {
            return "0";
        }

        var index = arrayAccess.Indices[0];

        // Unwrap conversions
        while (index is IConversionOperation conversion) {
            index = conversion.Operand;
        }

        if (index is ILiteralOperation { ConstantValue.HasValue: true } literal) {
            return literal.ConstantValue.Value?.ToString() ?? "0";
        }

        return GetOperandName(index);
    }

    private static IOperation? GetParentOperation(IOperation operation) {
        // Walk up the operation tree
        return operation.Parent;
    }

    private static string GetOperandName(IOperation? operation) {
        if (operation is null) {
            return "attr";
        }

        // Unwrap conversions
        while (operation is IConversionOperation conversion) {
            operation = conversion.Operand;
        }

        return operation switch {
            ILocalReferenceOperation local => local.Local.Name,
            IParameterReferenceOperation param => param.Parameter.Name,
            IPropertyReferenceOperation prop => prop.Property.Name,
            IFieldReferenceOperation field => field.Field.Name,
            _ => "attr"
        };
    }
}
