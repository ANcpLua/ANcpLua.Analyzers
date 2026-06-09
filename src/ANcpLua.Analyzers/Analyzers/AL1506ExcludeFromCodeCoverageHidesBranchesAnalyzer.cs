namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL1506: <c>[ExcludeFromCodeCoverage]</c> applied to code that contains branching logic
///     without recording a <c>Justification</c>.
/// </summary>
/// <remarks>
///     <para>
///         <c>[ExcludeFromCodeCoverage]</c> removes a symbol from coverage reports entirely, so any
///         <c>if</c>/<c>switch</c>/loop/<c>?:</c>/<c>catch</c> inside it counts as covered even though no
///         test ever executed it. That is the classic source of a misleading 100% line/branch rate — the
///         number reads green while real control flow has never run.
///     </para>
///     <para>
///         Legitimate exclusions exist — platform-specific P/Invoke, generated code, unreachable defensive
///         paths — but they should say <em>why</em>. When the attribute carries a non-empty
///         <c>Justification</c>, the exclusion is treated as a documented, auditable decision and is not
///         flagged. Otherwise the branching belongs under coverage and the attribute should be removed.
///     </para>
///     <para>
///         Only branch-bearing symbols are flagged: a plain data type, an auto-property, or a straight-line
///         pass-through method carries no untested branches, so excluding it is harmless and silent.
///     </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al1506ExcludeFromCodeCoverageHidesBranchesAnalyzer : AlAnalyzer {
    /// <summary>The diagnostic identifier for AL1506.</summary>
    private const string DiagnosticId = "AL1506";

    private const string ExcludeFromCodeCoverageMetadataName =
        "System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute";

    private const string JustificationPropertyName = "Justification";

    private static readonly DiagnosticDescriptor s_rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.Design,
        DiagnosticSeverity.Info);

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_rule];

    /// <inheritdoc />
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterCompilationStartAction(OnCompilationStart);

    private static void OnCompilationStart(CompilationStartAnalysisContext context) {
        if (context.Compilation.GetTypeByMetadataName(ExcludeFromCodeCoverageMetadataName) is not { } attributeType) {
            return;
        }

        context.RegisterSymbolAction(
            ctx => AnalyzeSymbol(ctx, attributeType),
            SymbolKind.NamedType,
            SymbolKind.Method,
            SymbolKind.Property);
    }

    private static void AnalyzeSymbol(SymbolAnalysisContext context, INamedTypeSymbol attributeType) {
        var symbol = context.Symbol;
        if (symbol.IsImplicitlyDeclared) {
            return;
        }

        if (FindExclusionAttribute(symbol, attributeType) is not { ApplicationSyntaxReference: { } syntaxReference } attribute) {
            return;
        }

        if (HasJustification(attribute) || !DeclaresBranch(symbol)) {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            s_rule,
            syntaxReference.GetSyntax().GetLocation(),
            symbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
    }

    private static AttributeData? FindExclusionAttribute(ISymbol symbol, INamedTypeSymbol attributeType) {
        foreach (var attribute in symbol.GetAttributes()) {
            if (attribute.AttributeClass is { } attributeClass && attributeClass.IsEqualTo(attributeType)) {
                return attribute;
            }
        }

        return null;
    }

    private static bool HasJustification(AttributeData attribute) {
        foreach (var argument in attribute.NamedArguments) {
            if (argument.Key is JustificationPropertyName
                && argument.Value.Value is string justification
                && !string.IsNullOrWhiteSpace(justification)) {
                return true;
            }
        }

        return false;
    }

    private static bool DeclaresBranch(ISymbol symbol) {
        foreach (var reference in symbol.DeclaringSyntaxReferences) {
            foreach (var node in reference.GetSyntax().DescendantNodes()) {
                if (IsBranch(node)) {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool IsBranch(SyntaxNode node) => node.Kind() switch {
        SyntaxKind.IfStatement => true,
        SyntaxKind.SwitchStatement => true,
        SyntaxKind.SwitchExpression => true,
        SyntaxKind.ConditionalExpression => true,
        SyntaxKind.ForStatement => true,
        SyntaxKind.ForEachStatement => true,
        SyntaxKind.ForEachVariableStatement => true,
        SyntaxKind.WhileStatement => true,
        SyntaxKind.DoStatement => true,
        SyntaxKind.CatchClause => true,
        _ => false
    };
}
