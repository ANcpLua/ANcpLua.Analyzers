using ANcpLua.Analyzers.Core;

namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0001: Prohibit reassignment of primary constructor parameters.
/// </summary>
/// <remarks>
///     <para>
///         Primary constructor parameters should be treated as immutable since they
///         define the initial state of the object. Reassigning them leads to confusion
///         about whether the original or modified value is being used elsewhere in
///         the class.
///     </para>
///     <para>
///         This analyzer detects all forms of reassignment including simple assignment,
///         compound assignment (+=, -=), coalesce assignment (??=), deconstruction
///         assignment, and increment/decrement operations (++, --).
///     </para>
///     <para>
///         The rule only applies to parameters declared in primary constructors on
///         classes, structs, and records. Regular constructor parameters are not
///         subject to this rule since they have limited scope.
///     </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0001ProhibitPrimaryConstructorParameterReassignmentAnalyzer : AlAnalyzer {
    /// <summary>AL0001: Prohibit reassignment of primary constructor parameters.</summary>
    private const string DiagnosticId = DiagnosticIds.ProhibitPrimaryConstructorParameterReassignment;

    private static readonly LocalizableResourceString Title = new(
        nameof(Resources.AL0001AnalyzerTitle), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableResourceString MessageFormat = new(
        nameof(Resources.AL0001AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableResourceString Description = new(
        nameof(Resources.AL0001AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId, Title, MessageFormat, DiagnosticCategories.Design,
        DiagnosticSeverity.Error, true, Description,
        HelpLinkBase);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    /// <summary>Registers syntax and operation actions to analyze primary constructor parameter reassignments.</summary>
    protected override void RegisterActions(AnalysisContext context) {
        context.RegisterOperationAction(AnalyzeAssignment, OperationKind.SimpleAssignment);
        context.RegisterOperationAction(AnalyzeAssignment, OperationKind.CompoundAssignment);
        context.RegisterOperationAction(AnalyzeAssignment, OperationKind.CoalesceAssignment);
        context.RegisterOperationAction(AnalyzeAssignment, OperationKind.DeconstructionAssignment);
        context.RegisterOperationAction(AnalyzeIncrementOrDecrement, OperationKind.Increment);
        context.RegisterOperationAction(AnalyzeIncrementOrDecrement, OperationKind.Decrement);
    }

    private static void AnalyzeAssignment(OperationAnalysisContext context) {
        var operation = (IAssignmentOperation)context.Operation;
        var target = operation.Target;

        if (target is ITupleOperation) {
            CheckTuple(target, context);
        } else {
            CheckTargetAndReport(target, context);
        }
    }

    private static void CheckTuple(IOperation target, OperationAnalysisContext context) {
        if (target is ITupleOperation tuple) {
            foreach (var element in tuple.Elements) {
                CheckTuple(element, context);
            }
        } else {
            CheckTargetAndReport(target, context);
        }
    }

    private static void AnalyzeIncrementOrDecrement(OperationAnalysisContext context) {
        var operation = (IIncrementOrDecrementOperation)context.Operation;
        CheckTargetAndReport(operation.Target, context);
    }

    private static void CheckTargetAndReport(IOperation target, OperationAnalysisContext context) {
        if (target is not IParameterReferenceOperation parameterRef) {
            return;
        }

        if (parameterRef.Parameter.ContainingSymbol is not IMethodSymbol { MethodKind: MethodKind.Constructor } ctor) {
            return;
        }

        if (!ctor.DeclaringSyntaxReferences.Any(sr =>
                sr.GetSyntax(context.CancellationToken) is ClassDeclarationSyntax or StructDeclarationSyntax
                    or RecordDeclarationSyntax)) {
            return;
        }

        context.ReportDiagnostic(Rule, target.Syntax.GetLocation(), parameterRef.Parameter.Name);
    }
}
