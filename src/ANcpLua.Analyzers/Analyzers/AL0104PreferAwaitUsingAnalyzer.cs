
namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0104: Detects <c>using</c> statements on IAsyncDisposable types inside async methods.
/// </summary>
/// <remarks>
///     <para>
///         When a type implements <c>IAsyncDisposable</c>, using <c>await using</c>
///         instead of <c>using</c> ensures that <c>DisposeAsync()</c> is called rather than
///         <c>Dispose()</c>. This matters for types that perform async cleanup such as flushing
///         buffers, closing network connections, or finalizing telemetry export.
///     </para>
///     <para>
///         This analyzer detects:
///         <list type="bullet">
///             <item><c>using var x = new AsyncDisposable();</c> in async methods</item>
///             <item><c>using (var x = new AsyncDisposable()) { }</c> in async methods</item>
///         </list>
///     </para>
///     <para>
///         The analyzer only fires when:
///         <list type="bullet">
///             <item>The containing method is <c>async</c> (so <c>await</c> is valid)</item>
///             <item>The type implements <c>IAsyncDisposable</c></item>
///             <item>The statement is not already <c>await using</c></item>
///         </list>
///     </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0104PreferAwaitUsingAnalyzer : AlAnalyzer {
    /// <summary>The diagnostic identifier for AL0104.</summary>
    public const string DiagnosticId = "AL0104";

    private static readonly DiagnosticDescriptor Rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.Reliability,
        DiagnosticSeverity.Warning);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    /// <summary>Registers compilation start action to resolve IAsyncDisposable type.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterCompilationStartAction(OnCompilationStart);

    private static void OnCompilationStart(CompilationStartAnalysisContext context) {
        if (context.Compilation.GetTypeByMetadataName("System.IAsyncDisposable") is not { } asyncDisposableType) {
            return;
        }

        context.RegisterSyntaxNodeAction(
            ctx => AnalyzeUsingDeclaration(ctx, asyncDisposableType),
            SyntaxKind.LocalDeclarationStatement);

        context.RegisterSyntaxNodeAction(
            ctx => AnalyzeUsingStatement(ctx, asyncDisposableType),
            SyntaxKind.UsingStatement);
    }

    /// <summary>Analyzes <c>using var x = ...;</c> declarations.</summary>
    private static void AnalyzeUsingDeclaration(SyntaxNodeAnalysisContext context, INamedTypeSymbol asyncDisposableType) {
        var localDeclaration = (LocalDeclarationStatementSyntax)context.Node;

        // Must be a using declaration (using var x = ...)
        if (localDeclaration.UsingKeyword.IsKind(SyntaxKind.None)) {
            return;
        }

        // Already await using — nothing to do
        if (localDeclaration.AwaitKeyword.IsKind(SyntaxKind.AwaitKeyword)) {
            return;
        }

        // Must be inside an async method/lambda
        if (!AsyncContextHelper.IsInsideAsyncContext(localDeclaration)) {
            return;
        }

        // Check if the declared type implements IAsyncDisposable
        if (localDeclaration.Declaration.Variables.Count is 0) {
            return;
        }

        var typeInfo = context.SemanticModel.GetTypeInfo(localDeclaration.Declaration.Type, context.CancellationToken);
        if (typeInfo.Type is not { } declaredType) {
            return;
        }

        if (declaredType.Implements(asyncDisposableType)) {
            context.ReportDiagnostic(Rule, localDeclaration.UsingKeyword.GetLocation(), declaredType.Name);
        }
    }

    /// <summary>Analyzes <c>using (var x = ...) { }</c> statements.</summary>
    private static void AnalyzeUsingStatement(SyntaxNodeAnalysisContext context, INamedTypeSymbol asyncDisposableType) {
        var usingStatement = (UsingStatementSyntax)context.Node;

        // Already await using — nothing to do
        if (usingStatement.AwaitKeyword.IsKind(SyntaxKind.AwaitKeyword)) {
            return;
        }

        // Must be inside an async method/lambda
        if (!AsyncContextHelper.IsInsideAsyncContext(usingStatement)) {
            return;
        }

        ITypeSymbol? disposedType = null;

        // using (var x = expr) { } — check the declared variable's type
        if (usingStatement.Declaration is { Variables.Count: > 0 } declaration) {
            var typeInfo = context.SemanticModel.GetTypeInfo(declaration.Type, context.CancellationToken);
            disposedType = typeInfo.Type;
        }

        // using (expr) { } — check the expression's type
        if (disposedType is null && usingStatement.Expression is not null) {
            var typeInfo = context.SemanticModel.GetTypeInfo(usingStatement.Expression, context.CancellationToken);
            disposedType = typeInfo.Type;
        }

        if (disposedType is not null && disposedType.Implements(asyncDisposableType)) {
            context.ReportDiagnostic(Rule, usingStatement.UsingKeyword.GetLocation(), disposedType.Name);
        }
    }
}
