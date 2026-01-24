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

    private static readonly LocalizableResourceString Title = new(
        nameof(Resources.AL0041AnalyzerTitle), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableResourceString MessageFormat = new(
        nameof(Resources.AL0041AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableResourceString Description = new(
        nameof(Resources.AL0041AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.AotTestMustReturnInt,
        Title, MessageFormat, DiagnosticCategories.AotTesting,
        DiagnosticSeverity.Error, true, Description,
        HelpLinkBase);

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

        // Skip non-test methods
        if (GetAotOrTrimTestAttribute(method) is not { } attribute) {
            return;
        }

        // Check if return type is int
        if (method.ReturnType.IsEqualTo(intType)) {
            return;
        }

        var attributeName = attribute.AttributeClass?.Name ?? "Unknown";
        var returnTypeName = method.ReturnType.ToDisplayString();
        context.ReportDiagnostic(Rule, method.Locations[0], method.Name, attributeName, returnTypeName);
    }

    private static AttributeData? GetAotOrTrimTestAttribute(IMethodSymbol method) {
        foreach (var attribute in method.GetAttributes()) {
            var attributeName = attribute.AttributeClass?.Name;
            if (attributeName == AotTestAttributeName ||
                attributeName == TrimTestAttributeName ||
                attributeName == $"{AotTestAttributeName}Attribute" ||
                attributeName == $"{TrimTestAttributeName}Attribute") {
                return attribute;
            }
        }

        return null;
    }
}
