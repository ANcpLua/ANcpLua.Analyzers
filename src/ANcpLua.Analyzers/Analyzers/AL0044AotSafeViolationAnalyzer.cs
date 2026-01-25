using ANcpLua.Analyzers.Core;

namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0044: Code marked [AotSafe] must not call methods with [RequiresDynamicCode].
/// </summary>
/// <remarks>
///     <para>
///         The [AotSafe] attribute indicates that code is safe to use in AOT-compiled applications.
///         Calling methods with [RequiresDynamicCode] violates this guarantee because those methods
///         rely on runtime code generation which is not available in AOT scenarios.
///     </para>
///     <para>
///         This analyzer checks both direct calls and transitive calls within the same compilation.
///         It reports warnings when [AotSafe] code calls methods that require dynamic code generation.
///     </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0044AotSafeViolationAnalyzer : AlAnalyzer {
    private const string AotSafeAttributeName = "AotSafe";
    private const string RequiresDynamicCodeAttributeName = "RequiresDynamicCode";

    private static readonly LocalizableResourceString Title = new(
        nameof(Resources.AL0044AnalyzerTitle), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableResourceString MessageFormat = new(
        nameof(Resources.AL0044AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableResourceString Description = new(
        nameof(Resources.AL0044AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.AotSafeViolation,
        Title, MessageFormat, DiagnosticCategories.AotTesting,
        DiagnosticSeverity.Warning, true, Description,
        HelpLinkBase);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    /// <summary>Registers syntax or operation actions for analysis.</summary>

    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterCompilationStartAction(OnCompilationStart);

    private static void OnCompilationStart(CompilationStartAnalysisContext context) {
        context.RegisterOperationAction(
            AnalyzeInvocation,
            OperationKind.Invocation);
    }

    private static void AnalyzeInvocation(OperationAnalysisContext context) {
        var invocation = (IInvocationOperation)context.Operation;

        if (context.ContainingSymbol is not IMethodSymbol callingMethod) {
            return;
        }

        // Check if the calling method is marked [AotSafe]
        if (!HasAttribute(callingMethod, AotSafeAttributeName)) {
            return;
        }

        var targetMethod = invocation.TargetMethod;

        // Check if the called method has [RequiresDynamicCode]
        if (!HasAttribute(targetMethod, RequiresDynamicCodeAttributeName)) {
            return;
        }

        // Report violation
        var callingMethodName = callingMethod.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        var targetMethodName = targetMethod.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

        context.ReportDiagnostic(
            Rule,
            invocation.Syntax.GetLocation(),
            callingMethodName,
            targetMethodName);
    }

    private static bool HasAttribute(ISymbol symbol, string attributeName) {
        foreach (var attribute in symbol.GetAttributes()) {
            var name = attribute.AttributeClass?.Name;
            if (name == attributeName || name == $"{attributeName}Attribute") {
                return true;
            }
        }

        return false;
    }
}
