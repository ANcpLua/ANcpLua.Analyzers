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
    /// <summary>The diagnostic identifier for AL0038.</summary>
    public const string DiagnosticId = "AL0038";

    private static readonly DiagnosticDescriptor Rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.RoslynUtilities,
        DiagnosticSeverities.Suggestion);

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
        if (!IsTryGetValuePattern(conditional, invocation)) {
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

    private static bool IsTryGetValuePattern(IConditionalOperation conditional, IInvocationOperation invocation) {
        // The WhenTrue should reference the out variable
        var whenTrue = conditional.WhenTrue.UnwrapAllConversions();

        if (whenTrue is not ILocalReferenceOperation localRef) {
            return false;
        }

        // Get the out parameter's declared local (second argument is the out parameter)
        if (invocation.Arguments.Length < 2) {
            return false;
        }

        var outArg = invocation.Arguments[1];
        if (outArg.Value is not IDeclarationExpressionOperation declExpr ||
            declExpr.Expression is not ILocalReferenceOperation outLocal) {
            return false;
        }

        // Verify WhenTrue references the same local as the out parameter
        if (!SymbolEqualityComparer.Default.Equals(localRef.Local, outLocal.Local)) {
            return false;
        }

        // The WhenFalse should be null, default, or a constant
        if (conditional.WhenFalse is not { } whenFalseOp) {
            return false;
        }

        var whenFalse = whenFalseOp.UnwrapAllConversions();

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
        var dictName = invocation.Instance.GetOperandName("dict");

        // Get key name (first argument)
        var keyName = invocation.Arguments.Length > 0
            ? invocation.Arguments[0].Value.GetOperandName("key")
            : "key";

        // Determine extension: GetOrNull if WhenFalse is null, GetOrDefault otherwise
        var whenFalse = conditional.WhenFalse?.UnwrapAllConversions();
        var extensionName = whenFalse switch {
            ILiteralOperation { ConstantValue.HasValue: true, ConstantValue.Value: null } => "GetOrNull",
            IDefaultValueOperation => "GetOrNull",
            _ => "GetOrDefault"
        };

        return (dictName, keyName, extensionName);
    }
}
