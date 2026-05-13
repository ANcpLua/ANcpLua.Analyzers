using MsOperationExtensions = Microsoft.CodeAnalysis.Operations.OperationExtensions;

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
public sealed partial class Al0029UseHasAttributeAnalyzer : AlAnalyzer {
    /// <summary>The diagnostic identifier for AL0029.</summary>
    public const string DiagnosticId = "AL0029";

    /// <summary>Metadata name for ISymbol.</summary>
    private const string ISymbolTypeName = "Microsoft.CodeAnalysis.ISymbol";

    private static readonly DiagnosticDescriptor s_rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.RoslynUtilities,
        DiagnosticSeverity.Info);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_rule];

    /// <summary>Registers compilation start action to analyze GetAttributes usage patterns.</summary>
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
            context.ReportDiagnostic(s_rule, invocation.Syntax.GetLocation(),
                "symbol.HasAttribute(name)", "GetAttributes() LINQ query");
        }
    }

    private static void AnalyzeLoop(OperationAnalysisContext context) {
        if (context.Operation is not IForEachLoopOperation forEachLoop) {
            return;
        }

        var collectionName = forEachLoop.Collection.GetCollectionSourceName();
        if (collectionName is "GetAttributes" &&
            ContainsAttributeClassComparison(forEachLoop.Body) &&
            !ExtractsAttributeData(forEachLoop.Body)) {
            context.ReportDiagnostic(s_rule, forEachLoop.Syntax.GetLocation(),
                "symbol.HasAttribute(name)", "foreach over GetAttributes()");
        }
    }

    private static bool IsAttributeLinqCheck(IInvocationOperation invocation) {
        var method = invocation.TargetMethod;

        if (method.Name is not "Any" ||
            !HasPredicateArgument(invocation)) {
            return false;
        }

        if (GetUnderlyingInvocationName(invocation.Instance) == "GetAttributes") {
            return true;
        }

        return method.IsExtensionMethod &&
               invocation.Arguments.Length > 0 &&
               GetUnderlyingInvocationName(invocation.Arguments[0].Value) == "GetAttributes";
    }

    private static bool HasPredicateArgument(IInvocationOperation invocation) {
        var expectedArgumentCount = invocation.Instance is null ? 2 : 1;
        return invocation.Arguments.Length == expectedArgumentCount;
    }

    private static string? GetUnderlyingInvocationName(IOperation? operation) =>
        operation switch {
            IInvocationOperation inv => inv.TargetMethod.Name,
            IConversionOperation conv => GetUnderlyingInvocationName(conv.Operand),
            _ => null
        };

    private static bool ContainsAttributeClassComparison(IOperation? body) {
        if (body is null) {
            return false;
        }

        foreach (var descendant in MsOperationExtensions.Descendants(body)) {
            if (descendant is IPropertyReferenceOperation { Property.Name: "AttributeClass" }) {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    ///     Checks if the loop body extracts data from AttributeData beyond just checking existence.
    ///     When true, the foreach pattern is valid and shouldn't trigger AL0029.
    /// </summary>
    private static bool ExtractsAttributeData(IOperation? body) {
        if (body is null) {
            return false;
        }

        foreach (var descendant in MsOperationExtensions.Descendants(body)) {
            if (descendant is IPropertyReferenceOperation { Property.Name: "ConstructorArguments" or "NamedArguments" or
                    "ApplicationSyntaxReference" or "AttributeConstructor" })
                // These properties extract data from the attribute, not just check existence
            {
                return true;
            }
        }

        return false;
    }
}
