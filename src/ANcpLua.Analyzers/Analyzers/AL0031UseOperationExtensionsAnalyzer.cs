using ANcpLua.Analyzers.Core;
using OperationExtensions = Microsoft.CodeAnalysis.Operations.OperationExtensions;

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
    private const string IInvocationOperationTypeName = "Microsoft.CodeAnalysis.Operations.IInvocationOperation";

    private static readonly LocalizableResourceString Title = new(
        nameof(Resources.AL0031AnalyzerTitle), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableResourceString MessageFormat = new(
        nameof(Resources.AL0031AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableResourceString Description = new(
        nameof(Resources.AL0031AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.UseOperationExtensions,
        Title, MessageFormat, DiagnosticCategories.RoslynUtilities,
        DiagnosticSeverity.Info, true, Description,
        HelpLinkBase);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    /// <summary>Registers syntax or operation actions for analysis.</summary>

    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterCompilationStartAction(OnCompilationStart);

    private static void OnCompilationStart(CompilationStartAnalysisContext context) {
        // Only analyze if IInvocationOperation is referenced (indicates Roslyn usage)
        if (context.Compilation.GetTypeByMetadataName(IInvocationOperationTypeName) is null) {
            return;
        }

        context.RegisterOperationAction(AnalyzeBinaryOperator, OperationKind.BinaryOperator);
    }

    private static void AnalyzeBinaryOperator(OperationAnalysisContext context) {
        if (context.Operation is not IBinaryOperation binary) {
            return;
        }

        // Pattern: invocation.TargetMethod.Name == "name" -> invocation.IsMethodNamed(containingType, "name")
        if (binary.OperatorKind is BinaryOperatorKind.Equals or BinaryOperatorKind.NotEquals) {
            if (IsTargetMethodNameComparison(binary, out var methodName)) {
                var suggestion = binary.OperatorKind == BinaryOperatorKind.Equals
                    ? $"invocation.IsMethodNamed(containingType, \"{methodName}\")"
                    : $"!invocation.IsMethodNamed(containingType, \"{methodName}\")";
                context.ReportDiagnostic(Rule, binary.Syntax.GetLocation(),
                    suggestion, "TargetMethod.Name == comparison");
            }
        }

        // Pattern: operation.ConstantValue.HasValue && ... -> TryGetConstantValue<T>()
        if (binary.OperatorKind == BinaryOperatorKind.ConditionalAnd && IsConstantValueHasValueCheck(binary)) {
            context.ReportDiagnostic(Rule, binary.Syntax.GetLocation(),
                "operation.TryGetConstantValue<T>(out value)", "ConstantValue.HasValue check");
        }
    }

    private static bool IsTargetMethodNameComparison(IBinaryOperation binary, out string? methodName) {
        methodName = null;

        var (propSide, literalSide) = GetPropertyAndLiteralSides(binary);
        if (propSide is null || literalSide is null) {
            return false;
        }

        if (propSide is IPropertyReferenceOperation { Property.Name: "Name", Instance: { } rawInstance }) {
            // Unwrap conversions on the Instance to find the TargetMethod property access
            var instance = UnwrapConversions(rawInstance);
            if (instance is IPropertyReferenceOperation { Property.Name: "TargetMethod" } &&
                literalSide.ConstantValue is { HasValue: true, Value: string value }) {
                methodName = value;
                return true;
            }
        }

        return false;
    }

    private static (IOperation? propSide, IOperation? literalSide) GetPropertyAndLiteralSides(IBinaryOperation binary) {
        var left = UnwrapConversions(binary.LeftOperand);
        var right = UnwrapConversions(binary.RightOperand);

        if (left is IPropertyReferenceOperation && right.ConstantValue.HasValue) {
            return (left, right);
        }

        if (right is IPropertyReferenceOperation && left.ConstantValue.HasValue) {
            return (right, left);
        }

        return (null, null);
    }

    private static IOperation UnwrapConversions(IOperation operation) {
        while (operation is IConversionOperation conversion) {
            operation = conversion.Operand;
        }

        return operation;
    }

    private static bool IsConstantValueHasValueCheck(IBinaryOperation binary) {
        foreach (var descendant in OperationExtensions.Descendants(binary)) {
            if (descendant is IPropertyReferenceOperation { Property.Name: "HasValue", Instance: { } rawInstance }) {
                var instance = UnwrapConversions(rawInstance);
                if (instance is IPropertyReferenceOperation { Property.Name: "ConstantValue" }) {
                    return true;
                }
            }
        }

        return false;
    }
}
