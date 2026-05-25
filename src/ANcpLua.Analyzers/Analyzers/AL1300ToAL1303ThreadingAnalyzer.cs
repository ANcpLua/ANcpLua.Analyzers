
namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL1300-AL1303: Threading anti-pattern analyzers.
///     <list type="bullet">
///         <item>AL1300: Avoid async void methods (except event handlers)</item>
///         <item>AL1301: Avoid lock(this) - external code can cause deadlocks</item>
///         <item>AL1302: Avoid lock(typeof(T)) - type objects are globally visible</item>
///         <item>AL1303: Avoid lock("string") - string interning causes cross-assembly locking</item>
///     </list>
/// </summary>
/// <remarks>
///     <para>
///         <b>AL1300 - async void:</b> Async void methods cannot be awaited, exceptions crash
///         the process, and testing becomes difficult. Only event handlers should use async void.
///     </para>
///     <para>
///         <b>AL1301 - lock(this):</b> When you lock on <c>this</c>, any external code that has
///         a reference to your object can also lock on it, potentially causing deadlocks.
///     </para>
///     <para>
///         <b>AL1302 - lock(typeof(...)):</b> Type objects are singleton instances shared across
///         the entire application domain. Any code anywhere can lock on the same Type.
///     </para>
///     <para>
///         <b>AL1303 - lock("string"):</b> String literals are interned by the CLR, meaning
///         identical string literals across different assemblies share the same reference.
///     </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al1300ToAl1303ThreadingAnalyzer : AlAnalyzer {
    /// <summary>The diagnostic identifier for AL1300.</summary>
    private const string DiagnosticIdAl1300 = "AL1300";
    /// <summary>The diagnostic identifier for AL1301.</summary>
    private const string DiagnosticIdAl1301 = "AL1301";
    /// <summary>The diagnostic identifier for AL1302.</summary>
    private const string DiagnosticIdAl1302 = "AL1302";
    /// <summary>The diagnostic identifier for AL1303.</summary>
    private const string DiagnosticIdAl1303 = "AL1303";

    private static readonly DiagnosticDescriptor s_asyncVoidRule = CreateRule(
        DiagnosticIdAl1300,
        DiagnosticCategories.Threading,
        DiagnosticSeverity.Warning);

    private static readonly DiagnosticDescriptor s_lockOnThisRule = CreateRule(
        DiagnosticIdAl1301,
        DiagnosticCategories.Threading,
        DiagnosticSeverity.Warning);

    private static readonly DiagnosticDescriptor s_lockOnTypeRule = CreateRule(
        DiagnosticIdAl1302,
        DiagnosticCategories.Threading,
        DiagnosticSeverity.Warning);

    private static readonly DiagnosticDescriptor s_lockOnStringRule = CreateRule(
        DiagnosticIdAl1303,
        DiagnosticCategories.Threading,
        DiagnosticSeverity.Warning);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics (AL1300-AL1303).</summary>
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
