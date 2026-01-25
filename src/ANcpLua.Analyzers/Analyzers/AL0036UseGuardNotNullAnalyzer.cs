using ANcpLua.Analyzers.Core;

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
    private static readonly LocalizableResourceString Title = new(
        nameof(Resources.AL0036AnalyzerTitle), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableResourceString MessageFormat = new(
        nameof(Resources.AL0036AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableResourceString Description = new(
        nameof(Resources.AL0036AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.UseGuardNotNull,
        Title, MessageFormat, DiagnosticCategories.RoslynUtilities,
        DiagnosticSeverities.Suggestion, true, Description,
        HelpLinkBase);

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
        var operandName = GetOperandName(coalesce.Value);

        context.ReportDiagnostic(Diagnostic.Create(Rule, coalesce.Syntax.GetLocation(), operandName));
    }

    private static bool IsArgumentNullExceptionThrow(IOperation? operation) {
        if (operation is null) {
            return false;
        }

        // Unwrap conversions
        while (operation is IConversionOperation conversion) {
            operation = conversion.Operand;
        }

        // Check for throw expression
        if (operation is not IThrowOperation throwOp) {
            return false;
        }

        // Get the exception being thrown
        if (throwOp.Exception is not { } exception) {
            return false;
        }

        // Unwrap conversions
        while (exception is IConversionOperation exConversion) {
            exception = exConversion.Operand;
        }

        // Check if it's creating an ArgumentNullException
        if (exception is not IObjectCreationOperation { Type: { } exceptionType }) {
            return false;
        }

        // Check if it's ArgumentNullException
        var typeName = exceptionType.ToDisplayString();
        return typeName is "System.ArgumentNullException" or "ArgumentNullException";
    }

    private static string GetOperandName(IOperation operation) {
        // Unwrap conversions
        while (operation is IConversionOperation conversion) {
            operation = conversion.Operand;
        }

        return operation switch {
            ILocalReferenceOperation local => local.Local.Name,
            IParameterReferenceOperation param => param.Parameter.Name,
            IPropertyReferenceOperation prop => prop.Property.Name,
            IFieldReferenceOperation field => field.Field.Name,
            IInvocationOperation inv => $"{inv.TargetMethod.Name}()",
            _ => "value"
        };
    }
}
