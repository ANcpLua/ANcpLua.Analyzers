using RoslynOperationExtensions = Microsoft.CodeAnalysis.Operations.OperationExtensions;

namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0031: Suggests using operation extensions instead of verbose patterns.
/// </summary>
/// <remarks>
///     <list type="bullet">
///         <item><c>invocation.TargetMethod.Name == "name"</c> → <c>invocation.IsMethodNamed("name")</c></item>
///         <item>
///             <c>operation.ConstantValue.HasValue &amp;&amp; operation.ConstantValue.Value is T</c> →
///             <c>operation.TryGetConstantValue&lt;T&gt;(out value)</c>
///         </item>
///     </list>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0031UseOperationExtensionsAnalyzer : AlAnalyzer {
    /// <summary>The diagnostic identifier for AL0031.</summary>
    public const string DiagnosticId = "AL0031";

    private const string IInvocationOperationTypeName = "Microsoft.CodeAnalysis.Operations.IInvocationOperation";

    private static readonly DiagnosticDescriptor s_rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.RoslynUtilities,
        DiagnosticSeverity.Info);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_rule];

    /// <summary>Registers syntax or operation actions for analysis.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterCompilationStartAction(OnCompilationStart);

    private static void OnCompilationStart(CompilationStartAnalysisContext context) {
        if (context.Compilation.GetTypeByMetadataName(IInvocationOperationTypeName) is null) {
            return;
        }

        context.RegisterOperationAction(AnalyzeBinaryOperator, OperationKind.BinaryOperator);
    }

    private static void AnalyzeBinaryOperator(OperationAnalysisContext context) {
        if (context.Operation is not IBinaryOperation binary) {
            return;
        }

        if (binary.OperatorKind is BinaryOperatorKind.Equals or BinaryOperatorKind.NotEquals &&
            IsTargetMethodNameComparison(binary, out var methodName)) {
            var suggestion = binary.OperatorKind == BinaryOperatorKind.Equals
                ? $"invocation.IsMethodNamed(containingType, \"{methodName}\")"
                : $"!invocation.IsMethodNamed(containingType, \"{methodName}\")";
            context.ReportDiagnostic(s_rule, binary.Syntax.GetLocation(),
                suggestion, "TargetMethod.Name == comparison");
        }

        if (binary.OperatorKind == BinaryOperatorKind.ConditionalAnd && IsConstantValueHasValueCheck(binary)) {
            context.ReportDiagnostic(s_rule, binary.Syntax.GetLocation(),
                "operation.TryGetConstantValue<T>(out value)", "ConstantValue.HasValue check");
        }
    }

    private static bool IsTargetMethodNameComparison(IBinaryOperation binary, out string? methodName) {
        methodName = null;

        var (propSide, literalSide) = GetPropertyAndLiteralSides(binary);
        if (propSide is null || literalSide is null) {
            return false;
        }

        if (propSide is IPropertyReferenceOperation { Property.Name: "Name", Instance: { } rawInstance } &&
            rawInstance.UnwrapAllConversions() is IPropertyReferenceOperation { Property.Name: "TargetMethod" } &&
            literalSide.ConstantValue is { HasValue: true, Value: string value }) {
            methodName = value;
            return true;
        }

        return false;
    }

    private static (IOperation? propSide, IOperation? literalSide) GetPropertyAndLiteralSides(IBinaryOperation binary) {
        var left = binary.LeftOperand.UnwrapAllConversions();
        var right = binary.RightOperand.UnwrapAllConversions();

        if (left is IPropertyReferenceOperation && right.ConstantValue.HasValue) {
            return (left, right);
        }

        if (right is IPropertyReferenceOperation && left.ConstantValue.HasValue) {
            return (right, left);
        }

        return (null, null);
    }

    private static bool IsConstantValueHasValueCheck(IBinaryOperation binary) {
        var hasHasValueCheck = false;
        var hasValueAccess = false;

        foreach (var descendant in RoslynOperationExtensions.Descendants(binary)) {
            if (descendant is not IPropertyReferenceOperation propRef) {
                continue;
            }

            if (propRef.Instance?.UnwrapAllConversions() is not IPropertyReferenceOperation { Property.Name: "ConstantValue" }) {
                continue;
            }

            switch (propRef.Property.Name) {
                case "HasValue":
                    hasHasValueCheck = true;
                    break;
                case "Value":
                    hasValueAccess = true;
                    break;
            }
        }

        // .HasValue alone is a valid pattern; only suggest TryGetConstantValue when both are accessed
        return hasHasValueCheck && hasValueAccess;
    }
}
