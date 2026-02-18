
namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0106: Detects <c>Task.Run</c> usage in ASP.NET Core request handlers.
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
public sealed partial class Al0106AvoidTaskRunInAspNetCoreAnalyzer : AlAnalyzer {
    /// <summary>The diagnostic identifier for AL0106.</summary>
    public const string DiagnosticId = "AL0106";

    private static readonly DiagnosticDescriptor Rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.AspNetCore,
        DiagnosticSeverities.Suggestion);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    /// <summary>Registers compilation start action to resolve ASP.NET Core types.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterCompilationStartAction(OnCompilationStart);

    private static void OnCompilationStart(CompilationStartAnalysisContext context) {
        if (context.Compilation.GetTypeByMetadataName("System.Threading.Tasks.Task") is not { } taskType) {
            return;
        }

        var controllerBaseType = context.Compilation.GetTypeByMetadataName(
            "Microsoft.AspNetCore.Mvc.ControllerBase");
        var pageModelType = context.Compilation.GetTypeByMetadataName(
            "Microsoft.AspNetCore.Mvc.RazorPages.PageModel");

        // If none of the ASP.NET Core types are available, this isn't an ASP.NET Core project
        if (controllerBaseType is null && pageModelType is null) {
            return;
        }

        context.RegisterSyntaxNodeAction(
            ctx => AnalyzeInvocation(ctx, taskType, controllerBaseType, pageModelType),
            SyntaxKind.InvocationExpression);
    }

    /// <summary>Analyzes invocation expressions for Task.Run calls in ASP.NET Core handlers.</summary>
    private static void AnalyzeInvocation(
        SyntaxNodeAnalysisContext context,
        INamedTypeSymbol taskType,
        INamedTypeSymbol? controllerBaseType,
        INamedTypeSymbol? pageModelType) {
        var invocation = (InvocationExpressionSyntax)context.Node;

        // Check if this is Task.Run(...)
        if (invocation.Expression is not MemberAccessExpressionSyntax { Name.Identifier.Text: "Run" } memberAccess) {
            return;
        }

        // Verify the receiver is System.Threading.Tasks.Task
        var typeInfo = context.SemanticModel.GetTypeInfo(memberAccess.Expression, context.CancellationToken);
        if (typeInfo.Type is not { } receiverType || !receiverType.IsEqualTo(taskType)) {
            // Also check for static access: Task.Run (where expression is the type name, not an instance)
            if (context.SemanticModel.GetSymbolInfo(memberAccess.Expression, context.CancellationToken).Symbol is not INamedTypeSymbol namedType ||
                !namedType.IsEqualTo(taskType)) {
                return;
            }
        }

        // Find the containing method and check if it's in an ASP.NET Core handler
        if (!IsInsideAspNetCoreHandler(invocation, context.SemanticModel, controllerBaseType, pageModelType, context.CancellationToken)) {
            return;
        }

        context.ReportDiagnostic(Rule, invocation.GetLocation());
    }

    /// <summary>Determines if a node is inside an ASP.NET Core request handler method.</summary>
    private static bool IsInsideAspNetCoreHandler(
        SyntaxNode node,
        SemanticModel semanticModel,
        INamedTypeSymbol? controllerBaseType,
        INamedTypeSymbol? pageModelType,
        CancellationToken cancellationToken) {
        // Walk up to find the containing method
        for (var current = node.Parent; current is not null; current = current.Parent) {
            // Local functions and lambdas are separate scopes — stop searching
            if (current is LocalFunctionStatementSyntax or AnonymousFunctionExpressionSyntax) {
                return false;
            }

            if (current is not MethodDeclarationSyntax method) {
                continue;
            }

            if (semanticModel.GetDeclaredSymbol(method, cancellationToken) is not { } methodSymbol) {
                continue;
            }

            if (methodSymbol.ContainingType is not { } containingType) {
                continue;
            }

            // Check if containing type inherits from ControllerBase
            if (controllerBaseType is not null && containingType.InheritsFrom(controllerBaseType)) {
                // Only flag public methods (action methods)
                if (methodSymbol.DeclaredAccessibility == Accessibility.Public) {
                    return true;
                }
            }

            // Check if containing type inherits from PageModel
            if (pageModelType is not null && containingType.InheritsFrom(pageModelType)) {
                // Razor Page handlers: OnGet*, OnPost*, OnPut*, OnDelete*, OnPatch*
                if (IsRazorPageHandler(methodSymbol.Name)) {
                    return true;
                }
            }

            // Found the containing method but it's not a handler — stop searching
            return false;
        }

        return false;
    }

    /// <summary>Determines if a method name matches Razor Page handler conventions.</summary>
    private static bool IsRazorPageHandler(string methodName) =>
        methodName.StartsWithOrdinal("OnGet") ||
        methodName.StartsWithOrdinal("OnPost") ||
        methodName.StartsWithOrdinal("OnPut") ||
        methodName.StartsWithOrdinal("OnDelete") ||
        methodName.StartsWithOrdinal("OnPatch");
}
