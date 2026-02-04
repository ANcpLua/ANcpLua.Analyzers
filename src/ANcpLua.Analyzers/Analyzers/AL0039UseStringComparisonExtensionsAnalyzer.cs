using ANcpLua.Analyzers.Core;

namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0039: Suggests using StringComparison extension methods for clearer intent.
/// </summary>
/// <remarks>
///     <list type="bullet">
///         <item><c>str.Equals(other, StringComparison.Ordinal)</c> → <c>str.EqualsOrdinal(other)</c></item>
///         <item><c>str.Equals(other, StringComparison.OrdinalIgnoreCase)</c> → <c>str.EqualsIgnoreCase(other)</c></item>
///         <item><c>str.StartsWith(prefix, StringComparison.Ordinal)</c> → <c>str.StartsWithOrdinal(prefix)</c></item>
///         <item><c>str.Contains(sub, StringComparison.OrdinalIgnoreCase)</c> → <c>str.ContainsIgnoreCase(sub)</c></item>
///     </list>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0039UseStringComparisonExtensionsAnalyzer : AlAnalyzer {
    private static readonly DiagnosticDescriptor Rule = CreateRule(
        DiagnosticIds.UseStringComparisonExtensions,
        DiagnosticCategories.RoslynUtilities,
        DiagnosticSeverities.Suggestion);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    /// <summary>Registers syntax or operation actions for analysis.</summary>

    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);

    private static void AnalyzeInvocation(OperationAnalysisContext context) {
        if (context.Operation is not IInvocationOperation invocation) {
            return;
        }

        var method = invocation.TargetMethod;

        // Check if it's a supported string method
        if (!MappingRegistry.HasStringComparisonExtension(method.Name)) {
            return;
        }

        // Check if the receiver is a string
        var receiverType = invocation.Instance?.Type;
        if (receiverType?.SpecialType != SpecialType.System_String) {
            return;
        }

        // Check if one of the arguments is a StringComparison
        if (FindStringComparisonArgument(invocation) is not { } comparisonArg) {
            return;
        }

        // Extensions only exist for simple cases (value + StringComparison).
        // If there are additional parameters (startIndex, count), skip.
        // Expected: 2 args for simple case (value + StringComparison)
        var nonComparisonArgCount = CountNonStringComparisonArgs(invocation);
        if (nonComparisonArgCount > 1) {
            return;
        }

        // Get the StringComparison value and suffix
        if (GetStringComparisonValue(comparisonArg) is not { } comparisonValue) {
            return;
        }

        if (MappingRegistry.GetStringComparisonSuffix(comparisonValue) is not { } suffix) {
            return;
        }

        // Build the suggestion
        var receiverName = invocation.Instance.GetOperandName("str");
        var extensionName = $"{method.Name}{suffix}";
        var argName = GetFirstStringArgument(invocation);
        var suggestion = $"{receiverName}.{extensionName}({argName})";

        context.ReportDiagnostic(Rule, invocation.Syntax.GetLocation(), suggestion);
    }

    private static int CountNonStringComparisonArgs(IInvocationOperation invocation) {
        var count = 0;
        foreach (var arg in invocation.Arguments) {
            var type = arg.Value.Type;
            if (type is null) {
                count++;
                continue;
            }

            var typeName = type.ToDisplayString();
            if (typeName is not ("System.StringComparison" or "StringComparison")) {
                count++;
            }
        }

        return count;
    }

    private static IArgumentOperation? FindStringComparisonArgument(IInvocationOperation invocation) {
        foreach (var arg in invocation.Arguments) {
            if (arg.Value.Type is not { } type) {
                continue;
            }

            var typeName = type.ToDisplayString();
            if (typeName is "System.StringComparison" or "StringComparison") {
                return arg;
            }
        }

        return null;
    }

    private static string? GetStringComparisonValue(IArgumentOperation argument) {
        var value = argument.Value;

        // Unwrap conversions
        while (value is IConversionOperation conversion) {
            value = conversion.Operand;
        }

        // Check for field reference (e.g., StringComparison.Ordinal)
        if (value is IFieldReferenceOperation fieldRef) {
            return fieldRef.Field.Name;
        }

        return null;
    }

    private static string GetFirstStringArgument(IInvocationOperation invocation) {
        foreach (var arg in invocation.Arguments) {
            // Skip StringComparison arguments
            var type = arg.Value.Type;
            var typeName = type?.ToDisplayString();
            if (typeName is "System.StringComparison" or "StringComparison") {
                continue;
            }

            return arg.Value.GetOperandName();
        }

        return "value";
    }
}
