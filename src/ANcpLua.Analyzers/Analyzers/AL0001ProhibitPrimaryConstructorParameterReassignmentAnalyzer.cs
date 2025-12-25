using ANcpLua.Analyzers.Core;

namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0001: Prohibit reassignment of primary constructor parameters.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AL0001ProhibitPrimaryConstructorParameterReassignmentAnalyzer : ALAnalyzer
{
    public const string DiagnosticId = DiagnosticIds.ProhibitPrimaryConstructorParameterReassignment;

    private static readonly LocalizableResourceString Title = new(
        nameof(Resources.AL0001AnalyzerTitle), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableResourceString MessageFormat = new(
        nameof(Resources.AL0001AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableResourceString Description = new(
        nameof(Resources.AL0001AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId, Title, MessageFormat, DiagnosticCategories.Design,
        DiagnosticSeverity.Error, isEnabledByDefault: true, Description,
        HelpLinkBase + "AL0001.md");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    protected override void RegisterActions(AnalysisContext context)
    {
        context.RegisterOperationAction(AnalyzeAssignment, OperationKind.SimpleAssignment);
        context.RegisterOperationAction(AnalyzeAssignment, OperationKind.CompoundAssignment);
        context.RegisterOperationAction(AnalyzeAssignment, OperationKind.CoalesceAssignment);
        context.RegisterOperationAction(AnalyzeAssignment, OperationKind.DeconstructionAssignment);
        context.RegisterOperationAction(AnalyzeIncrementOrDecrement, OperationKind.Increment);
        context.RegisterOperationAction(AnalyzeIncrementOrDecrement, OperationKind.Decrement);
    }

    private static void AnalyzeAssignment(OperationAnalysisContext context)
    {
        var operation = (IAssignmentOperation)context.Operation;
        var target = operation.Target;

        if (target is ITupleOperation)
            CheckTuple(target, context);
        else
            CheckTargetAndReport(target, context);
    }

    private static void CheckTuple(IOperation target, OperationAnalysisContext context)
    {
        if (target is ITupleOperation tuple)
            foreach (var element in tuple.Elements)
                CheckTuple(element, context);
        else
            CheckTargetAndReport(target, context);
    }

    private static void AnalyzeIncrementOrDecrement(OperationAnalysisContext context)
    {
        var operation = (IIncrementOrDecrementOperation)context.Operation;
        CheckTargetAndReport(operation.Target, context);
    }

    private static void CheckTargetAndReport(IOperation target, OperationAnalysisContext context)
    {
        if (target is not IParameterReferenceOperation parameterRef)
            return;

        if (parameterRef.Parameter.ContainingSymbol is not IMethodSymbol { MethodKind: MethodKind.Constructor } ctor)
            return;

        if (!ctor.DeclaringSyntaxReferences.Any(sr =>
                sr.GetSyntax(context.CancellationToken) is ClassDeclarationSyntax or StructDeclarationSyntax
                    or RecordDeclarationSyntax))
            return;

        context.ReportDiagnostic(Rule, target.Syntax.GetLocation(), parameterRef.Parameter.Name);
    }
}