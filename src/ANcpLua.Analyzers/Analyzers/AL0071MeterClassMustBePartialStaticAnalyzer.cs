using ANcpLua.Analyzers.Core;

namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0071: Detects [Meter] classes that are not declared as partial static.
/// </summary>
/// <remarks>
///     <para>
///         The source generator requires [Meter] classes to be partial static because:
///         <list type="bullet">
///             <item>The generator creates static Meter and instrument fields</item>
///             <item>The generator implements partial methods that record metrics</item>
///             <item>Static classes ensure single instance of meter/instruments</item>
///         </list>
///     </para>
///     <para>
///         Example of correct usage:
///         <code>
///         [Meter("MyApp")]
///         public static partial class AppMetrics
///         {
///             [Counter("orders.created")]
///             public static partial void RecordOrderCreated([Tag("status")] string status);
///         }
///         </code>
///     </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0071MeterClassMustBePartialStaticAnalyzer : AlAnalyzer {
    /// <summary>The diagnostic identifier for AL0071.</summary>
    public const string DiagnosticId = "AL0071";

    private const string MeterAttributeFullName = "qyl.ServiceDefaults.Instrumentation.MeterAttribute";

    private static readonly DiagnosticDescriptor Rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.Metrics,
        DiagnosticSeverities.RequiredFix);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    /// <summary>Registers syntax node actions to analyze class declarations with [Meter] attribute.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterSyntaxNodeAction(AnalyzeClassDeclaration, SyntaxKind.ClassDeclaration);

    private static void AnalyzeClassDeclaration(SyntaxNodeAnalysisContext context) {
        var classDeclaration = (ClassDeclarationSyntax)context.Node;

        // Quick check: skip if no attributes
        if (classDeclaration.AttributeLists.Count is 0) {
            return;
        }

        // Check if class has [Meter] attribute
        if (context.SemanticModel.GetDeclaredSymbol(classDeclaration, context.CancellationToken) is not { } classSymbol) {
            return;
        }

        if (!HasMeterAttribute(classSymbol, context.SemanticModel.Compilation)) {
            return;
        }

        // Check for partial modifier
        var hasPartial = classDeclaration.Modifiers.Any(SyntaxKind.PartialKeyword);

        // Check for static modifier
        var hasStatic = classDeclaration.Modifiers.Any(SyntaxKind.StaticKeyword);

        // Report if missing either modifier
        if (!hasPartial || !hasStatic) {
            context.ReportDiagnostic(Diagnostic.Create(
                Rule,
                classDeclaration.Identifier.GetLocation(),
                classSymbol.Name));
        }
    }

    private static bool HasMeterAttribute(INamedTypeSymbol classSymbol, Compilation compilation) {
        if (compilation.GetTypeByMetadataName(MeterAttributeFullName) is not { } meterAttributeType) {
            return false;
        }

        return classSymbol.GetAttributes().Any(a =>
            SymbolEqualityComparer.Default.Equals(a.AttributeClass, meterAttributeType));
    }
}
