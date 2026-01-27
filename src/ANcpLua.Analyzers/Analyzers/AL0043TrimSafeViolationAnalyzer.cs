using ANcpLua.Analyzers.Core;

namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0043: Code marked [TrimSafe] must not call methods with [RequiresUnreferencedCode].
/// </summary>
/// <remarks>
///     <para>
///         The [TrimSafe] attribute indicates that code is safe to use in trimmed applications.
///         Calling methods with [RequiresUnreferencedCode] violates this guarantee because those
///         methods may fail at runtime when types are trimmed away.
///     </para>
///     <para>
///         This analyzer checks both direct calls and transitive calls within the same compilation.
///         It reports warnings when [TrimSafe] code calls unsafe methods.
///     </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0043TrimSafeViolationAnalyzer : AlAnalyzer {
    private const string TrimSafeAttributeName = "TrimSafe";
    private const string RequiresUnreferencedCodeAttributeName = "RequiresUnreferencedCode";

    private static readonly LocalizableResourceString Title = new(
        nameof(Resources.AL0043AnalyzerTitle), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableResourceString MessageFormat = new(
        nameof(Resources.AL0043AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableResourceString Description = new(
        nameof(Resources.AL0043AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.TrimSafeViolation,
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

        // Check if the calling method is marked [TrimSafe]
        if (!callingMethod.HasAttributeByShortName(TrimSafeAttributeName)) {
            return;
        }

        var targetMethod = invocation.TargetMethod;

        // Check if the called method has [RequiresUnreferencedCode]
        if (!targetMethod.HasAttributeByShortName(RequiresUnreferencedCodeAttributeName)) {
            return;
        }

        // Report violation
        var callingMethodName = callingMethod.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        var targetMethodName = targetMethod.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

        context.ReportDiagnostic(Rule, invocation.Syntax.GetLocation(), callingMethodName, targetMethodName);
    }
}
