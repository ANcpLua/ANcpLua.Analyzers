
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
    /// <summary>The diagnostic identifier for AL0040.</summary>
    public const string DiagnosticId = "AL0040";

    private static readonly DiagnosticDescriptor s_rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.RoslynUtilities,
        DiagnosticSeverities.Suggestion);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_rule];

    /// <summary>Registers syntax or operation actions for analysis.</summary>

    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterOperationAction(AnalyzePropertyReference, OperationKind.PropertyReference);

    private static void AnalyzePropertyReference(OperationAnalysisContext context) {
        if (context.Operation is not IPropertyReferenceOperation propRef) {
            return;
        }

        switch (propRef.Property.Name) {
            case "Value" when IsTypedConstantType(propRef.Instance?.Type):
                switch (propRef.Instance) {
                    case IPropertyReferenceOperation { Instance: IArrayElementReferenceOperation arrayAccess }:
                        AnalyzeConstructorArgumentsAccess(context, propRef, arrayAccess);
                        break;
                    case IPropertyReferenceOperation indexerAccess:
                        AnalyzeConstructorArgumentsIndexerAccess(context, propRef, indexerAccess);
                        break;
                    case IArrayElementReferenceOperation directArrayAccess:
                        AnalyzeConstructorArgumentsAccess(context, propRef, directArrayAccess);
                        break;
                }

                break;
        }
    }

    private static void AnalyzeConstructorArgumentsAccess(
        OperationAnalysisContext context,
        IOperation valueAccess,
        IArrayElementReferenceOperation arrayAccess) {
        if (arrayAccess.ArrayReference is not IPropertyReferenceOperation { Property.Name: "ConstructorArguments" } arrayProp) {
            return;
        }

        if (!IsAttributeDataType(arrayProp.Instance?.Type)) {
            return;
        }

        var indexStr = GetIndexValue(arrayAccess);
        var attrName = arrayProp.Instance.GetOperandName("attr");
        var suggestion = $"{attrName}.GetConstructorArgument<T>({indexStr})";

        context.ReportDiagnostic(Diagnostic.Create(s_rule, valueAccess.Syntax.GetLocation(), suggestion));
    }

    private static void AnalyzeConstructorArgumentsIndexerAccess(
        OperationAnalysisContext context,
        IOperation valueAccess,
        IPropertyReferenceOperation indexerAccess) {
        if (indexerAccess.Instance is not IPropertyReferenceOperation { Property.Name: "ConstructorArguments" } arrayProp) {
            return;
        }

        if (!IsAttributeDataType(arrayProp.Instance?.Type)) {
            return;
        }

        var indexStr = GetIndexValue(indexerAccess);
        var attrName = arrayProp.Instance.GetOperandName("attr");
        var suggestion = $"{attrName}.GetConstructorArgument<T>({indexStr})";

        context.ReportDiagnostic(Diagnostic.Create(s_rule, valueAccess.Syntax.GetLocation(), suggestion));
    }

    private static bool IsTypedConstantType(ITypeSymbol? type) {
        var typeName = type?.ToDisplayString();
        return typeName is "Microsoft.CodeAnalysis.TypedConstant" or
            "TypedConstant";
    }

    private static bool IsAttributeDataType(ITypeSymbol? type) {
        var typeName = type?.ToDisplayString();
        return typeName is "Microsoft.CodeAnalysis.AttributeData" or
            "AttributeData";
    }

    private static string GetIndexValue(IArrayElementReferenceOperation arrayAccess) {
        if (arrayAccess.Indices.Length is 0) {
            return "0";
        }

        var index = arrayAccess.Indices[0].UnwrapAllConversions();

        if (index is ILiteralOperation { ConstantValue.HasValue: true } literal) {
            return literal.ConstantValue.Value?.ToString() ?? "0";
        }

        return index.GetOperandName("0");
    }

    private static string GetIndexValue(IPropertyReferenceOperation indexerAccess) {
        if (indexerAccess.Arguments.Length is 0) {
            return "0";
        }

        var index = indexerAccess.Arguments[0].Value.UnwrapAllConversions();

        if (index is ILiteralOperation { ConstantValue.HasValue: true } literal) {
            return literal.ConstantValue.Value?.ToString() ?? "0";
        }

        return index.GetOperandName("0");
    }

}
