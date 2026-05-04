
namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0057-AL0060: Threading anti-pattern analyzers.
///     <list type="bullet">
///         <item>AL0057: Avoid async void methods (except event handlers)</item>
///         <item>AL0058: Avoid lock(this) - external code can cause deadlocks</item>
///         <item>AL0059: Avoid lock(typeof(T)) - type objects are globally visible</item>
///         <item>AL0060: Avoid lock("string") - string interning causes cross-assembly locking</item>
///     </list>
/// </summary>
/// <remarks>
///     <para>
///         <b>AL0057 - async void:</b> Async void methods cannot be awaited, exceptions crash
///         the process, and testing becomes difficult. Only event handlers should use async void.
///     </para>
///     <para>
///         <b>AL0058 - lock(this):</b> When you lock on <c>this</c>, any external code that has
///         a reference to your object can also lock on it, potentially causing deadlocks.
///     </para>
///     <para>
///         <b>AL0059 - lock(typeof(...)):</b> Type objects are singleton instances shared across
///         the entire application domain. Any code anywhere can lock on the same Type.
///     </para>
///     <para>
///         <b>AL0060 - lock("string"):</b> String literals are interned by the CLR, meaning
///         identical string literals across different assemblies share the same reference.
///     </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0057ToAl0060ThreadingAnalyzer : AlAnalyzer {
    /// <summary>The diagnostic identifier for AL0057.</summary>
    private const string DiagnosticIdAl0057 = "AL0057";
    /// <summary>The diagnostic identifier for AL0058.</summary>
    private const string DiagnosticIdAl0058 = "AL0058";
    /// <summary>The diagnostic identifier for AL0059.</summary>
    private const string DiagnosticIdAl0059 = "AL0059";
    /// <summary>The diagnostic identifier for AL0060.</summary>
    private const string DiagnosticIdAl0060 = "AL0060";

    private static readonly DiagnosticDescriptor s_asyncVoidRule = CreateRule(
        DiagnosticIdAl0057,
        DiagnosticCategories.Threading,
        DiagnosticSeverity.Warning);

    private static readonly DiagnosticDescriptor s_lockOnThisRule = CreateRule(
        DiagnosticIdAl0058,
        DiagnosticCategories.Threading,
        DiagnosticSeverity.Warning);

    private static readonly DiagnosticDescriptor s_lockOnTypeRule = CreateRule(
        DiagnosticIdAl0059,
        DiagnosticCategories.Threading,
        DiagnosticSeverity.Warning);

    private static readonly DiagnosticDescriptor s_lockOnStringRule = CreateRule(
        DiagnosticIdAl0060,
        DiagnosticCategories.Threading,
        DiagnosticSeverity.Warning);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics (AL0057-AL0060).</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [s_asyncVoidRule, s_lockOnThisRule, s_lockOnTypeRule, s_lockOnStringRule];

    /// <summary>Registers syntax node actions for method declarations and lock statements.</summary>
    protected override void RegisterActions(AnalysisContext context) {
        context.RegisterSyntaxNodeAction(AnalyzeMethodDeclaration, SyntaxKind.MethodDeclaration);
        context.RegisterSyntaxNodeAction(AnalyzeLockStatement, SyntaxKind.LockStatement);
    }

    private static void AnalyzeMethodDeclaration(SyntaxNodeAnalysisContext context) {
        var method = (MethodDeclarationSyntax)context.Node;

        if (!method.Modifiers.Any(SyntaxKind.AsyncKeyword) ||
            method.ReturnType is not PredefinedTypeSyntax { Keyword.RawKind: (int)SyntaxKind.VoidKeyword } ||
            context.SemanticModel.GetDeclaredSymbol(method, context.CancellationToken) is not { } methodSymbol ||
            IsEventHandler(methodSymbol, context.SemanticModel.Compilation)) {
            return;
        }

        context.ReportDiagnostic(s_asyncVoidRule, method.Identifier.GetLocation(), methodSymbol.Name);
    }

    private static void AnalyzeLockStatement(SyntaxNodeAnalysisContext context) {
        var lockStatement = (LockStatementSyntax)context.Node;
        var expression = lockStatement.Expression;

        switch (expression) {
            case ThisExpressionSyntax:
                context.ReportDiagnostic(s_lockOnThisRule, expression.GetLocation());
                return;
            case TypeOfExpressionSyntax typeOfExpression:
                context.ReportDiagnostic(s_lockOnTypeRule, expression.GetLocation(), typeOfExpression.Type.ToString());
                return;
            case LiteralExpressionSyntax { RawKind: (int)SyntaxKind.StringLiteralExpression }:
                context.ReportDiagnostic(s_lockOnStringRule, expression.GetLocation());
                return;
        }

        if (IsConstantStringExpression(expression, context.SemanticModel, context.CancellationToken)) {
            context.ReportDiagnostic(s_lockOnStringRule, expression.GetLocation());
        }
    }

    /// <summary>
    ///     Event handlers have signature: void MethodName(object sender, EventArgs e).
    ///     async void is valid only for event handlers.
    /// </summary>
    private static bool IsEventHandler(IMethodSymbol method, Compilation compilation) {
        if (method.Parameters.Length != 2) {
            return false;
        }

        if (method.Parameters[0].Type.SpecialType != SpecialType.System_Object ||
            compilation.GetTypeByMetadataName("System.EventArgs") is not { } eventArgsType) {
            return false;
        }

        var secondParamType = method.Parameters[1].Type;
        return secondParamType.IsEqualTo(eventArgsType) || secondParamType.InheritsFrom(eventArgsType);
    }

    /// <summary>Catches constant string expressions not covered by the literal check (const fields, interpolated strings).</summary>
    private static bool IsConstantStringExpression(
        ExpressionSyntax expression,
        SemanticModel semanticModel,
        CancellationToken cancellationToken) =>
        expression is not LiteralExpressionSyntax &&
        semanticModel.GetConstantValue(expression, cancellationToken) is { HasValue: true, Value: string };
}
