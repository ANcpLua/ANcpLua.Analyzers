using ANcpLua.Analyzers.Core;

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
    private static readonly LocalizableResourceString Title = new(
        nameof(Resources.AL0010AnalyzerTitle), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableResourceString MessageFormat = new(
        nameof(Resources.AL0010AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableResourceString Description = new(
        nameof(Resources.AL0010AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.TypeShouldBePartial,
        Title, MessageFormat, DiagnosticCategories.Design,
        DiagnosticSeverity.Info, false, Description,
        HelpLinkBase);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

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
