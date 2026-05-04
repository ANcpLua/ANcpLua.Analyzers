
namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0113: Detects Activity.SetStatus(Error) inside catch blocks without recording the exception.
/// </summary>
/// <remarks>
///     <para>
///         When setting an Activity status to Error in a catch block, the exception should be
///         recorded as an event on the span for proper observability. Without the exception event,
///         traces show that an error occurred but provide no details about what went wrong.
///     </para>
///     <para>
///         The fix is to call <c>activity.AddEvent(new ActivityEvent("exception", ...))</c> or
///         a <c>RecordException</c> helper before or after the <c>SetStatus</c> call.
///     </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0113MissingExceptionRecordingOnActivityAnalyzer : AlAnalyzer {
    /// <summary>The diagnostic identifier for AL0113.</summary>
    private const string DiagnosticId = "AL0113";

    private static readonly DiagnosticDescriptor s_rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.OpenTelemetry,
        DiagnosticSeverity.Warning);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_rule];

    /// <summary>Registers a compilation-start action to resolve Activity and ActivityStatusCode types.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterCompilationStartAction(OnCompilationStart);

    private static void OnCompilationStart(CompilationStartAnalysisContext context) {
        var activityType = context.Compilation.GetTypeByMetadataName("System.Diagnostics.Activity");
        var statusCodeType = context.Compilation.GetTypeByMetadataName("System.Diagnostics.ActivityStatusCode");

        if (activityType is null || statusCodeType is null) {
            return;
        }

        context.RegisterOperationAction(
            ctx => AnalyzeInvocation(ctx, activityType, statusCodeType),
            OperationKind.Invocation);
    }

    private static void AnalyzeInvocation(
        OperationAnalysisContext context,
        INamedTypeSymbol activityType,
        INamedTypeSymbol statusCodeType) {
        var invocation = (IInvocationOperation)context.Operation;

        if (!IsSetStatusErrorOnActivity(invocation, activityType, statusCodeType)) {
            return;
        }

        if (FindEnclosingCatchClause(invocation) is not { } catchClause) {
            return;
        }

        var activityInstance = GetReceiverIdentifier(invocation);

        if (HasExceptionRecording(catchClause, activityInstance)) {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(s_rule, invocation.Syntax.GetLocation()));
    }

    private static bool IsSetStatusErrorOnActivity(
        IInvocationOperation invocation,
        INamedTypeSymbol activityType,
        INamedTypeSymbol statusCodeType) {
        if (invocation.TargetMethod.Name != "SetStatus") {
            return false;
        }

        if (!SymbolEqualityComparer.Default.Equals(invocation.TargetMethod.ContainingType, activityType)) {
            return false;
        }

        if (invocation.Arguments.Length is 0) {
            return false;
        }

        var firstArg = invocation.Arguments[0].Value;
        while (firstArg is IConversionOperation conversion) {
            firstArg = conversion.Operand;
        }

        if (firstArg is not IFieldReferenceOperation fieldRef) {
            return false;
        }

        return SymbolEqualityComparer.Default.Equals(fieldRef.Field.ContainingType, statusCodeType) &&
               fieldRef.Field.Name == "Error";
    }

    private static IOperation? FindEnclosingCatchClause(IOperation operation) {
        var current = operation.Parent;
        while (current is not null) {
            switch (current) {
                case ICatchClauseOperation:
                    return current;
                case IMethodBodyOperation:
                case IAnonymousFunctionOperation:
                case ILocalFunctionOperation:
                    return null;
            }

            current = current.Parent;
        }

        return null;
    }

    private static string? GetReceiverIdentifier(IInvocationOperation invocation) =>
        invocation.Instance switch {
            ILocalReferenceOperation local => local.Local.Name,
            IParameterReferenceOperation param => param.Parameter.Name,
            IFieldReferenceOperation field => field.Field.Name,
            _ => null
        };

    private static bool HasExceptionRecording(IOperation catchClause, string? activityIdentifier) =>
        SearchForExceptionRecording(catchClause, activityIdentifier);

    private static bool SearchForExceptionRecording(IOperation operation, string? activityIdentifier) {
        switch (operation) {
            case IInvocationOperation invocation:
                if (IsExceptionRecordingCall(invocation, activityIdentifier)) {
                    return true;
                }

                break;
        }

        foreach (var child in operation.ChildOperations) {
            if (SearchForExceptionRecording(child, activityIdentifier)) {
                return true;
            }
        }

        return false;
    }

    private static bool IsExceptionRecordingCall(IInvocationOperation invocation, string? activityIdentifier) {
        var methodName = invocation.TargetMethod.Name;

        switch (methodName) {
            case "RecordException":
            case "AddException":
                return activityIdentifier is null || MatchesReceiver(invocation, activityIdentifier);

            case "AddEvent":
                if (activityIdentifier is not null && !MatchesReceiver(invocation, activityIdentifier)) {
                    return false;
                }

                return HasExceptionEventArgument(invocation);

            default:
                return false;
        }
    }

    private static bool MatchesReceiver(IInvocationOperation invocation, string activityIdentifier) =>
        invocation.Instance switch {
            ILocalReferenceOperation local => local.Local.Name == activityIdentifier,
            IParameterReferenceOperation param => param.Parameter.Name == activityIdentifier,
            IFieldReferenceOperation field => field.Field.Name == activityIdentifier,
            _ => false
        };

    private static bool HasExceptionEventArgument(IInvocationOperation invocation) {
        if (invocation.Arguments.Length is 0) {
            return false;
        }

        var arg = invocation.Arguments[0].Value;
        while (arg is IConversionOperation conversion) {
            arg = conversion.Operand;
        }

        if (arg is not IObjectCreationOperation creation) {
            return true;
        }

        if (creation.Arguments.Length is 0) {
            return false;
        }

        var nameArg = creation.Arguments[0].Value;
        while (nameArg is IConversionOperation nameConversion) {
            nameArg = nameConversion.Operand;
        }

        return nameArg.ConstantValue is { HasValue: true, Value: string eventName } &&
               eventName.EqualsIgnoreCase("exception");
    }
}
