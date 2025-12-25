using ANcpLua.Analyzers.Core;

namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     Analyzer for IXmlSerializable.GetSchema usage patterns.
///     AL0007: GetSchema should be explicitly implemented
///     AL0008: GetSchema must return null and not be abstract
///     AL0009: Don't call GetSchema
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AL0007ToAL0009IXmlSerializableAnalyzer : ALAnalyzer
{
    private static readonly LocalizableResourceString TitleAL0007 = new(
        nameof(Resources.AL0007AnalyzerTitle), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableResourceString MessageFormatAL0007 = new(
        nameof(Resources.AL0007AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableResourceString DescriptionAL0007 = new(
        nameof(Resources.AL0007AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableResourceString TitleAL0008 = new(
        nameof(Resources.AL0008AnalyzerTitle), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableResourceString MessageFormatAL0008 = new(
        nameof(Resources.AL0008AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableResourceString DescriptionAL0008 = new(
        nameof(Resources.AL0008AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableResourceString TitleAL0009 = new(
        nameof(Resources.AL0009AnalyzerTitle), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableResourceString MessageFormatAL0009 = new(
        nameof(Resources.AL0009AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableResourceString DescriptionAL0009 = new(
        nameof(Resources.AL0009AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

    private static readonly DiagnosticDescriptor RuleAL0007 = new(
        DiagnosticIds.GetSchemaShouldBeExplicitlyImplemented,
        TitleAL0007, MessageFormatAL0007, DiagnosticCategories.Usage,
        DiagnosticSeverity.Error, isEnabledByDefault: true, DescriptionAL0007,
        HelpLinkBase + "AL0007.md");

    private static readonly DiagnosticDescriptor RuleAL0008 = new(
        DiagnosticIds.GetSchemaMustReturnNull,
        TitleAL0008, MessageFormatAL0008, DiagnosticCategories.Usage,
        DiagnosticSeverity.Error, isEnabledByDefault: true, DescriptionAL0008,
        HelpLinkBase + "AL0008.md");

    private static readonly DiagnosticDescriptor RuleAL0009 = new(
        DiagnosticIds.DontCallGetSchema,
        TitleAL0009, MessageFormatAL0009, DiagnosticCategories.Usage,
        DiagnosticSeverity.Error, isEnabledByDefault: true, DescriptionAL0009,
        HelpLinkBase + "AL0009.md");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [RuleAL0007, RuleAL0008, RuleAL0009];

    protected override void RegisterActions(AnalysisContext context)
    {
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    private static void OnCompilationStart(CompilationStartAnalysisContext context)
    {
        if (context.Compilation.GetTypeByMetadataName("System.Xml.Serialization.IXmlSerializable")
            is not { } ixmlSerializable)
            return;

        if (ixmlSerializable.GetMembers("GetSchema").OfType<IMethodSymbol>().SingleOrDefault()
            is not { } getSchemaMethod)
            return;

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
        IMethodSymbol interfaceGetSchema)
    {
        var methodDeclaration = (MethodDeclarationSyntax)context.Node;
        var methodSymbol = context.SemanticModel.GetDeclaredSymbol(methodDeclaration, context.CancellationToken);

        if (methodSymbol is null)
            return;

        if (!IsGetSchemaImplementation(methodSymbol, ixmlSerializable))
            return;

        // AL0007: Check if explicitly implemented
        if (!methodSymbol.ExplicitInterfaceImplementations.Any(i =>
                SymbolEqualityComparer.Default.Equals(i, interfaceGetSchema)))
            context.ReportDiagnostic(RuleAL0007, methodSymbol.Locations[0]);

        // AL0008: Check if abstract or returns non-null
        if (methodSymbol.IsAbstract || ReturnsNonNullValue(methodDeclaration, context.SemanticModel))
        {
            var location = methodDeclaration.DescendantNodes()
                               .FirstOrDefault(n => n is BlockSyntax or ArrowExpressionClauseSyntax)?.GetLocation()
                           ?? methodDeclaration.GetLocation();

            context.ReportDiagnostic(RuleAL0008, location);
        }
    }

    private static void AnalyzeInvocation(
        OperationAnalysisContext context,
        INamedTypeSymbol ixmlSerializable,
        IMethodSymbol interfaceGetSchema)
    {
        var invocation = (IInvocationOperation)context.Operation;
        var targetMethod = invocation.TargetMethod;

        // AL0009: Don't call GetSchema
        if (SymbolEqualityComparer.Default.Equals(targetMethod, interfaceGetSchema) ||
            IsGetSchemaImplementation(targetMethod, ixmlSerializable))
            context.ReportDiagnostic(RuleAL0009, invocation.Syntax.GetLocation());
    }

    private static bool IsGetSchemaImplementation(IMethodSymbol method, INamedTypeSymbol ixmlSerializable)
    {
        var implementsInterface =
            method.ContainingType.AllInterfaces.Contains(ixmlSerializable, SymbolEqualityComparer.Default);

        if (!implementsInterface)
            return false;

        return method.Name == "GetSchema" ||
               method.ExplicitInterfaceImplementations.Any(i => i.Name == "GetSchema");
    }

    private static bool ReturnsNonNullValue(MethodDeclarationSyntax methodDeclaration, SemanticModel model)
    {
        foreach (var node in methodDeclaration.DescendantNodes())
        {
            ExpressionSyntax? expression = node switch
            {
                ReturnStatementSyntax returnStatement => returnStatement.Expression,
                ArrowExpressionClauseSyntax arrow => arrow.Expression,
                _ => null
            };

            if (expression is null)
                continue;

            var constantValue = model.GetConstantValue(expression);

            if (!constantValue.HasValue || constantValue.Value is not null)
                return true;
        }

        return false;
    }
}
