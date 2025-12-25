using ANcpLua.Analyzers.Core;

namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0006: Field names should not conflict with primary constructor parameters.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AL0006FieldNameConflictWithPrimaryConstructorAnalyzer : ALAnalyzer
{
    private static readonly LocalizableResourceString Title = new(
        nameof(Resources.AL0006AnalyzerTitle), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableResourceString MessageFormat = new(
        nameof(Resources.AL0006AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableResourceString Description = new(
        nameof(Resources.AL0006AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.FieldNameConflictsWithPrimaryConstructorParameter,
        Title, MessageFormat, DiagnosticCategories.Design,
        DiagnosticSeverity.Warning, isEnabledByDefault: true, Description,
        HelpLinkBase + "AL0006.md");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    protected override void RegisterActions(AnalysisContext context)
    {
        context.RegisterSyntaxNodeAction(SyntaxNodeAction, SyntaxKind.FieldDeclaration);
    }

    private static void SyntaxNodeAction(SyntaxNodeAnalysisContext context)
    {
        var member = (FieldDeclarationSyntax)context.Node;

        if (member.Parent is not TypeDeclarationSyntax { ParameterList: { } parameterList })
            return;

        var parameterNames = new HashSet<string>(
            parameterList.Parameters.Select(p => p.Identifier.ValueText),
            StringComparer.Ordinal);

        foreach (var variable in member.Declaration.Variables)
        {
            var identifier = variable.Identifier;
            if (parameterNames.Contains(identifier.ValueText))
                context.ReportDiagnostic(Rule, identifier.GetLocation(), identifier);
        }
    }
}
