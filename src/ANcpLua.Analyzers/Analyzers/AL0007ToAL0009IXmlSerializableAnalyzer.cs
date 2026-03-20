
namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     Analyzer for IXmlSerializable.GetSchema usage patterns.
///     AL0007: GetSchema should be explicitly implemented
///     AL0008: GetSchema must return null and not be abstract
///     AL0009: Don't call GetSchema
/// </summary>
/// <remarks>
///     <para>
///         The <see cref="System.Xml.Serialization.IXmlSerializable.GetSchema" /> method
///         is a historical artifact that should always return <c>null</c>. Microsoft's
///         documentation explicitly states this, and the <see cref="System.Xml.Serialization.XmlSerializer" />
///         ignores its return value entirely.
///     </para>
///     <para>
///         AL0007 enforces explicit interface implementation to prevent <c>GetSchema</c>
///         from appearing in the public API surface. An implicit implementation exposes
///         a meaningless method that always returns <c>null</c>.
///     </para>
///     <para>
///         AL0008 ensures the method returns <c>null</c> and is not abstract. Abstract
///         <c>GetSchema</c> methods force derived classes to implement something that
///         has no meaningful implementation.
///     </para>
///     <para>
///         AL0009 prevents calling <c>GetSchema</c> since its return value is always
///         <c>null</c> by contract. Any code that calls it is either dead code or
///         based on a misunderstanding of the interface.
///     </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0007ToAl0009IXmlSerializableAnalyzer : AlAnalyzer {
    /// <summary>The diagnostic identifier for AL0007.</summary>
    public const string DiagnosticIdAl0007 = "AL0007";
    /// <summary>The diagnostic identifier for AL0008.</summary>
    public const string DiagnosticIdAl0008 = "AL0008";
    /// <summary>The diagnostic identifier for AL0009.</summary>
    public const string DiagnosticIdAl0009 = "AL0009";

    private static readonly LocalizableResourceString TitleAl0007 = new(
        nameof(Resources.AL0007AnalyzerTitle), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableResourceString MessageFormatAl0007 = new(
        nameof(Resources.AL0007AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableResourceString DescriptionAl0007 = new(
        nameof(Resources.AL0007AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableResourceString TitleAl0008 = new(
        nameof(Resources.AL0008AnalyzerTitle), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableResourceString MessageFormatAl0008 = new(
        nameof(Resources.AL0008AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableResourceString DescriptionAl0008 = new(
        nameof(Resources.AL0008AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableResourceString TitleAl0009 = new(
        nameof(Resources.AL0009AnalyzerTitle), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableResourceString MessageFormatAl0009 = new(
        nameof(Resources.AL0009AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableResourceString DescriptionAl0009 = new(
        nameof(Resources.AL0009AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

    private static readonly DiagnosticDescriptor RuleAl0007 = new(
        DiagnosticIdAl0007,
        TitleAl0007, MessageFormatAl0007, DiagnosticCategories.Usage,
        DiagnosticSeverity.Error, true, DescriptionAl0007,
        HelpLink(DiagnosticIdAl0007));

    private static readonly DiagnosticDescriptor RuleAl0008 = new(
        DiagnosticIdAl0008,
        TitleAl0008, MessageFormatAl0008, DiagnosticCategories.Usage,
        DiagnosticSeverity.Error, true, DescriptionAl0008,
        HelpLink(DiagnosticIdAl0008));

    private static readonly DiagnosticDescriptor RuleAl0009 = new(
        DiagnosticIdAl0009,
        TitleAl0009, MessageFormatAl0009, DiagnosticCategories.Usage,
        DiagnosticSeverity.Error, true, DescriptionAl0009,
        HelpLink(DiagnosticIdAl0009));

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics (AL0007, AL0008, AL0009).</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [RuleAl0007, RuleAl0008, RuleAl0009];

    /// <summary>Registers compilation start action to analyze IXmlSerializable implementations.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterCompilationStartAction(OnCompilationStart);

    private static void OnCompilationStart(CompilationStartAnalysisContext context) {
        if (context.Compilation.GetTypeByMetadataName("System.Xml.Serialization.IXmlSerializable")
            is not { } ixmlSerializable) {
            return;
        }

        if (ixmlSerializable.GetMembers("GetSchema").OfType<IMethodSymbol>().FirstOrDefault()
            is not { } getSchemaMethod) {
            return;
        }

        context.RegisterSyntaxNodeAction(
            ctx => AnalyzeMethodDeclaration(ctx, ixmlSerializable, getSchemaMethod),
            SyntaxKind.MethodDeclaration);

        context.RegisterOperationAction(
            ctx => AnalyzeInvocation(ctx, ixmlSerializable, getSchemaMethod),
            OperationKind.Invocation);
    }

    private static void AnalyzeMethodDeclaration(
        SyntaxNodeAnalysisContext context,
        INamedTypeSymbol ixmlSerializable,
        IMethodSymbol interfaceGetSchema) {
        var methodDeclaration = (MethodDeclarationSyntax)context.Node;
        if (context.SemanticModel.GetDeclaredSymbol(methodDeclaration, context.CancellationToken) is not
            { } methodSymbol) {
            return;
        }

        if (!IsGetSchemaImplementation(methodSymbol, ixmlSerializable)) {
            return;
        }

        if (!methodSymbol.ExplicitInterfaceImplementations.Any(i =>
                i.IsEqualTo(interfaceGetSchema))) {
            context.ReportDiagnostic(RuleAl0007, methodSymbol.Locations[0]);
        }

        if (methodSymbol.IsAbstract || ReturnsNonNullValue(methodDeclaration, context.SemanticModel)) {
            var location = methodDeclaration.DescendantNodes()
                               .FirstOrDefault(static n => n is BlockSyntax or ArrowExpressionClauseSyntax)
                               ?.GetLocation()
                           ?? methodDeclaration.GetLocation();

            context.ReportDiagnostic(RuleAl0008, location);
        }
    }

    private static void AnalyzeInvocation(
        OperationAnalysisContext context,
        INamedTypeSymbol ixmlSerializable,
        IMethodSymbol interfaceGetSchema) {
        var invocation = (IInvocationOperation)context.Operation;
        var targetMethod = invocation.TargetMethod;

        if (targetMethod.IsEqualTo(interfaceGetSchema) ||
            IsGetSchemaImplementation(targetMethod, ixmlSerializable)) {
            context.ReportDiagnostic(RuleAl0009, invocation.Syntax.GetLocation());
        }
    }

    private static bool IsGetSchemaImplementation(IMethodSymbol method, INamedTypeSymbol ixmlSerializable) {
        var implementsInterface =
            method.ContainingType.AllInterfaces.Contains(ixmlSerializable, SymbolEqualityComparer.Default);

        if (!implementsInterface) {
            return false;
        }

        return method.Name == "GetSchema" ||
               method.ExplicitInterfaceImplementations.Any(static i => i.Name == "GetSchema");
    }

    private static bool ReturnsNonNullValue(SyntaxNode methodDeclaration, SemanticModel model) {
        foreach (var node in methodDeclaration.DescendantNodes()) {
            if (node switch {
                ReturnStatementSyntax returnStatement => returnStatement.Expression,
                ArrowExpressionClauseSyntax arrow => arrow.Expression,
                _ => null
            } is not { } expression) {
                continue;
            }

            var constantValue = model.GetConstantValue(expression);

            if (!constantValue.HasValue || constantValue.Value is not null) {
                return true;
            }
        }

        return false;
    }
}
