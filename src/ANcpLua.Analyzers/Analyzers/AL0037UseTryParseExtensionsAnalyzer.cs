using ANcpLua.Analyzers.Core;
using ANcpLua.Roslyn.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using System.Collections.Immutable;

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

        // Check if condition is a TryParse invocation
        var condition = conditional.Condition;

        // Unwrap parentheses and conversions
        while (condition is IParenthesizedOperation paren) {
            condition = paren.Operand;
        }

        if (condition is not IInvocationOperation invocation) {
            return;
        }

        var method = invocation.TargetMethod;

        // Check if it's a TryParse method
        if (method.Name != "TryParse" || !method.IsStatic || method.Parameters.Length < 2) {
            return;
        }

        // Get the containing type to determine which extension to suggest
        if (method.ContainingType is not { } containingType) {
            return;
        }

        var typeName = containingType.ToDisplayString();
        if (MappingRegistry.GetTryParseExtension(typeName) is not { } extensionName) {
            return;
        }

        // Check if the WhenTrue branch returns the out parameter
        // and the WhenFalse returns null/default
        if (!IsTryParseResultPattern(conditional, invocation)) {
            return;
        }

        // Get the string argument name for the suggestion
        var stringArg = GetStringArgumentName(invocation);
        var suggestion = $"{stringArg}.{extensionName}()";

        context.ReportDiagnostic(Diagnostic.Create(Rule, conditional.Syntax.GetLocation(), suggestion));
    }

    private static bool IsTryParseResultPattern(IConditionalOperation conditional, IInvocationOperation tryParse) {
        // The out parameter should be the second argument
        if (tryParse.Arguments.Length < 2) {
            return false;
        }

        // Get the out argument
        var outArg = tryParse.Arguments[1];
        if (outArg.Parameter?.RefKind != RefKind.Out) {
            return false;
        }

        // The WhenTrue should reference the out variable
        if (conditional.WhenTrue is not { } whenTrueOp) {
            return false;
        }

        var whenTrue = whenTrueOp.UnwrapAllConversions();

        // Check if WhenTrue is referencing a local that was declared in the out argument
        if (whenTrue is not ILocalReferenceOperation) {
            return false;
        }

        // The WhenFalse should be null or default only (not other constants like 0)
        // because the extension method returns null on parse failure, not 0
        if (conditional.WhenFalse is not { } whenFalseOp) {
            return false;
        }

        var whenFalse = whenFalseOp.UnwrapAllConversions();

        return whenFalse switch {
            IDefaultValueOperation => true,
            ILiteralOperation { ConstantValue: { HasValue: true, Value: null } } => true,
            IConversionOperation { Operand: IDefaultValueOperation } => true,
            // Do NOT match non-null literals like 0, false, etc. - semantic change
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
