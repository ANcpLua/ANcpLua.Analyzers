
namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL1109: Detects <c>Task.Run</c> usage in ASP.NET Core request handlers.
/// </summary>
/// <remarks>
///     <para>
///         In ASP.NET Core, request handling already runs on thread pool threads. Using
///         <c>Task.Run</c> to offload work to "another" thread pool thread adds overhead
///         (scheduling, context switching) without benefit. The request thread is already
///         a thread pool thread.
///     </para>
///     <para>
///         This analyzer flags <c>Task.Run</c> calls inside:
///         <list type="bullet">
///             <item>Controller action methods (classes inheriting from Controller/ControllerBase)</item>
///             <item>Minimal API endpoint delegates registered via MapGet/MapPost/etc.</item>
///             <item>Razor Page handler methods (OnGet, OnPost, etc.)</item>
///         </list>
///     </para>
///     <para>
///         Legitimate uses of <c>Task.Run</c> in web apps (CPU-bound work that needs to
///         not block the request thread) should suppress this diagnostic with a comment
///         explaining why the offload is necessary.
///     </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al1109AvoidTaskRunInAspNetCoreAnalyzer : AlAnalyzer {
    /// <summary>The diagnostic identifier for AL1109.</summary>
    private const string DiagnosticId = "AL1109";

    private enum KnownType { Task, ControllerBase, PageModel }

    private static readonly string[] s_knownTypeNames = [
        "System.Threading.Tasks.Task",
        "Microsoft.AspNetCore.Mvc.ControllerBase",
        "Microsoft.AspNetCore.Mvc.RazorPages.PageModel"
    ];

    private static readonly DiagnosticDescriptor s_rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.AspNetCore,
        DiagnosticSeverities.Suggestion);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_rule];

    /// <summary>Registers compilation start action to resolve ASP.NET Core types.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterCompilationStartAction(OnCompilationStart);

    private static void OnCompilationStart(CompilationStartAnalysisContext context) {
        var cache = new TypeCache<KnownType>(type => context.Compilation.GetTypeByMetadataName(s_knownTypeNames[(int)type]));

        if (cache.Get(KnownType.Task) is null) {
            return;
        }

        if (cache.Get(KnownType.ControllerBase) is null && cache.Get(KnownType.PageModel) is null) {
            return;
        }

        context.RegisterSyntaxNodeAction(
            ctx => AnalyzeInvocation(ctx, cache),
            SyntaxKind.InvocationExpression);
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context, TypeCache<KnownType> cache) {
        var invocation = (InvocationExpressionSyntax)context.Node;

        if (invocation.Expression is not MemberAccessExpressionSyntax { Name.Identifier.Text: "Run" } memberAccess) {
            return;
        }

        var typeInfo = context.SemanticModel.GetTypeInfo(memberAccess.Expression, context.CancellationToken);
        if (typeInfo.Type is not { } receiverType || !cache.IsType(receiverType, KnownType.Task)) {
            if (context.SemanticModel.GetSymbolInfo(memberAccess.Expression, context.CancellationToken).Symbol is not INamedTypeSymbol namedType ||
                !cache.IsType(namedType, KnownType.Task)) {
                return;
            }
        }

        if (IsInsideAspNetCoreHandler(invocation, context.SemanticModel, cache, context.CancellationToken)) {
            context.ReportDiagnostic(s_rule, invocation.GetLocation());
        }
    }

    private static bool IsInsideAspNetCoreHandler(
        SyntaxNode node,
        SemanticModel semanticModel,
        TypeCache<KnownType> cache,
        CancellationToken cancellationToken) {
        for (var current = node.Parent; current is not null; current = current.Parent) {
            if (current is LocalFunctionStatementSyntax or AnonymousFunctionExpressionSyntax) {
                return false;
            }

            if (current is not MethodDeclarationSyntax method ||
                semanticModel.GetDeclaredSymbol(method, cancellationToken) is not { ContainingType: { } containingType } methodSymbol) {
                continue;
            }

            return (cache.ImplementsOrInheritsFrom(containingType, KnownType.ControllerBase) &&
                    methodSymbol.DeclaredAccessibility == Accessibility.Public) ||
                   (cache.ImplementsOrInheritsFrom(containingType, KnownType.PageModel) &&
                    IsRazorPageHandler(methodSymbol.Name));
        }

        return false;
    }

    private static bool IsRazorPageHandler(string methodName) =>
        methodName.StartsWithOrdinal("OnGet") ||
        methodName.StartsWithOrdinal("OnPost") ||
        methodName.StartsWithOrdinal("OnPut") ||
        methodName.StartsWithOrdinal("OnDelete") ||
        methodName.StartsWithOrdinal("OnPatch");
}
