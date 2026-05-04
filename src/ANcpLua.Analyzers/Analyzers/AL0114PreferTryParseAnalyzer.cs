
namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0114: Detects calls to <c>Parse</c> on numeric, date/time, and other types that throw on invalid input.
/// </summary>
/// <remarks>
///     <para>
///         Methods like <c>int.Parse</c>, <c>DateTime.Parse</c>, and <c>Guid.Parse</c> throw
///         <see cref="FormatException"/> (or <see cref="OverflowException"/>) when the input string
///         is not valid. In code that processes user input or external data, this is a reliability risk.
///     </para>
///     <para>
///         Each of these types provides a <c>TryParse</c> alternative that returns a boolean instead
///         of throwing, enabling safe handling of invalid input without exception overhead.
///     </para>
///     <para>
///         The analyzer suppresses the diagnostic when the <c>Parse</c> call is already inside a
///         <c>try</c> block whose <c>catch</c> clause handles <see cref="FormatException"/> (or a
///         bare <c>catch</c>), since the developer has explicitly opted into exception-based handling.
///     </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0114PreferTryParseAnalyzer : AlAnalyzer {
    /// <summary>The diagnostic identifier for AL0114.</summary>
    private const string DiagnosticId = "AL0114";

    private static readonly DiagnosticDescriptor s_rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.Reliability,
        DiagnosticSeverity.Warning);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_rule];

    /// <summary>Registers an operation action to detect Parse invocations on known types.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);

    private static void AnalyzeInvocation(OperationAnalysisContext context) {
        if (context.Operation is not IInvocationOperation invocation) {
            return;
        }

        var targetMethod = invocation.TargetMethod;
        if (targetMethod.Name != "Parse" || targetMethod.ContainingType is not { } containingType) {
            return;
        }

        if (!IsKnownParseType(containingType)) {
            return;
        }

        if (IsInsideTryCatchForFormatException(invocation.Syntax)) {
            return;
        }

        var typeName = containingType.SpecialType switch {
            SpecialType.System_Int32 => "int",
            SpecialType.System_Int64 => "long",
            SpecialType.System_Single => "float",
            SpecialType.System_Double => "double",
            SpecialType.System_Decimal => "decimal",
            _ => containingType.Name
        };

        context.ReportDiagnostic(s_rule, invocation.Syntax.GetLocation(), typeName);
    }

    private static bool IsKnownParseType(INamedTypeSymbol type) =>
        type.SpecialType is
            SpecialType.System_Int32 or
            SpecialType.System_Int64 or
            SpecialType.System_Single or
            SpecialType.System_Double or
            SpecialType.System_Decimal
        || type.ToDisplayString() is
            "System.DateTime" or
            "System.DateTimeOffset" or
            "System.Guid" or
            "System.Enum";

    private static bool IsInsideTryCatchForFormatException(SyntaxNode node) {
        for (var current = node.Parent; current is not null; current = current.Parent) {
            if (current is not TryStatementSyntax tryStatement) {
                continue;
            }

            foreach (var catchClause in tryStatement.Catches) {
                // Bare catch {} or catch (Exception) {} — catches everything
                if (catchClause.Declaration is null) {
                    return true;
                }

                var caughtTypeName = catchClause.Declaration.Type.ToString();
                if (caughtTypeName is "FormatException" or "System.FormatException" or "Exception" or "System.Exception") {
                    return true;
                }
            }
        }

        return false;
    }
}
