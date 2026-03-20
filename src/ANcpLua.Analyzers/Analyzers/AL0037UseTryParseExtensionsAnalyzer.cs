
namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0037: Suggests using TryParse extension methods instead of verbose patterns.
/// </summary>
/// <remarks>
///     <list type="bullet">
///         <item><c>int.TryParse(s, out var v) ? v : null</c> → <c>s.TryParseInt32()</c></item>
///         <item><c>int.TryParse(s, out var v) ? v : 0</c> → <c>s.TryParseInt32(0)</c></item>
///         <item><c>Guid.TryParse(s, out var v) ? v : default</c> → <c>s.TryParseGuid()</c></item>
///     </list>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0037UseTryParseExtensionsAnalyzer : AlAnalyzer {
    /// <summary>The diagnostic identifier for AL0037.</summary>
    public const string DiagnosticId = "AL0037";

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

        var condition = conditional.Condition;

        while (condition is IParenthesizedOperation paren) {
            condition = paren.Operand;
        }

        if (condition is not IInvocationOperation invocation) {
            return;
        }

        var method = invocation.TargetMethod;

        if (method.Name != "TryParse" || !method.IsStatic || method.Parameters.Length < 2) {
            return;
        }

        if (method.ContainingType is not { } containingType) {
            return;
        }

        if (MappingRegistry.GetTryParseExtension(containingType.ToDisplayString()) is not { } extensionName) {
            return;
        }

        if (!IsTryParseResultPattern(conditional, invocation)) {
            return;
        }

        var stringArg = GetStringArgumentName(invocation);
        context.ReportDiagnostic(Diagnostic.Create(Rule, conditional.Syntax.GetLocation(),
            $"{stringArg}.{extensionName}()"));
    }

    private static bool IsTryParseResultPattern(IConditionalOperation conditional, IInvocationOperation tryParse) {
        if (tryParse.Arguments.Length < 2) {
            return false;
        }

        if (tryParse.Arguments[1].Parameter?.RefKind != RefKind.Out) {
            return false;
        }

        if (conditional.WhenTrue is not { } whenTrueOp) {
            return false;
        }

        if (whenTrueOp.UnwrapAllConversions() is not ILocalReferenceOperation) {
            return false;
        }

        if (conditional.WhenFalse is not { } whenFalseOp) {
            return false;
        }

        return whenFalseOp.UnwrapAllConversions() switch {
            IDefaultValueOperation => true,
            ILiteralOperation { ConstantValue: { HasValue: true, Value: null } } => true,
            IConversionOperation { Operand: IDefaultValueOperation } => true,
            // Non-null literals (0, false, etc.) would change semantics
            _ => false
        };
    }

    private static string GetStringArgumentName(IInvocationOperation invocation) {
        if (invocation.Arguments.Length is 0) {
            return "value";
        }

        var firstArg = invocation.Arguments[0].Value;
        return firstArg.UnwrapAllConversions().GetOperandName();
    }
}
