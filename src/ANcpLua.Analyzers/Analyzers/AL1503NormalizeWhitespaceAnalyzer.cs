
namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL1503: Detects calls to NormalizeWhitespace() which is expensive in source generators.
/// </summary>
/// <remarks>
///     NormalizeWhitespace (from Microsoft.CodeAnalysis.SyntaxNodeExtensions)
///     traverses the entire syntax tree to rewrite whitespace trivia. In source generators that run
///     on every keystroke, this adds unnecessary overhead. Raw string literals or manual string building
///     produces identical output without the syntax tree overhead.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al1503NormalizeWhitespaceAnalyzer : AlAnalyzer {
    /// <summary>The diagnostic identifier for AL1503.</summary>
    public const string DiagnosticId = "AL1503";

    private static readonly DiagnosticDescriptor s_rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.RoslynUtilities,
        DiagnosticSeverities.Suggestion);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_rule];

    /// <summary>Registers an operation action to analyze method invocations.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterCompilationStartAction(OnCompilationStart);

    private static void OnCompilationStart(CompilationStartAnalysisContext context) {
        if (context.Compilation.GetTypeByMetadataName("Microsoft.CodeAnalysis.SyntaxNode") is null) {
            return;
        }

        context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
    }

    private static void AnalyzeInvocation(OperationAnalysisContext context) {
        var invocation = (IInvocationOperation)context.Operation;
        var method = invocation.TargetMethod;

        if (method.Name is not "NormalizeWhitespace") {
            return;
        }

        if (method.ContainingType?.ContainingNamespace?.ToDisplayString() is not "Microsoft.CodeAnalysis") {
            return;
        }

        if (IsTextOutputSink(invocation)) {
            return;
        }

        // Narrow the span to just NormalizeWhitespace() rather than the full receiver chain
        if (invocation.Syntax is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax memberAccess } invocationSyntax) {
            var span = Microsoft.CodeAnalysis.Text.TextSpan.FromBounds(memberAccess.Name.SpanStart, invocationSyntax.Span.End);
            context.ReportDiagnostic(Diagnostic.Create(s_rule,
                Location.Create(invocationSyntax.SyntaxTree, span)));
        } else {
            context.ReportDiagnostic(s_rule, invocation.Syntax.GetLocation());
        }
    }

    private static bool IsTextOutputSink(IInvocationOperation invocation) {
        if (invocation.Parent is not IInvocationOperation parentInvocation) {
            return false;
        }

        if (!IsTextOutputMethod(parentInvocation.TargetMethod.Name)) {
            return false;
        }

        var instance = parentInvocation.Instance;

        while (instance is not null) {
            if (ReferenceEquals(instance, invocation)) {
                return true;
            }

            if (instance is IConversionOperation conversion) {
                instance = conversion.Operand;
                continue;
            }

            if (instance is IParenthesizedOperation parenthesized) {
                instance = parenthesized.Operand;
                continue;
            }

            return false;
        }

        return false;
    }

    private static bool IsTextOutputMethod(string methodName) =>
        string.Equals(methodName, "ToFullString", StringComparison.Ordinal) ||
        string.Equals(methodName, "ToString", StringComparison.Ordinal);
}
