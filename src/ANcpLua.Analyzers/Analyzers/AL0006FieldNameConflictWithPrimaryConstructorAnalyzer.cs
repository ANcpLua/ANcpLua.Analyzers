using ANcpLua.Analyzers.Core;

namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0006: Field names should not conflict with primary constructor parameters.
/// </summary>
/// <remarks>
///     <para>
///         When a field has the same name as a primary constructor parameter, the field
///         shadows the parameter within its scope. This creates ambiguity about which
///         symbol is being referenced and can lead to subtle bugs where developers think
///         they are using the parameter but are actually using the field, or vice versa.
///     </para>
///     <para>
///         Unlike traditional constructors where parameters are only visible within the
///         constructor body, primary constructor parameters are captured and available
///         throughout the entire type. This extended visibility makes naming conflicts
///         particularly problematic.
///     </para>
///     <para>
///         The analyzer performs case-sensitive matching. Fields with names that differ
///         only in casing from constructor parameters are not flagged, though such naming
///         may still be confusing.
///     </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AL0006FieldNameConflictWithPrimaryConstructorAnalyzer : ALAnalyzer {
    private static readonly LocalizableResourceString Title = new(
        nameof(Resources.AL0006AnalyzerTitle), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableResourceString MessageFormat = new(
        nameof(Resources.AL0006AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableResourceString Description = new(
        nameof(Resources.AL0006AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.FieldNameConflictsWithPrimaryConstructorParameter,
        Title, MessageFormat, DiagnosticCategories.Design,
        DiagnosticSeverity.Warning, true, Description,
        HelpLinkBase);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterSyntaxNodeAction(SyntaxNodeAction, SyntaxKind.FieldDeclaration);

    private static void SyntaxNodeAction(SyntaxNodeAnalysisContext context) {
        var member = (FieldDeclarationSyntax)context.Node;

        if (member.Parent is not TypeDeclarationSyntax { ParameterList: { } parameterList }) {
            return;
        }

        var parameterNames = new HashSet<string>(
            parameterList.Parameters.Select(p => p.Identifier.ValueText),
            StringComparer.Ordinal);

        foreach (var variable in member.Declaration.Variables) {
            var identifier = variable.Identifier;
            if (parameterNames.Contains(identifier.ValueText)) {
                context.ReportDiagnostic(Rule, identifier.GetLocation(), identifier);
            }
        }
    }
}
