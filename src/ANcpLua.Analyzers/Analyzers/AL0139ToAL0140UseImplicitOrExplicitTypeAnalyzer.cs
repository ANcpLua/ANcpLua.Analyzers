namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0139: Use <c>var</c> when the initializer makes the type apparent.
///     AL0140: Use an explicit type when <c>var</c> would hide the type.
/// </summary>
/// <remarks>
///     Integrated into the ANcpLua analyzer catalog as a conservative apparent-type style rule.
///     The rule intentionally follows the conservative dotnet/runtime-style apparent-type heuristic:
///     literals, casts, <c>as</c>, <c>default(T)</c>, object creation, and array creation are apparent;
///     foreach variables, out variables, lambdas, delegates, method groups, null, dynamic conversions,
///     and multi-declarator declarations stay explicit.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0139ToAl0140UseImplicitOrExplicitTypeAnalyzer : AlAnalyzer {
    /// <summary>AL0139: Use implicit type when the type is apparent.</summary>
    public const string DiagnosticIdAl0139 = "AL0139";
    /// <summary>AL0140: Use explicit type when the type is not apparent.</summary>
    public const string DiagnosticIdAl0140 = "AL0140";

    private static readonly DiagnosticDescriptor s_ruleAl0139 = CreateRule(
        DiagnosticIdAl0139,
        DiagnosticCategories.Style,
        DiagnosticSeverities.Suggestion);

    private static readonly DiagnosticDescriptor s_ruleAl0140 = CreateRule(
        DiagnosticIdAl0140,
        DiagnosticCategories.Style,
        DiagnosticSeverities.Suggestion);

    /// <summary>Gets the diagnostic descriptors for AL0139 and AL0140.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [s_ruleAl0139, s_ruleAl0140];

    /// <summary>Registers syntax actions for local, foreach, and declaration-expression typing.</summary>
    protected override void RegisterActions(AnalysisContext context) {
        context.RegisterSyntaxNodeAction(AnalyzeVariableDeclaration, SyntaxKind.VariableDeclaration);
        context.RegisterSyntaxNodeAction(AnalyzeDeclarationExpression, SyntaxKind.DeclarationExpression);
        context.RegisterSyntaxNodeAction(AnalyzeForEachStatement, SyntaxKind.ForEachStatement);
    }

    private static void AnalyzeVariableDeclaration(SyntaxNodeAnalysisContext context) {
        var declaration = (VariableDeclarationSyntax)context.Node;

        if (!ShouldAnalyze(declaration, out var initializer)) {
            return;
        }

        var typeInfo = context.SemanticModel.GetTypeInfo(initializer.Value, context.CancellationToken);
        var shouldUseImplicitType = ShouldUseImplicitType(
            declaration,
            initializer,
            typeInfo,
            context.SemanticModel);

        if (declaration.Type.IsVar && !shouldUseImplicitType) {
            context.ReportDiagnostic(Diagnostic.Create(s_ruleAl0140, declaration.Type.GetLocation()));
            return;
        }

        if (!declaration.Type.IsVar && shouldUseImplicitType) {
            context.ReportDiagnostic(Diagnostic.Create(s_ruleAl0139, declaration.Type.GetLocation()));
        }
    }

    private static void AnalyzeDeclarationExpression(SyntaxNodeAnalysisContext context) {
        var declaration = (DeclarationExpressionSyntax)context.Node;

        if (declaration is { Type.IsVar: true, Designation: SingleVariableDesignationSyntax }) {
            context.ReportDiagnostic(Diagnostic.Create(s_ruleAl0140, declaration.Type.GetLocation()));
        }
    }

    private static void AnalyzeForEachStatement(SyntaxNodeAnalysisContext context) {
        var foreachStatement = (ForEachStatementSyntax)context.Node;

        if (foreachStatement.Type.IsVar) {
            context.ReportDiagnostic(Diagnostic.Create(s_ruleAl0140, foreachStatement.Type.GetLocation()));
        }
    }

    private static bool ShouldAnalyze(
        VariableDeclarationSyntax declaration,
        out EqualsValueClauseSyntax initializer) {
        initializer = null!;

        if (declaration.DescendantNodesAndTokensAndSelf()
            .Any(static nodeOrToken => nodeOrToken.GetDiagnostics()
                .Any(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error))) {
            return false;
        }

        if (declaration.Parent is LocalDeclarationStatementSyntax { IsConst: true }) {
            return false;
        }

        if (declaration.Parent is FieldDeclarationSyntax or EventFieldDeclarationSyntax) {
            return false;
        }

        if (declaration.Variables.Count != 1) {
            return false;
        }

        var declarator = declaration.Variables[0];
        if (declarator.Initializer is not { } declaratorInitializer) {
            return false;
        }

        initializer = declaratorInitializer;
        return true;
    }

    private static bool ShouldUseImplicitType(
        VariableDeclarationSyntax declaration,
        EqualsValueClauseSyntax initializer,
        TypeInfo typeInfo,
        SemanticModel model) {
        if (!SymbolEqualityComparer.Default.Equals(typeInfo.Type, typeInfo.ConvertedType)) {
            return false;
        }

        if (!model.LookupSymbols(declaration.Type.SpanStart, name: "var").IsEmpty &&
            typeInfo.Type?.Name != "var") {
            return false;
        }

        return initializer.Value.Kind() is
            SyntaxKind.StringLiteralExpression or
            SyntaxKind.InterpolatedStringExpression or
            SyntaxKind.NumericLiteralExpression or
            SyntaxKind.CastExpression or
            SyntaxKind.AsExpression or
            SyntaxKind.DefaultExpression or
            SyntaxKind.ObjectCreationExpression or
            SyntaxKind.ArrayCreationExpression or
            SyntaxKind.ImplicitArrayCreationExpression;
    }
}
