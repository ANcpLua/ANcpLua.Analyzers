using ANcpLua.Analyzers.Core;
using ANcpLua.Roslyn.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using System.Collections.Immutable;

namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0036: Suggests using Guard.NotNull() instead of null-coalescing throw patterns.
/// </summary>
/// <remarks>
///     <list type="bullet">
///         <item><c>value ?? throw new ArgumentNullException(nameof(value))</c> → <c>Guard.NotNull(value)</c></item>
///         <item><c>value ?? throw new ArgumentNullException("value")</c> → <c>Guard.NotNull(value)</c></item>
///     </list>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0036UseGuardNotNullAnalyzer : AlAnalyzer {
    /// <summary>The diagnostic identifier for AL0036.</summary>
    public const string DiagnosticId = "AL0036";

    private static readonly DiagnosticDescriptor Rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.RoslynUtilities,
        DiagnosticSeverities.Suggestion);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    /// <summary>Registers syntax or operation actions for analysis.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterOperationAction(AnalyzeCoalesce, OperationKind.Coalesce);

    private static void AnalyzeCoalesce(OperationAnalysisContext context) {
        if (context.Operation is not ICoalesceOperation coalesce) {
            return;
        }

        // Check if the right side is a throw expression with ArgumentNullException
        if (!IsArgumentNullExceptionThrow(coalesce.WhenNull)) {
            return;
        }

        // Get the name of the left operand for the message
        var operandName = coalesce.Value.GetOperandName();

        context.ReportDiagnostic(Diagnostic.Create(Rule, coalesce.Syntax.GetLocation(), operandName));
    }

    private static bool IsArgumentNullExceptionThrow(IOperation? operation) {
        // Unwrap conversions - use pattern match to handle nullable
        if (operation is not IConversionOperation and not IThrowOperation) {
            // Fast path: not a conversion or throw
            if (operation is null) {
                return false;
            }
        }

        while (operation is IConversionOperation conversion) {
            operation = conversion.Operand;
        }

        // Check for throw expression
        if (operation is not IThrowOperation { Exception: { } exception }) {
            return false;
        }

        // Unwrap conversions and check if creating an ArgumentNullException
        var unwrappedException = exception.UnwrapAllConversions();

        return unwrappedException is IObjectCreationOperation { Type: { } exceptionType }
               && OperationHelper.IsArgumentNullException(exceptionType);
    }
}
