
namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL1217: Suggests using Guard.NotEmpty() instead of if (guid == Guid.Empty) throw patterns.
/// </summary>
/// <remarks>
///     <list type="bullet">
///         <item><c>if (id == Guid.Empty) throw new ArgumentException(...)</c> -> <c>Guard.NotEmpty(id)</c></item>
///         <item><c>if (Guid.Empty == id) throw new ArgumentException(...)</c> -> <c>Guard.NotEmpty(id)</c></item>
///     </list>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al1217UseGuardNotEmptyGuidAnalyzer : AlAnalyzer {
    /// <summary>The diagnostic identifier for AL1217.</summary>
    public const string DiagnosticId = "AL1217";

    private const string GuardMetadataName = "ANcpLua.Roslyn.Utilities.Guard";

    private static readonly DiagnosticDescriptor s_rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.RoslynUtilities,
        DiagnosticSeverities.Suggestion);

    /// <summary>Property key for the Guid identifier.</summary>
    private const string PropertyExpression = "Expression";

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_rule];

    /// <summary>Registers syntax actions for analysis.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterCompilationStartAction(OnCompilationStart);

    private static void OnCompilationStart(CompilationStartAnalysisContext context) {
        // Guard.* lives in ANcpLua.Roslyn.Utilities.Guard. Only fire when that type is present and
        // callable from this compilation; otherwise the code fix would rewrite to a symbol the
        // consumer cannot resolve. Projects that do not reference ANcpLua.Roslyn.Utilities are unaffected.
        if (context.Compilation.GetTypeByMetadataName(GuardMetadataName) is not { } guardType) {
            return;
        }

        if (!context.Compilation.IsSymbolAccessibleWithin(guardType, context.Compilation.Assembly)) {
            return;
        }

        context.RegisterSyntaxNodeAction(AnalyzeIfStatement, SyntaxKind.IfStatement);
    }

    private static void AnalyzeIfStatement(SyntaxNodeAnalysisContext context) {
        var ifStatement = (IfStatementSyntax)context.Node;

        if (ifStatement.Else is not null ||
            !TryParseGuidEmptyCheck(ifStatement.Condition, context.SemanticModel, out var identifier) ||
            TryGetThrowStatement(ifStatement.Statement) is not { } throwStmt ||
            !IsArgumentExceptionThrow(throwStmt, context.SemanticModel)) {
            return;
        }

        var properties = ImmutableDictionary.CreateBuilder<string, string?>();
        properties.Add(PropertyExpression, identifier);

        context.ReportDiagnostic(Diagnostic.Create(
            s_rule,
            ifStatement.GetLocation(),
            properties.ToImmutable(),
            identifier));
    }

    private static bool TryParseGuidEmptyCheck(
        ExpressionSyntax condition,
        SemanticModel model,
        out string identifier) {
        identifier = "";

        if (condition is not BinaryExpressionSyntax { RawKind: (int)SyntaxKind.EqualsExpression } binary) {
            return false;
        }

        return (IsGuidEmpty(binary.Right, model) && TryGetGuidIdentifier(binary.Left, model, out identifier)) ||
               (IsGuidEmpty(binary.Left, model) && TryGetGuidIdentifier(binary.Right, model, out identifier));
    }

    private static bool IsGuidEmpty(ExpressionSyntax expression, SemanticModel model) =>
        expression is MemberAccessExpressionSyntax { Name.Identifier.Text: "Empty" } memberAccess &&
        (ModelExtensions.GetTypeInfo(model, memberAccess.Expression).Type?.ToDisplayString() == "System.Guid" ||
         memberAccess.Expression is IdentifierNameSyntax { Identifier.Text: "Guid" });

    private static bool TryGetGuidIdentifier(
        ExpressionSyntax expression,
        SemanticModel model,
        out string identifier) {
        identifier = "";

        if (ModelExtensions.GetTypeInfo(model, expression).Type?.ToDisplayString() != "System.Guid") {
            return false;
        }

        identifier = expression switch {
            IdentifierNameSyntax id => id.Identifier.Text,
            MemberAccessExpressionSyntax { Name: IdentifierNameSyntax } => expression.ToString(),
            _ => ""
        };

        return !string.IsNullOrEmpty(identifier);
    }

    private static ThrowStatementSyntax? TryGetThrowStatement(StatementSyntax statement) =>
        statement switch {
            ThrowStatementSyntax t => t,
            BlockSyntax { Statements: [ThrowStatementSyntax t] } => t,
            _ => null
        };

    private static bool IsArgumentExceptionThrow(ThrowStatementSyntax throwStmt, SemanticModel model) {
        if (throwStmt.Expression is not ObjectCreationExpressionSyntax creation) {
            return false;
        }

        var typeSymbol = ModelExtensions.GetTypeInfo(model, creation.Type).Type;
        if (typeSymbol is null) {
            var typeName = creation.Type.ToString();
            return typeName is "ArgumentException" or "System.ArgumentException"
                or "ArgumentNullException" or "System.ArgumentNullException";
        }

        return typeSymbol.ToDisplayString() is "System.ArgumentException" or "System.ArgumentNullException";
    }
}
