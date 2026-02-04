using ANcpLua.Analyzers.Core;

namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0041: Methods decorated with [AotTest] or [TrimTest] must return int.
/// </summary>
/// <remarks>
///     <para>
///         The AOT/Trim testing framework expects test methods to return an integer
///         exit code to indicate success or failure. Methods with [AotTest] or [TrimTest]
///         attributes must have a return type of <c>int</c>.
///     </para>
///     <para>
///         This analyzer detects by attribute name string matching ("AotTest" or "TrimTest")
///         to avoid requiring a package reference to ANcpLua.AotTesting.Attributes.
///     </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0041AotTestMustReturnIntAnalyzer : AlAnalyzer {
    private const string AotTestAttributeName = "AotTest";
    private const string TrimTestAttributeName = "TrimTest";

    private static readonly DiagnosticDescriptor Rule = CreateRule(
        DiagnosticIds.AotTestMustReturnInt,
        DiagnosticCategories.AotTesting,
        DiagnosticSeverity.Error);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    /// <summary>Registers syntax or operation actions for analysis.</summary>

    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterCompilationStartAction(OnCompilationStart);

    private static void OnCompilationStart(CompilationStartAnalysisContext context) {
        if (context.Compilation.GetSpecialType(SpecialType.System_Int32) is not { } intType) {
            return;
        }

        context.RegisterSymbolAction(
            ctx => AnalyzeMethod(ctx, intType),
            SymbolKind.Method);
    }

    private static void AnalyzeMethod(SymbolAnalysisContext context, INamedTypeSymbol intType) {
        if (context.Symbol is not IMethodSymbol method) {
            return;
        }

        // Check for AotTest or TrimTest attribute
        string? attributeName = null;
        if (method.HasAttributeByShortName(AotTestAttributeName)) {
            attributeName = AotTestAttributeName;
        } else if (method.HasAttributeByShortName(TrimTestAttributeName)) {
            attributeName = TrimTestAttributeName;
        }

        if (attributeName is null) {
            return;
        }

        // Check if return type is int
        if (method.ReturnType.IsEqualTo(intType)) {
            return;
        }

        var returnTypeName = method.ReturnType.ToDisplayString();
        context.ReportDiagnostic(Rule, method.Locations[0], method.Name, attributeName, returnTypeName);
    }
}
