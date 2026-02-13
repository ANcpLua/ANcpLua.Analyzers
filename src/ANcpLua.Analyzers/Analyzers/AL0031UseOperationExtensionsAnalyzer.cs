using ANcpLua.Analyzers.Core;
using ANcpLua.Roslyn.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using System.Collections.Immutable;
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
    private const string IInvocationOperationTypeName = "Microsoft.CodeAnalysis.Operations.IInvocationOperation";

    private static readonly DiagnosticDescriptor Rule = CreateRule(
        DiagnosticIds.UseOperationExtensions,
        DiagnosticCategories.RoslynUtilities,
        DiagnosticSeverity.Info);

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
            var instance = rawInstance.UnwrapAllConversions();
            if (instance is IPropertyReferenceOperation { Property.Name: "TargetMethod" } &&
                literalSide.ConstantValue is { HasValue: true, Value: string value }) {
                methodName = value;
                return true;
            }
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

            var instance = propRef.Instance?.UnwrapAllConversions();

            // Check for .HasValue on ConstantValue
            if (propRef.Property.Name == "HasValue" &&
                instance is IPropertyReferenceOperation { Property.Name: "ConstantValue" }) {
                hasHasValueCheck = true;
            }

            // Check for .Value on ConstantValue
            if (propRef.Property.Name == "Value" &&
                instance is IPropertyReferenceOperation { Property.Name: "ConstantValue" }) {
                hasValueAccess = true;
            }
        }

        // Only suggest TryGetConstantValue when both .HasValue and .Value are accessed
        // Just checking .HasValue alone is a valid pattern that doesn't need refactoring
        return hasHasValueCheck && hasValueAccess;
    }
}
