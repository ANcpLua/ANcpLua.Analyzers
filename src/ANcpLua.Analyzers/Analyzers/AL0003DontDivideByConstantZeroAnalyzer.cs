using ANcpLua.Analyzers.Core;

namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0003: Integers and Decimal should never be divided by the constant 0.
/// </summary>
/// <remarks>
///     <para>
///         Division by zero for integer types throws <see cref="DivideByZeroException" />
///         at runtime, and for <see cref="decimal" /> it also throws an exception. Unlike
///         floating-point types which produce infinity or NaN, these divisions always fail.
///     </para>
///     <para>
///         This analyzer catches division and remainder (modulo) operations where the
///         divisor is a compile-time constant zero. It covers all integral types including
///         <c>byte</c>, <c>sbyte</c>, <c>short</c>, <c>ushort</c>, <c>int</c>, <c>uint</c>,
///         <c>long</c>, <c>ulong</c>, <c>nint</c>, <c>nuint</c>, <c>Int128</c>, <c>UInt128</c>,
///         and <c>decimal</c>.
///     </para>
///     <para>
///         Floating-point division by zero (<c>float</c>, <c>double</c>) is not flagged
///         because it produces valid IEEE 754 values (infinity or NaN) rather than
///         throwing exceptions.
///     </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0003DontDivideByConstantZeroAnalyzer : AlAnalyzer {
    private const string DiagnosticId = DiagnosticIds.DontDivideByConstantZero;

    private static readonly LocalizableResourceString Title = new(
        nameof(Resources.AL0003AnalyzerTitle), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableResourceString MessageFormat = new(
        nameof(Resources.AL0003AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableResourceString Description = new(
        nameof(Resources.AL0003AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId, Title, MessageFormat, DiagnosticCategories.Reliability,
        DiagnosticSeverity.Error, true, Description,
        HelpLinkBase);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterOperationAction(BinaryOperationAction, OperationKind.Binary);

    private static void BinaryOperationAction(OperationAnalysisContext context) {
        var operation = (IBinaryOperation)context.Operation;

        if (operation.OperatorKind is not (BinaryOperatorKind.Divide or BinaryOperatorKind.Remainder)) {
            return;
        }

        var leftType = operation.LeftOperand.Type;
        if (leftType is null || !IsIntegerOrDecimalType(leftType)) {
            return;
        }

        var rightConstant = operation.RightOperand.ConstantValue;
        if (!rightConstant.HasValue || !IsZero(rightConstant.Value)) {
            return;
        }

        context.ReportDiagnostic(Rule, operation.Syntax.GetLocation());
    }

    private static bool IsIntegerOrDecimalType(ITypeSymbol typeSymbol) {
        if (typeSymbol.SpecialType is
            SpecialType.System_Byte or SpecialType.System_SByte or
            SpecialType.System_Int16 or SpecialType.System_UInt16 or
            SpecialType.System_Int32 or SpecialType.System_UInt32 or
            SpecialType.System_Int64 or SpecialType.System_UInt64 or
            SpecialType.System_IntPtr or SpecialType.System_UIntPtr or
            SpecialType.System_Decimal) {
            return true;
        }

        var fullName = typeSymbol.ToDisplayString();
        return fullName is "System.Int128" or "System.UInt128";
    }

    private static bool IsZero(object? value) =>
        value switch {
            0 or 0u or 0L or 0ul => true,
            byte b => b is 0,
            sbyte sb => sb is 0,
            short s => s is 0,
            ushort us => us is 0,

            decimal d => d is 0m,

            nint n => n is 0,
            nuint nu => nu is 0,
            _ => false
        };
}
