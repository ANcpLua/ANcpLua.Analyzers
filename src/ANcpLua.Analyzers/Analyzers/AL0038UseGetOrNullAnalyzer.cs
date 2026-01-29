using ANcpLua.Analyzers.Core;

namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0038: Suggests using GetOrNull/GetOrDefault instead of TryGetValue patterns.
/// </summary>
/// <remarks>
///     <list type="bullet">
///         <item><c>dict.TryGetValue(key, out var v) ? v : null</c> → <c>dict.GetOrNull(key)</c></item>
///         <item><c>dict.TryGetValue(key, out var v) ? v : default</c> → <c>dict.GetOrDefault(key, default)</c></item>
///     </list>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0038UseGetOrNullAnalyzer : AlAnalyzer {
    private static readonly LocalizableResourceString Title = new(
        nameof(Resources.AL0038AnalyzerTitle), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableResourceString MessageFormat = new(
        nameof(Resources.AL0038AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableResourceString Description = new(
        nameof(Resources.AL0038AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.UseGetOrNull,
        Title, MessageFormat, DiagnosticCategories.RoslynUtilities,
        DiagnosticSeverities.Suggestion, true, Description,
        HelpLinkBase);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    /// <summary>Registers syntax or operation actions for analysis.</summary>

    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterOperationAction(AnalyzeConditional, OperationKind.Conditional);

    private static void AnalyzeConditional(OperationAnalysisContext context) {
        if (context.Operation is not IConditionalOperation conditional) {
            return;
        }

        // Check if condition is a TryGetValue invocation
        var condition = conditional.Condition;

        // Unwrap parentheses
        while (condition is IParenthesizedOperation paren) {
            condition = paren.Operand;
        }

        if (condition is not IInvocationOperation invocation) {
            return;
        }

        var method = invocation.TargetMethod;

        // Check if it's a TryGetValue method
        if (method.Name != "TryGetValue" || method.Parameters.Length < 2) {
            return;
        }

        // Check if the receiver type implements IDictionary or IReadOnlyDictionary
        var receiverType = invocation.Instance?.Type;
        if (receiverType is null || !IsDictionaryType(receiverType)) {
            return;
        }

        // Check if the pattern matches: TryGetValue(...) ? outVar : default/null
        if (!IsTryGetValuePattern(conditional)) {
            return;
        }

        // Determine which extension to suggest
        var (dictName, keyName, extensionName) = GetSuggestion(invocation, conditional);
        var suggestion = $"{dictName}.{extensionName}({keyName})";

        context.ReportDiagnostic(Diagnostic.Create(Rule, conditional.Syntax.GetLocation(), suggestion));
    }

    private static bool IsDictionaryType(ITypeSymbol type) {
        // Check if it's a dictionary type by interface
        foreach (var iface in type.AllInterfaces) {
            var ifaceName = iface.OriginalDefinition.ToDisplayString();
            if (ifaceName is
                "System.Collections.Generic.IDictionary<TKey, TValue>" or
                "System.Collections.Generic.IReadOnlyDictionary<TKey, TValue>") {
                return true;
            }
        }

        // Also check the type itself
        var typeName = type.OriginalDefinition.ToDisplayString();
        return typeName is
            "System.Collections.Generic.Dictionary<TKey, TValue>" or
            "System.Collections.Generic.IDictionary<TKey, TValue>" or
            "System.Collections.Generic.IReadOnlyDictionary<TKey, TValue>" or
            "System.Collections.Concurrent.ConcurrentDictionary<TKey, TValue>";
    }

    private static bool IsTryGetValuePattern(IConditionalOperation conditional) {
        // The WhenTrue should reference the out variable
        var whenTrue = OperationHelper.UnwrapConversions(conditional.WhenTrue);

        if (whenTrue is not ILocalReferenceOperation) {
            return false;
        }

        // The WhenFalse should be null, default, or a constant
        var whenFalse = OperationHelper.UnwrapConversions(conditional.WhenFalse);

        return whenFalse switch {
            IDefaultValueOperation => true,
            ILiteralOperation { ConstantValue.HasValue: true, ConstantValue.Value: null } => true,
            IConversionOperation { Operand: IDefaultValueOperation } => true,
            _ => false
        };
    }

    private static (string dictName, string keyName, string extensionName) GetSuggestion(
        IInvocationOperation invocation,
        IConditionalOperation conditional) {
        // Get dictionary name
        var dictName = GetOperandName(invocation.Instance);

        // Get key name (first argument)
        var keyName = invocation.Arguments.Length > 0
            ? GetOperandName(invocation.Arguments[0].Value)
            : "key";

        // Determine extension: GetOrNull if WhenFalse is null, GetOrDefault otherwise
        var whenFalse = OperationHelper.UnwrapConversions(conditional.WhenFalse);
        var extensionName = whenFalse switch {
            ILiteralOperation { ConstantValue.HasValue: true, ConstantValue.Value: null } => "GetOrNull",
            IDefaultValueOperation => "GetOrNull",
            _ => "GetOrDefault"
        };

        return (dictName, keyName, extensionName);
    }

    private static string GetOperandName(IOperation? operation) =>
        OperationHelper.GetOperandName(operation, "dict");
}
