
namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL1210: Suggests using StringComparison extension methods for clearer intent.
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
public sealed partial class Al1210UseStringComparisonExtensionsAnalyzer : AlAnalyzer {
    /// <summary>The diagnostic identifier for AL1210.</summary>
    public const string DiagnosticId = "AL1210";

    private static readonly DiagnosticDescriptor s_rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.RoslynUtilities,
        DiagnosticSeverities.Suggestion);

    private const string StringComparisonExtensionsMetadataName =
        "ANcpLua.Roslyn.Utilities.StringComparisonExtensions";

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_rule];

    /// <summary>Registers syntax or operation actions for analysis.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterCompilationStartAction(OnCompilationStart);

    private static void OnCompilationStart(CompilationStartAnalysisContext context) {
        // The suggested replacements (EqualsOrdinal, ContainsIgnoreCase, ...) live in
        // ANcpLua.Roslyn.Utilities.StringComparisonExtensions. Only analyze when that type is
        // present AND callable from this compilation; otherwise the code fix would rewrite a BCL
        // StringComparison call into a symbol the consumer can never resolve. A project that merely
        // pulls the analyzer package gets the helper DLL on the analyzer load path, not as a compile
        // reference, so the type does not resolve there and the rule stays silent.
        if (context.Compilation.GetTypeByMetadataName(StringComparisonExtensionsMetadataName) is not { } extensionsType) {
            return;
        }

        if (!context.Compilation.IsSymbolAccessibleWithin(extensionsType, context.Compilation.Assembly)) {
            return;
        }

        context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
    }

    private static void AnalyzeInvocation(OperationAnalysisContext context) {
        if (context.Operation is not IInvocationOperation invocation) {
            return;
        }

        var method = invocation.TargetMethod;

        if (!MappingRegistry.HasStringComparisonExtension(method.Name)) {
            return;
        }

        if (invocation.Instance?.Type?.SpecialType != SpecialType.System_String) {
            return;
        }

        if (FindStringComparisonArgument(invocation) is not { } comparisonArg) {
            return;
        }

        // Extensions only cover simple cases, not overloads with startIndex/count.
        // Replace takes 2 non-comparison args (oldValue, newValue) — all others take 1.
        var maxNonComparisonArgs = method.Name is "Replace" ? 2 : 1;
        if (CountNonStringComparisonArgs(invocation) > maxNonComparisonArgs) {
            return;
        }

        if (GetStringComparisonValue(comparisonArg) is not { } comparisonValue) {
            return;
        }

        if (MappingRegistry.GetStringComparisonSuffix(comparisonValue) is not { } suffix) {
            return;
        }

        var receiverName = invocation.Instance.GetOperandName("str");
        var extensionName = $"{method.Name}{suffix}";
        var argName = GetFirstStringArgument(invocation);

        context.ReportDiagnostic(s_rule, invocation.Syntax.GetLocation(),
            $"{receiverName}.{extensionName}({argName})");
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

        while (value is IConversionOperation conversion) {
            value = conversion.Operand;
        }

        return value is IFieldReferenceOperation fieldRef ? fieldRef.Field.Name : null;
    }

    private static string GetFirstStringArgument(IInvocationOperation invocation) {
        foreach (var arg in invocation.Arguments) {
            var typeName = arg.Value.Type?.ToDisplayString();
            if (typeName is "System.StringComparison" or "StringComparison") {
                continue;
            }

            return arg.Value.GetOperandName();
        }

        return "value";
    }
}
