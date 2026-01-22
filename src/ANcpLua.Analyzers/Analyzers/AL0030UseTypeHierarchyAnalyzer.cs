using ANcpLua.Analyzers.Core;
using OperationExtensions = Microsoft.CodeAnalysis.Operations.OperationExtensions;

namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0030: Suggests using type hierarchy extensions instead of manual loops.
/// </summary>
/// <remarks>
///     <list type="bullet">
///         <item>
///             <c>foreach (var iface in type.AllInterfaces) if (Equals(iface, target))</c> →
///             <c>type.Implements(target)</c>
///         </item>
///         <item>
///             <c>while (baseType != null) { if (Equals(baseType, target)) ... baseType = baseType.BaseType; }</c> →
///             <c>type.InheritsFrom(target)</c>
///         </item>
///     </list>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class Al0030UseTypeHierarchyAnalyzer : AlAnalyzer {
    private const string SymbolEqualityComparerTypeName = "Microsoft.CodeAnalysis.SymbolEqualityComparer";
    private const string ITypeSymbolTypeName = "Microsoft.CodeAnalysis.ITypeSymbol";

    private static readonly LocalizableResourceString Title = new(
        nameof(Resources.AL0030AnalyzerTitle), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableResourceString MessageFormat = new(
        nameof(Resources.AL0030AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableResourceString Description = new(
        nameof(Resources.AL0030AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.UseTypeHierarchyExtensions,
        Title, MessageFormat, DiagnosticCategories.RoslynUtilities,
        DiagnosticSeverity.Info, true, Description,
        HelpLinkBase);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterCompilationStartAction(OnCompilationStart);

    private static void OnCompilationStart(CompilationStartAnalysisContext context) {
        if (context.Compilation.GetTypeByMetadataName(ITypeSymbolTypeName) is null) {
            return;
        }

        var symbolEqualityComparerType = context.Compilation.GetTypeByMetadataName(SymbolEqualityComparerTypeName);

        context.RegisterOperationAction(
            ctx => AnalyzeLoop(ctx, symbolEqualityComparerType),
            OperationKind.Loop);
    }

    private static void AnalyzeLoop(OperationAnalysisContext context, INamedTypeSymbol? symbolEqualityComparerType) {
        if (context.Operation is IForEachLoopOperation forEachLoop) {
            var collectionName = GetCollectionAccessName(forEachLoop.Collection);

            if (collectionName is "AllInterfaces" &&
                ContainsSymbolEqualityComparison(forEachLoop.Body, symbolEqualityComparerType)) {
                context.ReportDiagnostic(Rule, forEachLoop.Syntax.GetLocation(),
                    "type.Implements(interfaceType)", "foreach over AllInterfaces");
                return;
            }
        }

        if (context.Operation is IWhileLoopOperation whileLoop &&
            IsBaseTypeWalkingLoop(whileLoop, symbolEqualityComparerType)) {
            context.ReportDiagnostic(Rule, whileLoop.Syntax.GetLocation(),
                "type.InheritsFrom(baseType)", "while loop walking BaseType");
        }
    }

    private static string? GetCollectionAccessName(IOperation? collection) =>
        collection switch {
            IInvocationOperation invocation => invocation.TargetMethod.Name,
            IPropertyReferenceOperation propertyRef => propertyRef.Property.Name,
            IConversionOperation conversion => GetCollectionAccessName(conversion.Operand),
            _ => null
        };

    private static bool ContainsSymbolEqualityComparison(IOperation? body, INamedTypeSymbol? symbolEqualityComparerType) {
        if (body is null) {
            return false;
        }

        foreach (var descendant in OperationExtensions.Descendants(body)) {
            if (descendant is IInvocationOperation invocation) {
                // Check for SymbolEqualityComparer.Default.Equals(a, b)
                if (symbolEqualityComparerType is not null &&
                    IsSymbolEqualityComparerEquals(invocation, symbolEqualityComparerType)) {
                    return true;
                }

                // Also check for a.IsEqualTo(b) pattern (from ANcpLua.Roslyn.Utilities)
                // Extension method signature: IsEqualTo(this ISymbol?, ISymbol?) has 2 parameters
                if (invocation.TargetMethod is {
                        Name: "IsEqualTo",
                        IsExtensionMethod: true,
                        Parameters.Length: 2
                    }) {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool IsBaseTypeWalkingLoop(IWhileLoopOperation whileLoop, INamedTypeSymbol? symbolEqualityComparerType) {
        var hasBaseTypeAccess = false;
        var hasBaseTypeAssignment = false;
        var hasEqualityCheck = false;

        foreach (var operation in OperationExtensions.Descendants(whileLoop)) {
            if (operation is IPropertyReferenceOperation { Property.Name: "BaseType" }) {
                hasBaseTypeAccess = true;
            }

            if (operation is ISimpleAssignmentOperation {
                    Value: IPropertyReferenceOperation { Property.Name: "BaseType" }
                }) {
                hasBaseTypeAssignment = true;
            }

            if (operation is IInvocationOperation invocation) {
                // Check for SymbolEqualityComparer.Default.Equals(a, b)
                if (symbolEqualityComparerType is not null &&
                    IsSymbolEqualityComparerEquals(invocation, symbolEqualityComparerType)) {
                    hasEqualityCheck = true;
                }

                // Also check for a.IsEqualTo(b) pattern (from ANcpLua.Roslyn.Utilities)
                // Extension method signature: IsEqualTo(this ISymbol?, ISymbol?) has 2 parameters
                if (invocation.TargetMethod is {
                        Name: "IsEqualTo",
                        IsExtensionMethod: true,
                        Parameters.Length: 2
                    }) {
                    hasEqualityCheck = true;
                }
            }
        }

        return hasBaseTypeAccess && hasBaseTypeAssignment && hasEqualityCheck;
    }

    private static bool IsSymbolEqualityComparerEquals(IInvocationOperation invocation, INamedTypeSymbol symbolEqualityComparerType) {
        var method = invocation.TargetMethod;
        if (method.Name != "Equals" || method.Parameters.Length != 2) {
            return false;
        }

        return method.ContainingType.IsEqualTo(symbolEqualityComparerType);
    }
}
