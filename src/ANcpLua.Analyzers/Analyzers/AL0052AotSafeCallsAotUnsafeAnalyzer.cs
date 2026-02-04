using ANcpLua.Analyzers.Core;

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
    private const string AotSafeAttributeName = "AotSafe";
    private const string AotUnsafeAttributeName = "AotUnsafe";

    private static readonly DiagnosticDescriptor Rule = CreateRule(
        DiagnosticIds.AotSafeCallsAotUnsafe,
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

        if (context.ContainingSymbol is not IMethodSymbol callingMethod) {
            return;
        }

        // Check if the calling method or its containing type is marked [AotSafe]
        if (!IsAotSafe(callingMethod)) {
            return;
        }

        var targetMethod = invocation.TargetMethod;

        // Check if the called method or its containing type is marked [AotUnsafe]
        if (!IsAotUnsafe(targetMethod)) {
            return;
        }

        // Report violation
        var callingMethodName = callingMethod.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        var targetMethodName = targetMethod.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

        context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.Syntax.GetLocation(), callingMethodName, targetMethodName));
    }

    private static bool IsAotSafe(IMethodSymbol method) {
        // Check method itself
        if (method.HasAttributeByShortName(AotSafeAttributeName)) {
            return true;
        }

        // Check containing type
        var containingType = method.ContainingType;
        while (containingType is not null) {
            if (containingType.HasAttributeByShortName(AotSafeAttributeName)) {
                return true;
            }
            containingType = containingType.ContainingType;
        }

        return false;
    }

    private static bool IsAotUnsafe(IMethodSymbol method) {
        // Check method itself
        if (method.HasAttributeByShortName(AotUnsafeAttributeName)) {
            return true;
        }

        // Check containing type
        var containingType = method.ContainingType;
        while (containingType is not null) {
            if (containingType.HasAttributeByShortName(AotUnsafeAttributeName)) {
                return true;
            }
            containingType = containingType.ContainingType;
        }

        return false;
    }
}
