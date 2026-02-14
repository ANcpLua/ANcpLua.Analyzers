
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
public sealed partial class Al0006FieldNameConflictWithPrimaryConstructorAnalyzer : AlAnalyzer {
    /// <summary>The diagnostic identifier for AL0006.</summary>
    public const string DiagnosticId = "AL0006";

    private static readonly DiagnosticDescriptor Rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.Design,
        DiagnosticSeverity.Warning);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    /// <summary>Registers syntax node actions to analyze field declarations for naming conflicts.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterSyntaxNodeAction(SyntaxNodeAction, SyntaxKind.FieldDeclaration);

    private static void SyntaxNodeAction(SyntaxNodeAnalysisContext context) {
        var member = (FieldDeclarationSyntax)context.Node;

        if (member.Parent is not TypeDeclarationSyntax { ParameterList: { } parameterList }) {
            return;
        }

        var parameterNames = new HashSet<string>(
            parameterList.Parameters.Select(static p => p.Identifier.ValueText),
            StringComparer.Ordinal);

        foreach (var variable in member.Declaration.Variables) {
            var identifier = variable.Identifier;
            if (parameterNames.Contains(identifier.ValueText)) {
                context.ReportDiagnostic(Rule, identifier.GetLocation(), identifier);
            }
        }
    }
}
