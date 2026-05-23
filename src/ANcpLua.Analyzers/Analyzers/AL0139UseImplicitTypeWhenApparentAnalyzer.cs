namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0139: Use <c>var</c> when the initializer makes the type apparent.
/// </summary>
/// <remarks>
///     Integrated into the ANcpLua analyzer catalog as a conservative apparent-type style rule.
///     The rule intentionally follows the conservative dotnet/runtime-style apparent-type heuristic:
///     literals, casts, <c>as</c>, <c>default(T)</c>, object creation, and array creation are apparent;
///     lambdas, delegates, method groups, null, dynamic conversions, and multi-declarator declarations
///     stay explicit.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0139UseImplicitTypeWhenApparentAnalyzer : AlAnalyzer {
    /// <summary>AL0139: Use implicit type when the type is apparent.</summary>
    public const string DiagnosticId = "AL0139";

    private static readonly DiagnosticDescriptor s_rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.Style,
        DiagnosticSeverities.Suggestion);

    /// <summary>Gets the diagnostic descriptor for AL0139.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_rule];

    /// <summary>Registers syntax actions for local variable typing.</summary>
    protected override void RegisterActions(AnalysisContext context) {
        context.RegisterSyntaxNodeAction(AnalyzeVariableDeclaration, SyntaxKind.VariableDeclaration);
    }

    private static void AnalyzeVariableDeclaration(SyntaxNodeAnalysisContext context) {
        var declaration = (VariableDeclarationSyntax)context.Node;

        if (declaration.Type.IsVar) {
            return;
        }

        if (!ShouldAnalyze(declaration, out var initializer)) {
            return;
        }

        var typeInfo = context.SemanticModel.GetTypeInfo(initializer.Value, context.CancellationToken);
        var shouldUseImplicitType = ShouldUseImplicitType(
            declaration,
            initializer,
            typeInfo,
            context.SemanticModel);

        if (shouldUseImplicitType) {
            context.ReportDiagnostic(Diagnostic.Create(s_rule, declaration.Type.GetLocation()));
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
