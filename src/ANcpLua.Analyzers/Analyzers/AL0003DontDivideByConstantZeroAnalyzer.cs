using ANcpLua.Analyzers.Core;

namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
/// AL0003: Integers and Decimal should never be divided by the constant 0.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AL0003DontDivideByConstantZeroAnalyzer : ALAnalyzer
{
    public const string DiagnosticId = "AL0003";
    private const string Category = "Reliability";

    private static readonly LocalizableResourceString Title = new(
        nameof(Resources.AL0003AnalyzerTitle), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableResourceString MessageFormat = new(
        nameof(Resources.AL0003AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableResourceString Description = new(
        nameof(Resources.AL0003AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId, Title, MessageFormat, Category,
        DiagnosticSeverity.Error, isEnabledByDefault: true, Description,
        HelpLinkBase + "AL0003.md");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterOperationAction(BinaryOperationAction, OperationKind.Binary);

    private static void BinaryOperationAction(OperationAnalysisContext context)
    {
        var operation = (IBinaryOperation)context.Operation;

        if (operation.OperatorKind is not (BinaryOperatorKind.Divide or BinaryOperatorKind.Remainder))
            return;

        var leftType = operation.LeftOperand.Type;
        if (leftType is null || !IsIntegerOrDecimalType(leftType))
            return;

        var rightConstant = operation.RightOperand.ConstantValue;
        if (!rightConstant.HasValue || !IsZero(rightConstant.Value))
            return;

        context.ReportDiagnostic(Diagnostic.Create(Rule, operation.Syntax.GetLocation()));
    }

    private static bool IsIntegerOrDecimalType(ITypeSymbol typeSymbol) =>
        typeSymbol.SpecialType is
            SpecialType.System_Byte or SpecialType.System_SByte or
            SpecialType.System_Int16 or SpecialType.System_UInt16 or
            SpecialType.System_Int32 or SpecialType.System_UInt32 or
            SpecialType.System_Int64 or SpecialType.System_UInt64 or
            SpecialType.System_Decimal;

    private static bool IsZero(object? value) =>
        value is 0 or 0u or 0L or 0ul or (byte)0 or (sbyte)0 or (short)0 or (ushort)0 or 0.0m;
}
