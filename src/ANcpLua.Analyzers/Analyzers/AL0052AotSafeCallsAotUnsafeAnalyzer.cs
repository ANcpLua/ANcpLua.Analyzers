
namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0052: [AotSafe] code must not call [AotUnsafe] code.
/// </summary>
/// <remarks>
///     <para>
///         The [AotSafe] attribute indicates that code is safe to use in AOT-compiled applications.
///         Calling methods marked with [AotUnsafe] violates this guarantee because those methods
///         explicitly declare they require JIT compilation.
///     </para>
///     <para>
///         This analyzer complements AL0044 which checks for [RequiresDynamicCode] calls.
///         While AL0044 catches BCL-annotated unsafe methods, this analyzer catches user-defined
///         unsafe code marked with [AotUnsafe].
///     </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0052AotSafeCallsAotUnsafeAnalyzer : AlAnalyzer {
    /// <summary>The diagnostic identifier for AL0052.</summary>
    private const string DiagnosticId = "AL0052";

    private const string AotSafeAttributeName = "AotSafe";
    private const string AotUnsafeAttributeName = "AotUnsafe";

    private static readonly DiagnosticDescriptor Rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.AotTesting,
        DiagnosticSeverities.RequiredFix);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    /// <summary>Registers syntax or operation actions for analysis.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterCompilationStartAction(OnCompilationStart);

    private static void OnCompilationStart(CompilationStartAnalysisContext context) =>
        context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);

    private static void AnalyzeInvocation(OperationAnalysisContext context) {
        var invocation = (IInvocationOperation)context.Operation;

        if (context.ContainingSymbol is not IMethodSymbol callingMethod ||
            !HasAttribute(callingMethod, AotSafeAttributeName) ||
            !HasAttribute(invocation.TargetMethod, AotUnsafeAttributeName)) {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            Rule,
            invocation.Syntax.GetLocation(),
            callingMethod.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
            invocation.TargetMethod.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
    }

    private static bool HasAttribute(IMethodSymbol method, string attributeName) {
        if (method.HasAttributeByShortName(attributeName)) {
            return true;
        }

        for (var type = method.ContainingType; type is not null; type = type.ContainingType) {
            if (type.HasAttributeByShortName(attributeName)) {
                return true;
            }
        }

        return false;
    }
}
