
namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0128: Detects Loom tools with destructive side effects that do not require approval.
/// </summary>
/// <remarks>
///     <para>
///         When a Loom tool has a destructive side effect (WritesExternalState, MutatesCode,
///         Deploys, or ClosesIssue), it must also declare [RequiresApproval] to ensure
///         governance gates are in place before execution.
///     </para>
///     <para>
///         The ToolSideEffect enum values considered destructive are:
///         <list type="bullet">
///             <item>WritesExternalState (2)</item>
///             <item>MutatesCode (3)</item>
///             <item>Deploys (4)</item>
///             <item>ClosesIssue (5)</item>
///         </list>
///     </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0128DestructiveToolMustRequireApprovalAnalyzer : AlAnalyzer {
    /// <summary>The diagnostic identifier for AL0128.</summary>
    private const string DiagnosticId = "AL0128";

    private const string LoomToolAttributeName = "LoomTool";
    private const string ToolSideEffectAttributeName = "ToolSideEffect";
    private const string RequiresApprovalAttributeName = "RequiresApproval";

    /// <summary>Minimum enum value that is considered destructive (WritesExternalState = 2).</summary>
    private const int DestructiveSideEffectThreshold = 2;

    private static readonly string[] s_destructiveSideEffectNames = [
        "WritesExternalState",
        "MutatesCode",
        "Deploys",
        "ClosesIssue"
    ];

    private static readonly DiagnosticDescriptor s_rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.GenAI,
        DiagnosticSeverities.Suggestion);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_rule];

    /// <summary>Registers symbol actions to analyze methods with [LoomTool] for missing [RequiresApproval].</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterSymbolAction(AnalyzeMethod, SymbolKind.Method);

    private static void AnalyzeMethod(SymbolAnalysisContext context) {
        var method = (IMethodSymbol)context.Symbol;

        if (!method.HasAttributeByShortName(LoomToolAttributeName)) {
            return;
        }

        if (HasAttributeOnMethodOrType(method, RequiresApprovalAttributeName)) {
            return;
        }

        if (GetDestructiveSideEffectName(method) is not { } destructiveSideEffect) {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            s_rule,
            method.Locations.FirstOrDefault() ?? Location.None,
            method.Name,
            destructiveSideEffect));
    }

    private static string? GetDestructiveSideEffectName(IMethodSymbol method) {
        var result = FindDestructiveSideEffectInAttributes(method.GetAttributes());

        return result ?? FindDestructiveSideEffectInAttributes(method.ContainingType?.GetAttributes());
    }

    private static string? FindDestructiveSideEffectInAttributes(ImmutableArray<AttributeData>? attributes) {
        if (attributes is not { } attrs) {
            return null;
        }

        foreach (var attribute in attrs) {
            if (attribute.AttributeClass?.Name is not ToolSideEffectAttributeName and
                not "ToolSideEffectAttribute") {
                continue;
            }

            if (attribute.ConstructorArguments is [{ Value: int enumValue and >= DestructiveSideEffectThreshold }, ..]) {
                return enumValue < DestructiveSideEffectThreshold + s_destructiveSideEffectNames.Length
                    ? s_destructiveSideEffectNames[enumValue - DestructiveSideEffectThreshold]
                    : enumValue.ToString();
            }
        }

        return null;
    }

    private static bool HasAttributeOnMethodOrType(IMethodSymbol method, string attributeName) =>
        method.HasAttributeByShortName(attributeName) ||
        method.ContainingType?.HasAttributeByShortName(attributeName) is true;
}
