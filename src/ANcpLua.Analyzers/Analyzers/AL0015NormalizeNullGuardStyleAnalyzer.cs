using ANcpLua.Analyzers.Core;

namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0015: Normalize null-guard style.
///     Enforces consistent use of Throw.IfNull, ThrowIfNull, or portable coalesce-throw patterns.
///     Priority: throw (Throw.IfNull) > bcl (ThrowIfNull) > portable (coalesce)
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AL0015NormalizeNullGuardStyleAnalyzer : ALAnalyzer {
    public const string DiagnosticId = DiagnosticIds.NormalizeNullGuardStyle;

    public const string PropertyIdentifier = "Id";
    public const string PropertyTypeName = "Type";
    public const string PropertyStyle = "Style";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Normalize null-guard style",
        "Normalize null-guard to '{0}' style",
        DiagnosticCategories.Style,
        DiagnosticSeverity.Info,
        true,
        "Null-guards should be normalized to the preferred project style (Throw, BCL, or Portable).",
        HelpLinkBase + "AL0015.md");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterCompilationStartAction(OnCompilationStart);

    private static void OnCompilationStart(CompilationStartAnalysisContext context) {
        // Check for Throw.IfNull (highest priority)
        var hasThrowHelper = context.Compilation
            .GetTypeByMetadataName("Microsoft.Shared.Diagnostics.Throw")
            ?.GetMembers("IfNull")
            .OfType<IMethodSymbol>()
            .Any(m => m.IsStatic && m.Parameters.Length >= 1) ?? false;

        // Check for BCL ThrowIfNull
        var hasThrowIfNullBcl = context.Compilation
            .GetTypeByMetadataName("System.ArgumentNullException")
            ?.GetMembers("ThrowIfNull")
            .OfType<IMethodSymbol>()
            .Any(m => m.IsStatic && m.Parameters.Length >= 1) ?? false;

        var globalOptions = context.Options.AnalyzerConfigOptionsProvider.GlobalOptions;
        var isMultiTarget = globalOptions.TryGetValue("build_property.TargetFrameworks", out var tfms)
                            && !string.IsNullOrWhiteSpace(tfms)
                            && tfms.Contains(';');

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

        if (!TryParseNullCheck(ifStatement.Condition, out var identifier)) {
            return;
        }

        if (TryGetThrowStatement(ifStatement.Statement) is not { } throwStmt) {
            return;
        }

        if (!IsArgumentNullExceptionThrow(throwStmt, context.SemanticModel, identifier, out var typeName)) {
            return;
        }

        var config = context.Options.AnalyzerConfigOptionsProvider.GetOptions(ifStatement.SyntaxTree);
        var global = context.Options.AnalyzerConfigOptionsProvider.GlobalOptions;

        // Check multi-target from global, per-file, or custom option
        var isMultiTarget = isMultiTargetGlobal
                            || (config.TryGetValue("ancplua_is_multi_target", out var mt)
                                && string.Equals(mt, "true", StringComparison.OrdinalIgnoreCase))
                            || (global.TryGetValue("ancplua_is_multi_target", out var gmt)
                                && string.Equals(gmt, "true", StringComparison.OrdinalIgnoreCase));

        // Check style from per-file or global config
        string configStyle;
        if (config.TryGetValue("ancplua_nullguard_style", out var val)) {
            configStyle = val.ToLowerInvariant();
        } else if (global.TryGetValue("ancplua_nullguard_style", out var gval)) {
            configStyle = gval.ToLowerInvariant();
        } else {
            configStyle = "auto";
        }

        // Compute target style: throw > bcl > portable
        var targetStyle = ComputeTargetStyle(hasThrowHelper, hasThrowIfNullBcl, isMultiTarget, configStyle);

        var properties = ImmutableDictionary.CreateBuilder<string, string?>();
        properties.Add(PropertyIdentifier, identifier);
        properties.Add(PropertyTypeName, typeName);
        properties.Add(PropertyStyle, targetStyle);

        context.ReportDiagnostic(Diagnostic.Create(
            Rule,
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
            "throw" => hasThrowHelper ? "throw" : hasThrowIfNullBcl ? "bcl" : "portable",
            "bcl" => hasThrowIfNullBcl ? "bcl" : "portable",
            "portable" => "portable",
            _ => hasThrowHelper ? "throw" : hasThrowIfNullBcl && !isMultiTarget ? "bcl" : "portable"
        };

    private static bool TryParseNullCheck(ExpressionSyntax condition, out string identifier) {
        identifier = "";

        switch (condition) {
            // x is null
            case IsPatternExpressionSyntax {
                    Pattern: ConstantPatternSyntax { Expression: LiteralExpressionSyntax l }
                } p
                when l.IsKind(SyntaxKind.NullLiteralExpression)
                     && p.Expression is IdentifierNameSyntax id:
                identifier = id.Identifier.Text;
                return true;

            // x == null / null == x
            case BinaryExpressionSyntax { Left: var left, Right: var right } bin
                when bin.IsKind(SyntaxKind.EqualsExpression): {
                if (right.IsKind(SyntaxKind.NullLiteralExpression) && left is IdentifierNameSyntax lId) {
                    identifier = lId.Identifier.Text;
                    return true;
                }

                if (left.IsKind(SyntaxKind.NullLiteralExpression) && right is IdentifierNameSyntax rId) {
                    identifier = rId.Identifier.Text;
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
            BlockSyntax { Statements.Count: 1 } b when b.Statements[0] is ThrowStatementSyntax t => t,
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

        // Verify it's ArgumentNullException - semantic check with syntax fallback
        var typeSymbol = model.GetTypeInfo(creation.Type).Type;
        bool isArgumentNullException;
        if (typeSymbol is not null) {
            var fullName = typeSymbol.ToDisplayString();
            isArgumentNullException = fullName == "System.ArgumentNullException";
        } else {
            // Fallback: check syntax for common patterns
            var syntaxTypeName = creation.Type.ToString();
            isArgumentNullException = syntaxTypeName is "ArgumentNullException" or "System.ArgumentNullException";
        }

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
}
