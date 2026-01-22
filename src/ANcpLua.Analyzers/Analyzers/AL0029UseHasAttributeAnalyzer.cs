using ANcpLua.Analyzers.Core;
using OperationExtensions = Microsoft.CodeAnalysis.Operations.OperationExtensions;

namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0029: Suggests using HasAttribute extension instead of GetAttributes() patterns.
/// </summary>
/// <remarks>
///     <list type="bullet">
///         <item><c>symbol.GetAttributes().Any(...)</c> → <c>symbol.HasAttribute(name)</c></item>
///         <item>
///             <c>foreach (var attr in symbol.GetAttributes()) if (attr.AttributeClass...)</c> →
///             <c>symbol.HasAttribute(name)</c>
///         </item>
///     </list>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class Al0029UseHasAttributeAnalyzer : AlAnalyzer {
    private const string ISymbolTypeName = "Microsoft.CodeAnalysis.ISymbol";

    private static readonly LocalizableResourceString Title = new(
        nameof(Resources.AL0029AnalyzerTitle), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableResourceString MessageFormat = new(
        nameof(Resources.AL0029AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableResourceString Description = new(
        nameof(Resources.AL0029AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.UseHasAttribute,
        Title, MessageFormat, DiagnosticCategories.RoslynUtilities,
        DiagnosticSeverity.Info, true, Description,
        HelpLinkBase);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterCompilationStartAction(OnCompilationStart);

    private static void OnCompilationStart(CompilationStartAnalysisContext context) {
        if (context.Compilation.GetTypeByMetadataName(ISymbolTypeName) is null) {
            return;
        }

        context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
        context.RegisterOperationAction(AnalyzeLoop, OperationKind.Loop);
    }

    private static void AnalyzeInvocation(OperationAnalysisContext context) {
        if (context.Operation is not IInvocationOperation invocation) {
            return;
        }

        if (IsAttributeLinqCheck(invocation)) {
            context.ReportDiagnostic(Rule, invocation.Syntax.GetLocation(),
                "symbol.HasAttribute(name)", "GetAttributes() LINQ query");
        }
    }

    private static void AnalyzeLoop(OperationAnalysisContext context) {
        if (context.Operation is not IForEachLoopOperation forEachLoop) {
            return;
        }

        var collectionName = GetCollectionAccessName(forEachLoop.Collection);
        if (collectionName is "GetAttributes" && ContainsAttributeClassComparison(forEachLoop.Body)) {
            context.ReportDiagnostic(Rule, forEachLoop.Syntax.GetLocation(),
                "symbol.HasAttribute(name)", "foreach over GetAttributes()");
        }
    }

    private static bool IsAttributeLinqCheck(IInvocationOperation invocation) {
        var method = invocation.TargetMethod;

        if (method.Name is not ("Any" or "FirstOrDefault" or "Where" or "Count")) {
            return false;
        }

        if (GetUnderlyingInvocationName(invocation.Instance) == "GetAttributes") {
            return true;
        }

        if (method.IsExtensionMethod &&
            invocation.Arguments.Length > 0 &&
            GetUnderlyingInvocationName(invocation.Arguments[0].Value) == "GetAttributes") {
            return true;
        }

        return false;
    }

    private static string? GetUnderlyingInvocationName(IOperation? operation) =>
        operation switch {
            IInvocationOperation inv => inv.TargetMethod.Name,
            IConversionOperation conv => GetUnderlyingInvocationName(conv.Operand),
            _ => null
        };

    private static string? GetCollectionAccessName(IOperation? collection) =>
        collection switch {
            IInvocationOperation invocation => invocation.TargetMethod.Name,
            IPropertyReferenceOperation propertyRef => propertyRef.Property.Name,
            IConversionOperation conversion => GetCollectionAccessName(conversion.Operand),
            ILocalReferenceOperation localRef => GetLocalAssignmentSourceName(localRef),
            _ => null
        };

    private static string? GetLocalAssignmentSourceName(ILocalReferenceOperation localRef) {
        // Find the containing method/block and look for assignment to this local
        if (localRef.GetContainingBlock() is not { } containingBlock) {
            return null;
        }

        // Look for variable declaration with initialization
        foreach (var operation in OperationExtensions.Descendants(containingBlock)) {
            if (operation is IVariableDeclaratorOperation declarator &&
                SymbolEqualityComparer.Default.Equals(declarator.Symbol, localRef.Local) &&
                declarator.Initializer?.Value is IInvocationOperation invocation) {
                return invocation.TargetMethod.Name;
            }

            // Also check for simple assignments
            if (operation is ISimpleAssignmentOperation {
                    Target: ILocalReferenceOperation targetLocal
                } assignment &&
                SymbolEqualityComparer.Default.Equals(targetLocal.Local, localRef.Local) &&
                assignment.Value is IInvocationOperation assignedInvocation) {
                return assignedInvocation.TargetMethod.Name;
            }
        }

        return null;
    }

    private static bool ContainsAttributeClassComparison(IOperation? body) {
        if (body is null) {
            return false;
        }

        foreach (var descendant in OperationExtensions.Descendants(body)) {
            if (descendant is IPropertyReferenceOperation { Property.Name: "AttributeClass" }) {
                return true;
            }
        }

        return false;
    }
}
