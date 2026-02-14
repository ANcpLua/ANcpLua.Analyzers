using ANcpLua.Analyzers.Core;
using ANcpLua.Roslyn.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Threading;

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
    public const string DiagnosticIdAL0057 = "AL0057";
    /// <summary>The diagnostic identifier for AL0058.</summary>
    public const string DiagnosticIdAL0058 = "AL0058";
    /// <summary>The diagnostic identifier for AL0059.</summary>
    public const string DiagnosticIdAL0059 = "AL0059";
    /// <summary>The diagnostic identifier for AL0060.</summary>
    public const string DiagnosticIdAL0060 = "AL0060";

    private static readonly DiagnosticDescriptor AsyncVoidRule = CreateRule(
        DiagnosticIdAL0057,
        DiagnosticCategories.Threading,
        DiagnosticSeverity.Warning);

    private static readonly DiagnosticDescriptor LockOnThisRule = CreateRule(
        DiagnosticIdAL0058,
        DiagnosticCategories.Threading,
        DiagnosticSeverity.Warning);

    private static readonly DiagnosticDescriptor LockOnTypeRule = CreateRule(
        DiagnosticIdAL0059,
        DiagnosticCategories.Threading,
        DiagnosticSeverity.Warning);

    private static readonly DiagnosticDescriptor LockOnStringRule = CreateRule(
        DiagnosticIdAL0060,
        DiagnosticCategories.Threading,
        DiagnosticSeverity.Warning);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics (AL0057-AL0060).</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [AsyncVoidRule, LockOnThisRule, LockOnTypeRule, LockOnStringRule];

    /// <summary>Registers syntax node actions for method declarations and lock statements.</summary>
    protected override void RegisterActions(AnalysisContext context) {
        context.RegisterSyntaxNodeAction(AnalyzeMethodDeclaration, SyntaxKind.MethodDeclaration);
        context.RegisterSyntaxNodeAction(AnalyzeLockStatement, SyntaxKind.LockStatement);
    }

    /// <summary>Analyzes method declarations for async void anti-pattern (AL0057).</summary>
    private static void AnalyzeMethodDeclaration(SyntaxNodeAnalysisContext context) {
        var method = (MethodDeclarationSyntax)context.Node;

        // Must be async
        if (!method.Modifiers.Any(SyntaxKind.AsyncKeyword)) {
            return;
        }

        // Must return void (not Task, not Task<T>)
        if (method.ReturnType is not PredefinedTypeSyntax { Keyword.RawKind: (int)SyntaxKind.VoidKeyword }) {
            return;
        }

        // Get method symbol to check for event handler signature
        if (context.SemanticModel.GetDeclaredSymbol(method, context.CancellationToken) is not { } methodSymbol) {
            return;
        }

        // Skip event handlers - async void is valid for them
        if (IsEventHandler(methodSymbol, context.SemanticModel.Compilation)) {
            return;
        }

        // Report on the method name
        context.ReportDiagnostic(AsyncVoidRule, method.Identifier.GetLocation(), methodSymbol.Name);
    }

    /// <summary>Analyzes lock statements for dangerous lock targets (AL0058-AL0060).</summary>
    private static void AnalyzeLockStatement(SyntaxNodeAnalysisContext context) {
        var lockStatement = (LockStatementSyntax)context.Node;
        var expression = lockStatement.Expression;

        // AL0058: lock(this)
        if (expression is ThisExpressionSyntax) {
            context.ReportDiagnostic(LockOnThisRule, expression.GetLocation());
            return;
        }

        // AL0059: lock(typeof(...))
        if (expression is TypeOfExpressionSyntax typeOfExpression) {
            context.ReportDiagnostic(LockOnTypeRule, expression.GetLocation(), typeOfExpression.Type.ToString());
            return;
        }

        // AL0060: lock("string literal") or constant string
        if (expression is LiteralExpressionSyntax { RawKind: (int)SyntaxKind.StringLiteralExpression }) {
            context.ReportDiagnostic(LockOnStringRule, expression.GetLocation());
            return;
        }

        // Also check for constant string expressions (interpolated strings, const fields, etc.)
        if (IsConstantStringExpression(expression, context.SemanticModel, context.CancellationToken)) {
            context.ReportDiagnostic(LockOnStringRule, expression.GetLocation());
        }
    }

    /// <summary>
    ///     Determines if a method is an event handler based on its signature.
    ///     Event handlers have signature: void MethodName(object sender, EventArgs e)
    /// </summary>
    private static bool IsEventHandler(IMethodSymbol method, Compilation compilation) {
        // Must have exactly 2 parameters
        if (method.Parameters.Length != 2) {
            return false;
        }

        var firstParam = method.Parameters[0];
        var secondParam = method.Parameters[1];

        // First parameter should be object (sender)
        if (firstParam.Type.SpecialType != SpecialType.System_Object) {
            return false;
        }

        // Second parameter should be EventArgs or a derived type
        if (compilation.GetTypeByMetadataName("System.EventArgs") is not { } eventArgsType) {
            return false;
        }

        return secondParam.Type.IsEqualTo(eventArgsType) ||
               secondParam.Type.InheritsFrom(eventArgsType);
    }

    /// <summary>Determines if an expression is a constant string (includes const fields, interpolated strings).</summary>
    private static bool IsConstantStringExpression(
        ExpressionSyntax expression,
        SemanticModel semanticModel,
        CancellationToken cancellationToken) {
        // Skip if we already handled it as a literal
        if (expression is LiteralExpressionSyntax) {
            return false;
        }

        // Check if it's a string constant (includes const fields, interpolated strings, etc.)
        var constantValue = semanticModel.GetConstantValue(expression, cancellationToken);
        return constantValue is { HasValue: true, Value: string };
    }
}
