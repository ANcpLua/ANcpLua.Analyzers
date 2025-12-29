using ANcpLua.Analyzers.Core;
using ANcpLua.Analyzers.Internal;

namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0015: Normalize null-guard style.
///     Detects simple ArgumentNullException null-guards and flags them for potential normalization.
/// </summary>
/// <remarks>
///     Detects patterns like:
///     <list type="bullet">
///         <item>
///             <c>if (x is null) throw new ArgumentNullException(nameof(x));</c>
///         </item>
///         <item>
///             <c>if (x == null) throw new ArgumentNullException(nameof(x));</c>
///         </item>
///         <item>
///             <c>if (x is null) { throw new ArgumentNullException(nameof(x)); }</c>
///         </item>
///         <item>
///             <c>if (x is null) throw new ArgumentNullException("x");</c>
///         </item>
///     </list>
///     Uses TFM-aware capability detection and editorconfig to decide whether to suggest
///     portable or BCL forms.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AL0015NormalizeNullGuardStyleAnalyzer : ALAnalyzer {
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
        true,
        "This null-guard pattern can be simplified to a standard form using ThrowIfNull or other normalized approaches.",
        HelpLinkBase + "AL0015.md");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterCompilationStartAction(OnCompilationStart);

    private static void OnCompilationStart(CompilationStartAnalysisContext context) {
        var throwIfNullExists = ThrowIfNullExists(context.Compilation);
        var provider = context.Options.AnalyzerConfigOptionsProvider;

        context.RegisterSyntaxNodeAction(
            ctx => AnalyzeIfStatement(ctx, throwIfNullExists, provider),
            SyntaxKind.IfStatement);
    }

    /// <summary>
    ///     Gets an analyzer option, checking both global options and per-file options.
    ///     This ensures compatibility with both MSBuild (which uses GlobalOptions) and
    ///     testing frameworks (which may use per-file editorconfig).
    /// </summary>
    private static string GetOption(
        AnalyzerConfigOptionsProvider provider,
        SyntaxTree syntaxTree,
        string optionName,
        string defaultValue = "") {
        // First try global options (MSBuild scenario)
        var globalValue = provider.GlobalOptions.GetValueOrNull(optionName);
        if (!string.IsNullOrEmpty(globalValue)) {
            return StripQuotes(globalValue!);
        }

        // Then try per-file options (test scenario or explicit editorconfig)
        var fileOptions = provider.GetOptions(syntaxTree);
        var fileValue = fileOptions.GetValueOrNull(optionName);
        return !string.IsNullOrEmpty(fileValue) ? StripQuotes(fileValue!) : defaultValue;
    }

    /// <summary>
    ///     Strips surrounding quotes from a value if present.
    ///     EditorConfig values may be quoted to preserve special characters like semicolons.
    /// </summary>
    private static string StripQuotes(string value) {
        if (value.Length >= 2 && value.StartsWith("\"") && value.EndsWith("\"")) {
            return value.Substring(1, value.Length - 2);
        }

        return value;
    }

    private static bool ThrowIfNullExists(Compilation compilation) {
        // Look for System.ArgumentNullException.ThrowIfNull
        var argNullExType = compilation.GetTypeByMetadataName("System.ArgumentNullException");
        if (argNullExType is null) {
            return false;
        }

        // Check if ThrowIfNull method exists
        // Signature: public static void ThrowIfNull(object? value, string? paramName = null)
        foreach (var member in argNullExType.GetMembers("ThrowIfNull")) {
            if (member is IMethodSymbol { IsStatic: true, ReturnType.SpecialType: SpecialType.System_Void } method
                && IsThrowIfNullSignature(method)) {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    ///     Computes the null-guard mode based on EditorConfig settings, target frameworks, and capability detection.
    ///     Logic:
    ///     1. If multi-target (TargetFrameworks contains ';' or explicit override) => portable (stability)
    ///     2. Else if single target:
    ///     a. If mode == "bcl" AND ThrowIfNull is supported => bcl
    ///     b. Else => portable
    ///     3. If mode == "portable" => portable (explicit choice)
    ///     4. If mode == "auto" => use heuristic above
    /// </summary>
    private static string ComputeNullGuardMode(
        string? editorConfigMode,
        string? targetFrameworks,
        string? isMultiTargetOverride,
        bool throwIfNullExists) {
        // Multi-target detection: explicit override, or TargetFrameworks contains semicolon separator
        var isMultiTarget = string.Equals(isMultiTargetOverride, "true", StringComparison.OrdinalIgnoreCase)
                            || targetFrameworks?.Contains(';') == true;

        // If multi-target, always use portable for stability
        if (isMultiTarget) {
            return "portable";
        }

        // Single-target case: apply mode logic
        return editorConfigMode switch {
            "bcl" => throwIfNullExists ? "bcl" : "portable",
            "portable" => "portable",
            "auto" or _ => throwIfNullExists ? "bcl" : "portable"
        };
    }

    private static bool IsThrowIfNullSignature(IMethodSymbol method) {
        // Must have 1 or 2 parameters
        if (method.Parameters.Length is < 1 or > 2) {
            return false;
        }

        // First parameter should be object? (name doesn't matter)
        var firstParam = method.Parameters[0];
        if (firstParam.Type.SpecialType != SpecialType.System_Object &&
            firstParam.Type.ToDisplayString() != "object?") {
            return false;
        }

        switch (method.Parameters.Length) {
            // If there's a second parameter, it should be string? with a default value
            case 2: {
                var secondParam = method.Parameters[1];
                if (secondParam.Type.SpecialType != SpecialType.System_String &&
                    secondParam.Type.ToDisplayString() != "string?") {
                    return false;
                }

                if (!secondParam.HasExplicitDefaultValue) {
                    return false;
                }

                break;
            }
        }

        return true;
    }

    private static void AnalyzeIfStatement(
        SyntaxNodeAnalysisContext context,
        bool throwIfNullExists,
        AnalyzerConfigOptionsProvider provider) {
        var ifStatement = (IfStatementSyntax)context.Node;
        var syntaxTree = ifStatement.SyntaxTree;

        // Read EditorConfig and build properties (checking both global and per-file options)
        // Try MSBuild property first (production), then custom option (testing/explicit config)
        var targetFrameworks = GetOption(provider, syntaxTree, "build_property.TargetFrameworks");
        if (string.IsNullOrEmpty(targetFrameworks)) {
            targetFrameworks = GetOption(provider, syntaxTree, "ancplua_target_frameworks");
        }

        var targetFramework = GetOption(provider, syntaxTree, "build_property.TargetFramework");
        if (string.IsNullOrEmpty(targetFramework)) {
            GetOption(provider, syntaxTree, "ancplua_target_framework");
        }

        // Check explicit multi-target flag (for testing or explicit configuration)
        var isMultiTargetOverride = GetOption(provider, syntaxTree, "ancplua_is_multi_target");

        var nullGuardStyle = GetOption(provider, syntaxTree, "ancplua_nullguard_style", "auto");

        // Compute the null-guard mode based on config and capabilities
        var mode = ComputeNullGuardMode(nullGuardStyle, targetFrameworks, isMultiTargetOverride,
            throwIfNullExists);

        // Parse the condition to extract null-check info
        if (!TryParseNullCheckCondition(ifStatement.Condition, out var identifier)) {
            return;
        }

        // Check if statement is a single throw or a block with single throw
        if (!TryGetThrowStatement(ifStatement.Statement, out var throwStatement)) {
            return;
        }

        // throwStatement is guaranteed non-null after TryGetThrowStatement returns true
        if (throwStatement is null) {
            return;
        }

        // Verify it's throwing ArgumentNullException with exactly 1 argument
        if (!IsArgumentNullExceptionThrow(throwStatement, out var paramNameExpr, context.SemanticModel)) {
            return;
        }

        // paramNameExpr is guaranteed non-null after IsArgumentNullExceptionThrow returns true
        if (paramNameExpr is null) {
            return;
        }

        // Verify paramName matches the checked identifier
        if (!ParameterNameMatches(paramNameExpr, identifier)) {
            return;
        }

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
        out string identifier) {
        identifier = string.Empty;

        // Pattern: x is null
        if (condition is IsPatternExpressionSyntax isPattern) {
            if (isPattern.Pattern is not ConstantPatternSyntax constantPattern) {
                return false;
            }

            if (!constantPattern.Expression.IsKind(SyntaxKind.NullLiteralExpression)) {
                return false;
            }

            // Get identifier from expression - must be a simple identifier, not property/member access
            if (isPattern.Expression is not IdentifierNameSyntax identifierName) {
                return false;
            }

            identifier = identifierName.Identifier.Text;
            return true;
        }

        switch (condition) {
            // Pattern: x == null or x != null (we want == null)
            // Only match ==, not !=
            case BinaryExpressionSyntax binary when !binary.IsKind(SyntaxKind.EqualsExpression):
                return false;
            case BinaryExpressionSyntax binary: {
                bool expressionIsLeft;
                if (binary.Right.IsKind(SyntaxKind.NullLiteralExpression)) {
                    expressionIsLeft = true;
                } else if (binary.Left.IsKind(SyntaxKind.NullLiteralExpression)) {
                    expressionIsLeft = false;
                } else {
                    return false;
                }

                var expr = expressionIsLeft ? binary.Left : binary.Right;

                // Must be a simple identifier, not property/member access
                if (expr is not IdentifierNameSyntax identifierName) {
                    return false;
                }

                identifier = identifierName.Identifier.Text;
                return true;
            }
            default:
                return false;
        }
    }

    private static bool TryGetThrowStatement(StatementSyntax statement, out ThrowStatementSyntax? throwStatement) {
        throwStatement = null;

        // Direct throw
        if (statement is ThrowStatementSyntax directThrow) {
            throwStatement = directThrow;
            return true;
        }

        switch (statement) {
            // Block with single throw
            case BlockSyntax { Statements.Count: 1 } block when block.Statements[0] is ThrowStatementSyntax blockThrow:
                throwStatement = blockThrow;
                return true;
            default:
                return false;
        }
    }

    private static bool IsArgumentNullExceptionThrow(
        ThrowStatementSyntax throwStatement,
        out ExpressionSyntax? paramNameExpr,
        SemanticModel semanticModel) {
        paramNameExpr = null;

        if (throwStatement.Expression is not ObjectCreationExpressionSyntax objectCreation) {
            return false;
        }

        // Verify it's ArgumentNullException
        // First try semantic check, then fall back to syntax check
        var typeSymbol = semanticModel.GetTypeInfo(objectCreation.Type).Type;
        bool isArgumentNullException;

        if (typeSymbol is not null) {
            // Semantic check: type name is ArgumentNullException in System namespace
            isArgumentNullException = typeSymbol.Name == "ArgumentNullException" &&
                                      typeSymbol.ContainingNamespace?.ToDisplayString() == "System";
        } else {
            // Fallback: check syntax for common patterns
            var typeName = objectCreation.Type.ToString();
            isArgumentNullException = typeName is "ArgumentNullException" or "System.ArgumentNullException";
        }

        if (!isArgumentNullException) {
            return false;
        }

        // Must have exactly 1 argument (paramName only, no message or inner exception)
        if (objectCreation.ArgumentList?.Arguments.Count != 1) {
            return false;
        }

        paramNameExpr = objectCreation.ArgumentList.Arguments[0].Expression;
        return true;
    }

    private static bool ParameterNameMatches(ExpressionSyntax paramNameExpr, string identifier) {
        switch (paramNameExpr) {
            // Match: nameof(identifier)
            case InvocationExpressionSyntax {
                Expression: not IdentifierNameSyntax {
                    Identifier.Text: "nameof"
                }
            }:
                return false;
            case InvocationExpressionSyntax invocation when invocation.ArgumentList.Arguments.Count != 1:
                return false;
            case InvocationExpressionSyntax invocation: {
                var arg = invocation.ArgumentList.Arguments[0].Expression;
                if (arg is not IdentifierNameSyntax argIdentifier) {
                    return false;
                }

                return argIdentifier.Identifier.Text == identifier;
            }
            // Match: string literal "identifier"
            case LiteralExpressionSyntax literal when !literal.IsKind(SyntaxKind.StringLiteralExpression):
                return false;
            case LiteralExpressionSyntax literal: {
                var stringValue = literal.Token.ValueText;
                return stringValue == identifier;
            }
            default:
                return false;
        }
    }
}
