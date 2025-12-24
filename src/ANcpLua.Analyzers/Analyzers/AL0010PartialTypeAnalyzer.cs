using ANcpLua.Analyzers.Core;

namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
/// AL0010: Type should be partial for source generator support.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AL0010PartialTypeAnalyzer : ALAnalyzer
{
    private static readonly LocalizableResourceString Title = new(
        nameof(Resources.AL0010AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
    private static readonly LocalizableResourceString MessageFormat = new(
        nameof(Resources.AL0010AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
    private static readonly LocalizableResourceString Description = new(
        nameof(Resources.AL0010AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.TypeShouldBePartial,
        Title, MessageFormat, DiagnosticCategories.Design,
        DiagnosticSeverity.Info, isEnabledByDefault: false, Description,
        HelpLinkBase + "AL0010.md");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    protected override void RegisterActions(AnalysisContext context)
    {
        context.RegisterSyntaxNodeAction(AnalyzeTypeDeclaration,
            SyntaxKind.ClassDeclaration,
            SyntaxKind.StructDeclaration,
            SyntaxKind.RecordDeclaration,
            SyntaxKind.RecordStructDeclaration);
    }

    private static void AnalyzeTypeDeclaration(SyntaxNodeAnalysisContext context)
    {
        var typeDeclaration = (TypeDeclarationSyntax)context.Node;

        // Already partial - nothing to report
        if (typeDeclaration.Modifiers.Any(SyntaxKind.PartialKeyword))
            return;

        context.ReportDiagnostic(Rule,
            typeDeclaration.Identifier.GetLocation(),
            typeDeclaration.Identifier.Text);
    }
}
