
namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0010: Type should be partial for source generator support.
/// </summary>
/// <remarks>
///     Disabled by default because flagging all non-partial types is aggressive.
///     Users can enable via .editorconfig: dotnet_diagnostic.AL0010.severity = suggestion
///     Useful for codebases that heavily rely on source generators.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0010PartialTypeAnalyzer : AlAnalyzer {
    /// <summary>The diagnostic identifier for AL0010.</summary>
    public const string DiagnosticId = "AL0010";

    private static readonly DiagnosticDescriptor Rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.Design,
        DiagnosticSeverity.Info,
        isEnabledByDefault: false);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    /// <summary>Registers syntax node actions to analyze type declarations.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterSyntaxNodeAction(AnalyzeTypeDeclaration,
            SyntaxKind.ClassDeclaration,
            SyntaxKind.StructDeclaration,
            SyntaxKind.RecordDeclaration,
            SyntaxKind.RecordStructDeclaration);

    private static void AnalyzeTypeDeclaration(SyntaxNodeAnalysisContext context) {
        var typeDeclaration = (TypeDeclarationSyntax)context.Node;

        if (typeDeclaration.Modifiers.Any(SyntaxKind.PartialKeyword)) {
            return;
        }

        context.ReportDiagnostic(Rule,
            typeDeclaration.Identifier.GetLocation(),
            typeDeclaration.Identifier.Text);
    }
}
