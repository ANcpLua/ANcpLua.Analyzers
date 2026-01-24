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
    private static readonly LocalizableResourceString Title = new(
        nameof(Resources.AL0039AnalyzerTitle), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableResourceString MessageFormat = new(
        nameof(Resources.AL0039AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableResourceString Description = new(
        nameof(Resources.AL0039AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.UseStringComparisonExtensions,
        Title, MessageFormat, DiagnosticCategories.RoslynUtilities,
        DiagnosticSeverities.Suggestion, true, Description,
        HelpLinkBase);

    // Methods that have StringComparison extension equivalents
    private static readonly HashSet<string> SupportedMethods = [
        "Equals",
        "StartsWith",
        "EndsWith",
        "Contains",
        "IndexOf",
        "LastIndexOf"
    ];

    // Mapping from StringComparison value to extension suffix
    private static readonly Dictionary<string, string> ComparisonToSuffix = new(StringComparer.Ordinal) {
        ["Ordinal"] = "Ordinal",
        ["OrdinalIgnoreCase"] = "IgnoreCase",
        ["CurrentCulture"] = "CurrentCulture",
        ["CurrentCultureIgnoreCase"] = "CurrentCultureIgnoreCase",
        ["InvariantCulture"] = "InvariantCulture",
        ["InvariantCultureIgnoreCase"] = "InvariantCultureIgnoreCase"
    };

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
        if (!SupportedMethods.Contains(method.Name)) {
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

        // Get the StringComparison value
        var comparisonValue = GetStringComparisonValue(comparisonArg);
        if (comparisonValue is null || !ComparisonToSuffix.TryGetValue(comparisonValue, out var suffix)) {
            return;
        }

        // Build the suggestion
        var receiverName = GetOperandName(invocation.Instance);
        var extensionName = $"{method.Name}{suffix}";
        var argName = GetFirstStringArgument(invocation);
        var suggestion = $"{receiverName}.{extensionName}({argName})";

        context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.Syntax.GetLocation(), suggestion));
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
            if (type is not null) {
                var typeName = type.ToDisplayString();
                if (typeName is "System.StringComparison" or "StringComparison") {
                    continue;
                }
            }

            return GetOperandName(arg.Value);
        }

        return "value";
    }

    private static string GetOperandName(IOperation? operation) {
        if (operation is null) {
            return "str";
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
            ILiteralOperation { ConstantValue.HasValue: true, ConstantValue.Value: string s } => $"\"{s}\"",
            _ => "str"
        };
    }
}
