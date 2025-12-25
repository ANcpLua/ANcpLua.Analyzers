using ANcpLua.Analyzers.Core;
using ANcpLua.Roslyn.Utilities;

namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0015: Normalize null-guard style.
///     Detects simple ArgumentNullException null-guards and flags them for potential normalization.
/// </summary>
/// <remarks>
///     Detects patterns like:
///     <list type="bullet">
///         <item><c>if (x is null) throw new ArgumentNullException(nameof(x));</c></item>
///         <item><c>if (x == null) throw new ArgumentNullException(nameof(x));</c></item>
///         <item><c>if (x is null) { throw new ArgumentNullException(nameof(x)); }</c></item>
///         <item><c>if (x is null) throw new ArgumentNullException("x");</c></item>
///     </list>
///     Uses TFM-aware capability detection and editorconfig to decide whether to suggest
///     portable or BCL forms.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AL0015NormalizeNullGuardStyleAnalyzer : ALAnalyzer
{
    public const string DiagnosticId = DiagnosticIds.NormalizeNullGuardStyle;

    internal const string PropertyIdentifierName = "identifierName";
    internal const string PropertyHasThrowIfNull = "hasThrowIfNull";
    internal const string PropertyMode = "mode";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Normalize null-guard style",
        "Simplify this null-guard to a standard form",
        DiagnosticCategories.Style,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "This null-guard pattern can be simplified to a standard form using ThrowIfNull or other normalized approaches.",
        helpLinkUri: HelpLinkBase + "AL0015.md");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    protected override void RegisterActions(AnalysisContext context)
    {
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    private static void OnCompilationStart(CompilationStartAnalysisContext context)
    {
        var throwIfNullExists = ThrowIfNullExists(context.Compilation);

        // Read EditorConfig and build properties
        var provider = context.Options.AnalyzerConfigOptionsProvider;
        var targetFrameworks = provider.GlobalOptions.GetValueOrNull("build_property.TargetFrameworks") ?? "";
        var targetFramework = provider.GlobalOptions.GetValueOrNull("build_property.TargetFramework") ?? "";
        var nullGuardStyle = provider.GlobalOptions.GetValueOrNull("ancplua_nullguard_style") ?? "auto";

        // Compute the null-guard mode based on config and capabilities
        var mode = ComputeNullGuardMode(nullGuardStyle, targetFrameworks, targetFramework, throwIfNullExists);

        context.RegisterSyntaxNodeAction(
            ctx => AnalyzeIfStatement(ctx, throwIfNullExists, mode),
            SyntaxKind.IfStatement);
    }

    private static bool ThrowIfNullExists(Compilation compilation)
    {
        // Look for System.ArgumentNullException.ThrowIfNull
        var argNullExType = compilation.GetTypeByMetadataName("System.ArgumentNullException");
        if (argNullExType is null)
            return false;

        // Check if ThrowIfNull method exists
        // Signature: public static void ThrowIfNull(object? value, string? paramName = null)
        foreach (var member in argNullExType.GetMembers("ThrowIfNull"))
        {
            if (member is IMethodSymbol { IsStatic: true, ReturnType.SpecialType: SpecialType.System_Void } method
                && IsThrowIfNullSignature(method))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Computes the null-guard mode based on EditorConfig settings, target frameworks, and capability detection.
    ///
    /// Logic:
    /// 1. If multi-target (TargetFrameworks contains ';') => portable (stability)
    /// 2. Else if single target:
    ///    a. If mode == "bcl" AND ThrowIfNull is supported => bcl
    ///    b. Else => portable
    /// 3. If mode == "portable" => portable (explicit choice)
    /// 4. If mode == "auto" => use heuristic above
    /// </summary>
    private static string ComputeNullGuardMode(
        string? editorConfigMode,
        string? targetFrameworks,
        string? targetFramework,
        bool throwIfNullExists)
    {
        // Multi-target detection: TargetFrameworks contains semicolon separator
        bool isMultiTarget = targetFrameworks?.Contains(';') == true;

        // If multi-target, always use portable for stability
        if (isMultiTarget)
            return "portable";

        // Single-target case: apply mode logic
        return editorConfigMode switch
        {
            "bcl" => throwIfNullExists ? "bcl" : "portable",
            "portable" => "portable",
            "auto" or _ => throwIfNullExists ? "bcl" : "portable"
        };
    }

    private static bool IsThrowIfNullSignature(IMethodSymbol method)
    {
        // Must have 1 or 2 parameters
        if (method.Parameters.Length is < 1 or > 2)
            return false;

        // First parameter should be object? (name doesn't matter)
        var firstParam = method.Parameters[0];
        if (firstParam.Type.SpecialType != SpecialType.System_Object && firstParam.Type.ToDisplayString() != "object?")
            return false;

        // If there's a second parameter, it should be string? with a default value
        if (method.Parameters.Length == 2)
        {
            var secondParam = method.Parameters[1];
            if (secondParam.Type.SpecialType != SpecialType.System_String && secondParam.Type.ToDisplayString() != "string?")
                return false;
            if (!secondParam.HasExplicitDefaultValue)
                return false;
        }

        return true;
    }

    private static void AnalyzeIfStatement(SyntaxNodeAnalysisContext context, bool throwIfNullExists, string mode)
    {
        var ifStatement = (IfStatementSyntax)context.Node;

        // Parse the condition to extract null-check info
        if (!TryParseNullCheckCondition(ifStatement.Condition, context.SemanticModel, out var identifier, out var _))
            return;

        // Check if statement is a single throw or a block with single throw
        if (!TryGetThrowStatement(ifStatement.Statement, out var throwStatement))
            return;

        // throwStatement is guaranteed non-null after TryGetThrowStatement returns true
        if (throwStatement is null)
            return;

        // Verify it's throwing ArgumentNullException with exactly 1 argument
        if (!IsArgumentNullExceptionThrow(throwStatement!, out var paramNameExpr, context.SemanticModel))
            return;

        // paramNameExpr is guaranteed non-null after IsArgumentNullExceptionThrow returns true
        if (paramNameExpr is null)
            return;

        // Verify paramName matches the checked identifier
        if (!ParameterNameMatches(paramNameExpr!, identifier))
            return;

        // Create properties for the code fix
        var properties = ImmutableDictionary.CreateBuilder<string, string?>();
        properties.Add(PropertyIdentifierName, identifier);
        properties.Add(PropertyHasThrowIfNull, throwIfNullExists.ToString());
        properties.Add(PropertyMode, mode);

        context.ReportDiagnostic(Diagnostic.Create(
            Rule,
            ifStatement.IfKeyword.GetLocation(),
            properties.ToImmutable()));
    }

    private static bool TryParseNullCheckCondition(
        ExpressionSyntax condition,
        SemanticModel semanticModel,
        out string identifier,
        out bool isIsPattern)
    {
        identifier = string.Empty;
        isIsPattern = false;

        // Pattern: x is null
        if (condition is IsPatternExpressionSyntax isPattern)
        {
            if (isPattern.Pattern is not ConstantPatternSyntax constantPattern)
                return false;

            if (!constantPattern.Expression.IsKind(SyntaxKind.NullLiteralExpression))
                return false;

            // Get identifier from expression - must be a simple identifier, not property/member access
            if (isPattern.Expression is not IdentifierNameSyntax identifierName)
                return false;

            identifier = identifierName.Identifier.Text;
            isIsPattern = true;
            return true;
        }

        // Pattern: x == null or x != null (we want == null)
        if (condition is BinaryExpressionSyntax binary)
        {
            // Only match ==, not !=
            if (!binary.IsKind(SyntaxKind.EqualsExpression))
                return false;

            bool expressionIsLeft;
            if (binary.Right.IsKind(SyntaxKind.NullLiteralExpression))
            {
                expressionIsLeft = true;
            }
            else if (binary.Left.IsKind(SyntaxKind.NullLiteralExpression))
            {
                expressionIsLeft = false;
            }
            else
            {
                return false;
            }

            var expr = expressionIsLeft ? binary.Left : binary.Right;

            // Must be a simple identifier, not property/member access
            if (expr is not IdentifierNameSyntax identifierName)
                return false;

            identifier = identifierName.Identifier.Text;
            isIsPattern = false;
            return true;
        }

        return false;
    }

    private static bool TryGetThrowStatement(StatementSyntax statement, out ThrowStatementSyntax? throwStatement)
    {
        throwStatement = null;

        // Direct throw
        if (statement is ThrowStatementSyntax directThrow)
        {
            throwStatement = directThrow;
            return true;
        }

        // Block with single throw
        if (statement is BlockSyntax { Statements.Count: 1 } block)
        {
            if (block.Statements[0] is ThrowStatementSyntax blockThrow)
            {
                throwStatement = blockThrow;
                return true;
            }
        }

        return false;
    }

    private static bool IsArgumentNullExceptionThrow(
        ThrowStatementSyntax throwStatement,
        out ExpressionSyntax? paramNameExpr,
        SemanticModel semanticModel)
    {
        paramNameExpr = null;

        if (throwStatement.Expression is not ObjectCreationExpressionSyntax objectCreation)
            return false;

        // Verify it's ArgumentNullException
        var typeSymbol = semanticModel.GetTypeInfo(objectCreation.Type).Type;
        if (typeSymbol?.ToDisplayString() != "System.ArgumentNullException")
            return false;

        // Must have exactly 1 argument (paramName only, no message or inner exception)
        if (objectCreation.ArgumentList?.Arguments.Count != 1)
            return false;

        paramNameExpr = objectCreation.ArgumentList.Arguments[0].Expression;
        return true;
    }

    private static bool ParameterNameMatches(ExpressionSyntax paramNameExpr, string identifier)
    {
        // Match: nameof(identifier)
        if (paramNameExpr is InvocationExpressionSyntax invocation)
        {
            if (invocation.Expression is not IdentifierNameSyntax { Identifier.Text: "nameof" })
                return false;

            if (invocation.ArgumentList.Arguments.Count != 1)
                return false;

            var arg = invocation.ArgumentList.Arguments[0].Expression;
            if (arg is not IdentifierNameSyntax argIdentifier)
                return false;

            return argIdentifier.Identifier.Text == identifier;
        }

        // Match: string literal "identifier"
        if (paramNameExpr is LiteralExpressionSyntax literal)
        {
            if (!literal.IsKind(SyntaxKind.StringLiteralExpression))
                return false;

            var stringValue = literal.Token.ValueText;
            return stringValue == identifier;
        }

        return false;
    }
}
