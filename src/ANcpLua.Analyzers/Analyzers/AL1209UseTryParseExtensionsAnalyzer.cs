
namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL1209: Suggests using TryParse extension methods instead of verbose patterns.
/// </summary>
/// <remarks>
///     <list type="bullet">
///         <item><c>int.TryParse(s, out var v) ? v : null</c> → <c>s.TryParseInt32()</c></item>
///         <item><c>int.TryParse(s, out var v) ? v : 0</c> → <c>s.TryParseInt32(0)</c></item>
///         <item><c>Guid.TryParse(s, out var v) ? v : default</c> → <c>s.TryParseGuid()</c></item>
///     </list>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al1209UseTryParseExtensionsAnalyzer : AlAnalyzer {
    /// <summary>The diagnostic identifier for AL1209.</summary>
    public const string DiagnosticId = "AL1209";

    private const string TryExtensionsMetadataName = "ANcpLua.Roslyn.Utilities.TryExtensions";

    private static readonly DiagnosticDescriptor s_rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.RoslynUtilities,
        DiagnosticSeverities.Suggestion);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_rule];

    /// <summary>Registers syntax or operation actions for analysis.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterCompilationStartAction(OnCompilationStart);

    private static void OnCompilationStart(CompilationStartAnalysisContext context) {
        // TryParseInt32() and related methods live in ANcpLua.Roslyn.Utilities.TryExtensions. Only fire
        // when that type is present and callable from this compilation; otherwise the suggestion/fix
        // would reference a symbol the consumer cannot resolve.
        if (context.Compilation.GetTypeByMetadataName(TryExtensionsMetadataName) is not { } gateType) {
            return;
        }

        if (!context.Compilation.IsSymbolAccessibleWithin(gateType, context.Compilation.Assembly)) {
            return;
        }

        context.RegisterOperationAction(AnalyzeConditional, OperationKind.Conditional);
    }

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

        if (method.Name is not "TryParse" || !method.IsStatic || method.Parameters.Length != 2) {
            return;
        }

        if (!IsStringInputTryParse(method) ||
            method.ContainingType is not { } containingType) {
            return;
        }

        if (MappingRegistry.GetTryParseExtension(containingType.ToDisplayString()) is not { } extensionName) {
            return;
        }

        if (TryGetOutArgumentName(invocation) is not { } outVarName) {
            return;
        }

        if (!IsTryParseResultPattern(conditional, invocation, outVarName)) {
            return;
        }

        var stringArg = GetStringArgumentName(invocation);
        context.ReportDiagnostic(Diagnostic.Create(s_rule, conditional.Syntax.GetLocation(),
            $"{stringArg}.{extensionName}()"));
    }

    private static bool IsTryParseResultPattern(IConditionalOperation conditional, IInvocationOperation tryParse, string outVarName) {
        if (tryParse.Arguments[1].Parameter?.RefKind != RefKind.Out) {
            return false;
        }

        if (conditional.WhenTrue is not { } whenTrueOp) {
            return false;
        }

        if (whenTrueOp.UnwrapAllConversions() is not ILocalReferenceOperation { Local.Name: var name }) {
            return false;
        }

        if (name != outVarName) {
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

    private static bool IsStringInputTryParse(IMethodSymbol method) {
        if (method.Parameters.Length is not 2) {
            return false;
        }

        return method.Parameters[0].Type.SpecialType == SpecialType.System_String;
    }

    private static string? TryGetOutArgumentName(IInvocationOperation invocation) {
        if (invocation.Arguments.Length != 2 ||
            invocation.Arguments[1].Syntax is not ArgumentSyntax { RefKindKeyword: { } outKeyword, Expression: var outExpression } ||
            outKeyword.Kind() is not SyntaxKind.OutKeyword) {
            return null;
        }

        return outExpression switch {
            DeclarationExpressionSyntax { Designation: SingleVariableDesignationSyntax { Identifier.Text: var id } } => id,
            IdentifierNameSyntax { Identifier.Text: var id } => id,
            _ => null
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
