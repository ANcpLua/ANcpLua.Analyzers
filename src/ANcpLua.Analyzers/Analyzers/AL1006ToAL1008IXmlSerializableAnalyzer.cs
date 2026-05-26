namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     Analyzer for IXmlSerializable.GetSchema usage patterns.
///     AL1006: GetSchema should be explicitly implemented
///     AL1007: GetSchema must return null and not be abstract
///     AL1008: Don't call GetSchema
/// </summary>
/// <remarks>
///     <para>
///         The <see cref="System.Xml.Serialization.IXmlSerializable.GetSchema" /> method
///         is a historical artifact that should always return <c>null</c>. Microsoft's
///         documentation explicitly states this, and the <see cref="System.Xml.Serialization.XmlSerializer" />
///         ignores its return value entirely.
///     </para>
///     <para>
///         AL1006 enforces explicit interface implementation to prevent <c>GetSchema</c>
///         from appearing in the public API surface. An implicit implementation exposes
///         a meaningless method that always returns <c>null</c>.
///     </para>
///     <para>
///         AL1007 ensures the method returns <c>null</c> and is not abstract. Abstract
///         <c>GetSchema</c> methods force derived classes to implement something that
///         has no meaningful implementation.
///     </para>
///     <para>
///         AL1008 prevents calling <c>GetSchema</c> since its return value is always
///         <c>null</c> by contract. Any code that calls it is either dead code or
///         based on a misunderstanding of the interface.
///     </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al1006ToAl1008IXmlSerializableAnalyzer : AlAnalyzer {
    /// <summary>The diagnostic identifier for AL1006.</summary>
    private const string DiagnosticIdAl1006 = "AL1006";
    /// <summary>The diagnostic identifier for AL1007.</summary>
    public const string DiagnosticIdAl1007 = "AL1007";
    /// <summary>The diagnostic identifier for AL1008.</summary>
    private const string DiagnosticIdAl1008 = "AL1008";

    private static readonly LocalizableResourceString s_titleAl1006 = new(
        nameof(Resources.AL1006AnalyzerTitle), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableResourceString s_messageFormatAl1006 = new(
        nameof(Resources.AL1006AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableResourceString s_descriptionAl1006 = new(
        nameof(Resources.AL1006AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableResourceString s_titleAl1007 = new(
        nameof(Resources.AL1007AnalyzerTitle), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableResourceString s_messageFormatAl1007 = new(
        nameof(Resources.AL1007AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableResourceString s_descriptionAl1007 = new(
        nameof(Resources.AL1007AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableResourceString s_titleAl1008 = new(
        nameof(Resources.AL1008AnalyzerTitle), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableResourceString s_messageFormatAl1008 = new(
        nameof(Resources.AL1008AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableResourceString s_descriptionAl1008 = new(
        nameof(Resources.AL1008AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

    private static readonly DiagnosticDescriptor s_ruleAl1006 = new(
        DiagnosticIdAl1006,
        s_titleAl1006, s_messageFormatAl1006, DiagnosticCategories.Usage,
        DiagnosticSeverity.Error, true, s_descriptionAl1006,
        RuleDocs.HelpLinkAuto(DiagnosticIdAl1006));

    private static readonly DiagnosticDescriptor s_ruleAl1007 = new(
        DiagnosticIdAl1007,
        s_titleAl1007, s_messageFormatAl1007, DiagnosticCategories.Usage,
        DiagnosticSeverity.Error, true, s_descriptionAl1007,
        RuleDocs.HelpLinkAuto(DiagnosticIdAl1007));

    private static readonly DiagnosticDescriptor s_ruleAl1008 = new(
        DiagnosticIdAl1008,
        s_titleAl1008, s_messageFormatAl1008, DiagnosticCategories.Usage,
        DiagnosticSeverity.Error, true, s_descriptionAl1008,
        RuleDocs.HelpLinkAuto(DiagnosticIdAl1008));

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics (AL1006, AL1007, AL1008).</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [s_ruleAl1006, s_ruleAl1007, s_ruleAl1008];

    /// <summary>Registers compilation start action to analyze IXmlSerializable implementations.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterCompilationStartAction(OnCompilationStart);

    private static void OnCompilationStart(CompilationStartAnalysisContext context) {
        if (context.Compilation.GetTypeByMetadataName("System.Xml.Serialization.IXmlSerializable")
            is not { } ixmlSerializable) {
            return;
        }

        if (ixmlSerializable.GetMembers("GetSchema").OfType<IMethodSymbol>()
                .FirstOrDefault(m => m.Parameters.Length == 0) is not { } getSchemaMethod) {
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

        if (!IsGetSchemaImplementation(methodSymbol, ixmlSerializable, interfaceGetSchema)) {
            return;
        }

        if (!methodSymbol.ExplicitInterfaceImplementations.Any(i =>
                i.IsEqualTo(interfaceGetSchema))) {
            context.ReportDiagnostic(s_ruleAl1006, methodSymbol.Locations[0]);
        }

        if (methodSymbol.IsAbstract || ReturnsNonNullValue(methodDeclaration, context.SemanticModel)) {
            var location = methodDeclaration.DescendantNodes()
                               .FirstOrDefault(static n => n is BlockSyntax or ArrowExpressionClauseSyntax)
                               ?.GetLocation()
                           ?? methodDeclaration.GetLocation();

            context.ReportDiagnostic(s_ruleAl1007, location);
        }
    }

    private static void AnalyzeInvocation(
        OperationAnalysisContext context,
        INamedTypeSymbol ixmlSerializable,
        IMethodSymbol interfaceGetSchema) {
        var invocation = (IInvocationOperation)context.Operation;
        var targetMethod = invocation.TargetMethod;

        if (!targetMethod.IsEqualTo(interfaceGetSchema) &&
            !IsGetSchemaImplementation(targetMethod, ixmlSerializable, interfaceGetSchema)) {
            return;
        }

        context.ReportDiagnostic(s_ruleAl1008, invocation.Syntax.GetLocation());
    }

    private static bool IsGetSchemaImplementation(
        IMethodSymbol method,
        INamedTypeSymbol ixmlSerializable,
        IMethodSymbol interfaceGetSchema) {
        if (method.ExplicitInterfaceImplementations.Any(interfaceMethod =>
                interfaceMethod.IsEqualTo(interfaceGetSchema))) {
            return true;
        }

        if (method.ContainingType is not INamedTypeSymbol containingType ||
            !containingType.AllInterfaces.Contains(ixmlSerializable, SymbolEqualityComparer.Default)) {
            return false;
        }

        if (!IsGetSchemaSignature(method, interfaceGetSchema)) {
            return false;
        }

        return containingType.FindImplementationForInterfaceMember(interfaceGetSchema) is { } implementation &&
               implementation.IsEqualTo(method);
    }

    private static bool IsGetSchemaSignature(IMethodSymbol method, IMethodSymbol interfaceGetSchema) =>
        method.Name == "GetSchema" &&
        method.Arity == interfaceGetSchema.Arity &&
        method.Parameters.Length == interfaceGetSchema.Parameters.Length &&
        method.ReturnType.IsEqualTo(interfaceGetSchema.ReturnType);

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
