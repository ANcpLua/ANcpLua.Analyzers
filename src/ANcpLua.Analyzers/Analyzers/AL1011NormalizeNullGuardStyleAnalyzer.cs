
namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL1011: Normalize null-guard style.
///     Enforces consistent use of Throw.IfNull, ThrowIfNull, or portable coalesce-throw patterns.
///     Priority: throw (Throw.IfNull) > bcl (ThrowIfNull) > portable (coalesce)
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al1011NormalizeNullGuardStyleAnalyzer : AlAnalyzer {
    /// <summary>AL1011: Normalize null-guard style.</summary>
    public const string DiagnosticId = "AL1011";

    /// <summary>Property key for the parameter identifier.</summary>
    public const string PropertyIdentifier = "Id";
    /// <summary>Property key for the exception type name.</summary>
    public const string PropertyTypeName = "Type";
    /// <summary>Property key for the target null-guard style.</summary>
    public const string PropertyStyle = "Style";

    private static readonly DiagnosticDescriptor s_rule = new(
        DiagnosticId,
        "Normalize null-guard style",
        "Normalize null-guard to '{0}' style",
        DiagnosticCategories.Style,
        DiagnosticSeverity.Info,
        true,
        "Null-guards should be normalized to the preferred project style (Throw, BCL, or Portable).",
        RuleDocs.HelpLinkAuto(DiagnosticId));

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_rule];

    /// <summary>Registers compilation start action to analyze if statements with null-guards.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterCompilationStartAction(OnCompilationStart);

    private static void OnCompilationStart(CompilationStartAnalysisContext context) {
        var hasThrowHelper = context.Compilation
            .GetTypeByMetadataName("Microsoft.Shared.Diagnostics.Throw")
            ?.GetMembers("IfNull")
            .OfType<IMethodSymbol>()
            .Any(static m => m is { IsStatic: true, Parameters.Length: >= 1 }) ?? false;

        var hasThrowIfNullBcl = context.Compilation
            .GetTypeByMetadataName("System.ArgumentNullException")
            ?.GetMembers("ThrowIfNull")
            .OfType<IMethodSymbol>()
            .Any(static m => m is { IsStatic: true, Parameters.Length: >= 1 }) ?? false;

        var globalOptions = context.Options.AnalyzerConfigOptionsProvider.GlobalOptions;
        var isMultiTarget = globalOptions.TryGetValue("build_property.TargetFrameworks", out var tfms)
                            && !string.IsNullOrWhiteSpace(tfms)
                            && tfms.ContainsOrdinal(";");

        context.RegisterSyntaxNodeAction(
            ctx => AnalyzeIfStatement(ctx, hasThrowHelper, hasThrowIfNullBcl, isMultiTarget),
            SyntaxKind.IfStatement);
    }

    private static void AnalyzeIfStatement(
        SyntaxNodeAnalysisContext context,
        bool hasThrowHelper,
        bool hasThrowIfNullBcl,
        bool isMultiTargetGlobal) {
        var ifStatement = (IfStatementSyntax)context.Node;

        if (!TryParseNullCheck(ifStatement.Condition, out var identifierNode)) {
            return;
        }

        if (TryGetThrowStatement(ifStatement.Statement) is not { } throwStmt) {
            return;
        }

        if (!IsArgumentNullExceptionThrow(throwStmt, context.SemanticModel, identifierNode.Identifier.Text, out var typeName)) {
            return;
        }

        var config = context.Options.AnalyzerConfigOptionsProvider.GetOptions(ifStatement.SyntaxTree);
        var global = context.Options.AnalyzerConfigOptionsProvider.GlobalOptions;

        var isMultiTarget = isMultiTargetGlobal
                            || GetConfigBool(config, global, "ancplua_is_multi_target");

        var configStyle = GetConfigValue(config, global, "ancplua_nullguard_style", "auto").ToUpperInvariant();

        var targetStyle = ComputeTargetStyle(hasThrowHelper, hasThrowIfNullBcl, isMultiTarget, configStyle);
        if (targetStyle == "portable" && !CanAssignToIdentifier(context.SemanticModel, identifierNode)) {
            return;
        }

        var properties = ImmutableDictionary.CreateBuilder<string, string?>();
        properties.Add(PropertyIdentifier, identifierNode.Identifier.Text);
        properties.Add(PropertyTypeName, typeName);
        properties.Add(PropertyStyle, targetStyle);

        context.ReportDiagnostic(Diagnostic.Create(
            s_rule,
            ifStatement.IfKeyword.GetLocation(),
            properties.ToImmutable(),
            targetStyle));
    }

    private static string ComputeTargetStyle(
        bool hasThrowHelper,
        bool hasThrowIfNullBcl,
        bool isMultiTarget,
        string configStyle) =>
        configStyle switch {
            "THROW" => hasThrowHelper ? "throw" : hasThrowIfNullBcl ? "bcl" : "portable",
            "BCL" => hasThrowIfNullBcl ? "bcl" : "portable",
            "PORTABLE" => "portable",
            _ => hasThrowHelper ? "throw" : hasThrowIfNullBcl && !isMultiTarget ? "bcl" : "portable"
        };

    private static bool GetConfigBool(AnalyzerConfigOptions config, AnalyzerConfigOptions global, string key) =>
        (config.TryGetValue(key, out var v) || global.TryGetValue(key, out v))
        && string.Equals(v, "true", StringComparison.OrdinalIgnoreCase);

    private static string GetConfigValue(AnalyzerConfigOptions config, AnalyzerConfigOptions global, string key,
        string defaultValue) =>
        config.TryGetValue(key, out var v) ? v : global.TryGetValue(key, out v) ? v : defaultValue;

    private static bool TryParseNullCheck(
        ExpressionSyntax condition,
        [NotNullWhen(true)] out IdentifierNameSyntax? identifier) {
        identifier = null;

        switch (condition) {
            case IsPatternExpressionSyntax {
                Pattern: ConstantPatternSyntax { Expression: LiteralExpressionSyntax l }
            } p
                when l.IsKind(SyntaxKind.NullLiteralExpression)
                     && p.Expression is IdentifierNameSyntax id:
                identifier = id;
                return true;

            case BinaryExpressionSyntax { Left: var left, Right: var right } bin
                when bin.IsKind(SyntaxKind.EqualsExpression): {
                if (right.IsKind(SyntaxKind.NullLiteralExpression) && left is IdentifierNameSyntax lId) {
                    identifier = lId;
                    return true;
                }

                if (left.IsKind(SyntaxKind.NullLiteralExpression) && right is IdentifierNameSyntax rId) {
                    identifier = rId;
                    return true;
                }

                break;
            }
        }

        return false;
    }

    private static ThrowStatementSyntax? TryGetThrowStatement(StatementSyntax statement) =>
        statement switch {
            ThrowStatementSyntax t => t,
            BlockSyntax { Statements: [ThrowStatementSyntax t] } => t,
            _ => null
        };

    private static bool IsArgumentNullExceptionThrow(
        ThrowStatementSyntax throwStmt,
        SemanticModel model,
        string targetParam,
        out string typeName) {
        typeName = "ArgumentNullException";

        if (throwStmt.Expression is not ObjectCreationExpressionSyntax creation) {
            return false;
        }

        if (creation.ArgumentList?.Arguments.Count != 1) {
            return false;
        }

        var typeSymbol = ModelExtensions.GetTypeInfo(model, creation.Type).Type;
        var isArgumentNullException = typeSymbol is not null
            ? typeSymbol.ToDisplayString() == "System.ArgumentNullException"
            : creation.Type.ToString() is "ArgumentNullException" or "System.ArgumentNullException";

        if (!isArgumentNullException) {
            return false;
        }

        typeName = creation.Type.ToString();

        var arg = creation.ArgumentList.Arguments[0].Expression;

        return arg switch {
            InvocationExpressionSyntax {
                Expression: IdentifierNameSyntax { Identifier.Text: "nameof" },
                ArgumentList.Arguments.Count: 1
            } inv when inv.ArgumentList.Arguments[0].Expression is IdentifierNameSyntax argId
                => argId.Identifier.Text == targetParam,
            LiteralExpressionSyntax lit when lit.IsKind(SyntaxKind.StringLiteralExpression) =>
                lit.Token.ValueText == targetParam,
            _ => false
        };
    }

    private static bool CanAssignToIdentifier(SemanticModel model, IdentifierNameSyntax identifier) {
        var symbol = model.GetSymbolInfo(identifier).Symbol;

        return symbol switch {
            IParameterSymbol parameter => parameter.RefKind != RefKind.In,
            ILocalSymbol local => !local.IsConst,
            IFieldSymbol field => !field.IsReadOnly && !field.IsConst,
            IPropertySymbol property => property.SetMethod is not null,
            _ => true
        };
    }
}
